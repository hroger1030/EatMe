using EatMeServer;
using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Threading;

try
{
    ConfigManager.Load();
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to load config: {ex.Message}");
    return 1;
}

LoadingScreenTipsManager.Load();
BotNameManager.Load();
HighScoreManager.Load();

var settings = ConfigManager.Settings;

Console.WriteLine($"{settings.ServerName} starting");
Console.WriteLine($"  Port:          {settings.Port}");
Console.WriteLine($"  DiscoveryPort: {settings.DiscoveryPort}");
Console.WriteLine($"  MaxPlayers:    {settings.GameConfig.MaxPlayers}");
Console.WriteLine($"  MinPlayers:    {settings.GameConfig.MinPlayers}");
Console.WriteLine($"  AutoBalance:   {settings.AutoBalanceTeams}");
Console.WriteLine($"  TickRate:      {settings.TickRate}");
Console.WriteLine($"  TargetFps:     {settings.TargetFps}");
Console.WriteLine($"  Tips:          {LoadingScreenTipsManager.Tips.Count}");
Console.WriteLine($"  MOTD:          {settings.MessageOfTheDay}");
Console.WriteLine($"  Bot names:     {BotNameManager.Names.Count}");
Console.WriteLine($"  High scores:   {HighScoreManager.Scores.Count}");

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
    Console.Error.WriteLine($"Can't listen on port {settings.Port}: {ex.Message}");
    return 1;
}

try
{
    discovery.Start(settings.DiscoveryPort, shutdown.Token);
}
catch (SocketException ex)
{
    // the game still works by IP address without discovery
    Console.WriteLine($"LAN discovery off, can't listen on port {settings.DiscoveryPort}: {ex.Message}");
}

Console.WriteLine("Game paused until a player joins");
Console.WriteLine("Press Ctrl+C to stop.");

var tickInterval = TimeSpan.FromSeconds(1.0 / Math.Max(1, settings.TickRate));
var stopwatch = Stopwatch.StartNew();
var lastTickMs = stopwatch.ElapsedMilliseconds;

while (!shutdown.IsCancellationRequested)
{
    var now = stopwatch.Elapsed;

    // whole ms from the stopwatch's own count, so the game clock never drifts from it
    var nowMs = stopwatch.ElapsedMilliseconds;
    var elapsedMs = (int)(nowMs - lastTickMs);
    lastTickMs = nowMs;

    while (network.TryGetEvent(out var networkEvent))
        game.HandleEvent(networkEvent);

    game.Update(elapsedMs);

    var sleepTime = tickInterval - (stopwatch.Elapsed - now);

    if (sleepTime > TimeSpan.Zero)
        shutdown.Token.WaitHandle.WaitOne(sleepTime);
}

discovery.Stop();
network.Stop();

Console.WriteLine("Server stopped.");
return 0;
