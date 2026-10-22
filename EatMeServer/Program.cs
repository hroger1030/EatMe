using EatMeLibrary;
using EatMeServer;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

// server uptime, shown in the console's typing prompt
var uptime = Stopwatch.StartNew();

// help text is drawn in a 24-bit color, which the Windows console only shows with virtual terminal processing on
ServerLog.EnableAnsiColors();

try
{
    ConfigManager.Load();
}
catch (Exception ex)
{
    ServerLog.Write(eLogType.Error, $"Failed to load config: {ex.Message}");
    return 1;
}

LoadingScreenTipsManager.Load();
BotNameManager.Load();
HighScoreManager.Load();

var settings = ConfigManager.Settings;

ServerLog.Write(eLogType.Level, $"{settings.ServerName} starting");
ServerLog.Write(eLogType.Info, $"  Port:          {settings.Port}");
ServerLog.Write(eLogType.Info, $"  DiscoveryPort: {settings.DiscoveryPort}");
ServerLog.Write(eLogType.Info, $"  MaxPlayers:    {settings.GameConfig.MaxPlayers}");
ServerLog.Write(eLogType.Info, $"  MinPlayers:    {settings.GameConfig.MinPlayers}");
ServerLog.Write(eLogType.Info, $"  AutoBalance:   {settings.AutoBalanceTeams}");
ServerLog.Write(eLogType.Info, $"  RandomLevels:  {settings.RandomLevels}");
ServerLog.Write(eLogType.Info, $"  TickRate:      {settings.TickRate}");
ServerLog.Write(eLogType.Info, $"  ClientUpdates: {settings.ClientUpdatesPerSecond}");
ServerLog.Write(eLogType.Info, $"  Tips:          {LoadingScreenTipsManager.Tips.Count}");
ServerLog.Write(eLogType.Info, $"  MOTD:          {settings.MessageOfTheDay}");
ServerLog.Write(eLogType.Info, $"  Bot names:     {BotNameManager.Names.Count}");
ServerLog.Write(eLogType.Info, $"  High scores:   {HighScoreManager.Scores.Count}");

using var shutdown = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    shutdown.Cancel();
};

var game = new GameServer(settings, LoadingScreenTipsManager.Tips, level: settings.StartingLevel, botNames: BotNameManager.Names,
    highScores: HighScoreManager.Scores, onHighScoresChanged: HighScoreManager.Save);
var network = new NetworkServer();
var discovery = new DiscoveryResponder(game.BuildDiscoveryResponse);

try
{
    network.Start(settings.Port, shutdown.Token);
}
catch (SocketException ex)
{
    ServerLog.Write(eLogType.Error, $"Can't listen on port {settings.Port}: {ex.Message}");
    return 1;
}

try
{
    discovery.Start(settings.DiscoveryPort, shutdown.Token);
}
catch (SocketException ex)
{
    // the game still works by IP address without discovery
    ServerLog.Write(eLogType.Warning, $"LAN discovery off, can't listen on port {settings.DiscoveryPort}: {ex.Message}");
}

ServerLog.Write(eLogType.Info, "Game paused until a player joins");
ServerLog.Write(eLogType.Info, "Type help for commands, quit or Ctrl+C to stop.");

// someone types at a console: the server draws the typed line itself, under the log, so logged lines never break into it
var isInteractive = !Console.IsInputRedirected && !Console.IsOutputRedirected;

if (isInteractive)
    ServerLog.EnablePrompt(uptime);

// typed lines are read on their own thread and run on the main loop, so commands touch the game on one thread only
var commands = new ConcurrentQueue<string>();
var commandReader = new Thread(() =>
{
    try
    {
        if (isInteractive)
        {
            while (true)
            {
                if (ServerLog.HandleKey(Console.ReadKey(intercept: true), out var typed))
                    commands.Enqueue(typed);
            }
        }
        else
        {
            string line;

            // null once input is closed
            while ((line = Console.ReadLine()) != null)
                commands.Enqueue(line);
        }
    }
    catch (Exception ex)
    {
        ServerLog.Write(eLogType.Warning, $"Console commands off: {ex.Message}");
    }
})
{ IsBackground = true, Name = "Console commands" };
commandReader.Start();

var stopwatch = Stopwatch.StartNew();
var lastTickMs = stopwatch.ElapsedMilliseconds;

while (!shutdown.IsCancellationRequested)
{
    var now = stopwatch.Elapsed;

    // whole ms from the stopwatch's own count, so the game clock never drifts from it
    var nowMs = stopwatch.ElapsedMilliseconds;
    var elapsedMs = (int)(nowMs - lastTickMs);
    lastTickMs = nowMs;

    // the tick's parts are timed for perf logging; Record returns now, the next part's start
    var tickStart = PerfStats.GetTimestamp();

    while (network.TryGetEvent(out var networkEvent))
        game.HandleEvent(networkEvent);

    var perfStart = game.Perf.Record(ePerfSection.Network, tickStart);

    while (commands.TryDequeue(out var command))
    {
        var output = ServerCommands.Execute(game, command, out var quit);

        if (output.Length > 0)
            ServerLog.Write(ServerCommands.GetOutputType(command), output);

        if (quit)
            shutdown.Cancel();
    }

    game.Perf.Record(ePerfSection.Commands, perfStart);
    game.Update(elapsedMs);

    // the prompt follows window resizes and its uptime
    ServerLog.Refresh();
    game.Perf.Record(ePerfSection.Tick, tickStart);

    var perfReport = game.Perf.Update(game);

    if (perfReport != null)
        ServerLog.Write(eLogType.Perf, perfReport);

    // read each tick, so the console's set TickRate takes effect at once
    var tickInterval = TimeSpan.FromSeconds(1.0 / Math.Max(1, settings.TickRate));
    var sleepTime = tickInterval - (stopwatch.Elapsed - now);

    if (sleepTime > TimeSpan.Zero)
        shutdown.Token.WaitHandle.WaitOne(sleepTime);
}

discovery.Stop();
network.Stop();

ServerLog.DisablePrompt();
ServerLog.Write(eLogType.Level, "Server stopped.");
return 0;
