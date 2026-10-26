# Eat Me

A multiplayer, retro-style arcade game for LAN and Internet play: capture-the-flag Pac-Man in a large, randomly generated maze. Two teams of pacs, blue on the left and red on the right, eat dots to grow, eat each other, dodge ghosts, grab power ups and steal each other's flags. The first team to the target score wins the level.

The game is split into a headless server, which runs the whole simulation and its bots, and a MonoGame client, which draws what the server sends and passes the player's input back. Both are written in C# on .NET 10 and share a common library.

The full design and game rules are in [Specs.md](Specs.md).

## Requirements

- .NET 10 SDK
- Windows to build the client's content: the content build runs as a local dotnet tool (MonoGame 3.8, restored automatically)
- The **FastG** geometry library checked out next to this repo (see below).

## Getting the source

The library and client reference `..\FastG\FastG\FastG.csproj`, so clone both repos into the same parent folder:

```
<parent>\
    EatMe\      this repo
    FastG\      FastG\FastG.csproj
```

## Folder structure

```
EatMe\
    EatMe.sln               Solution with all four projects
    Specs.md                Product spec: game rules, levels, maps, networking
    CLAUDE.md               Detailed notes on how the code is put together
    ~PublishPC.bat          Publishes Windows release builds to publish\
    ~PublishMac.bat         Publishes macOS (Apple silicon) release builds to publish\
    ~StartServer.bat        Runs the published Windows server
    ~StartClient.bat        Runs the published Windows client

    EatMeClient\            MonoGame DesktopGL game client
        Content\            Content.mgcb and every asset it builds
            audio\          Sound effects and announcer clips (WAV)
            effects\        Shaders (.fx): brightness, bloom, pixel art, rainbow walls
            fonts\          Sprite fonts and the JLS Space Gothic and Liberation Sans font files
            images\         Sprite sheets, tiles, backgrounds and the logo
            music\          Title theme and level tracks (MP3)
            text\           Plain text such as the attract screen story
        Objects\
            Screens\        Title, attract, join, settings, high scores and game screens
            Controls\       UI controls: menus, sliders, mini map, player list, overlays
            Managers\       Assets, input, music, sound, settings and the content pack
            Classes\        Camera, settings, shader wrappers
            Helpers\        Drawing, text, texture and clipboard helpers
        readme.txt          Player notes, shipped with the published client

    EatMeServer\            Headless console server
        Objects\            Game server, console commands, logging, networking
        server.cfg          Server settings (JSON)
        LoadingScreenTips.txt   One tip per line, shown between levels
        bot_names.txt       Names given to bots, one per line
        highscores.txt      The server's high score table
        readme.txt          Host notes, shipped with the published server

    EatMeLibrary\           Code shared by client and server (no MonoGame dependency)
        Objects\            Map generation, movement, rules, bot and ghost brains, levels
            Network\        Messages, serialization and the TCP connection
            Const\          Content names and UI strings
            Enums\          Shared enums

    EatMeTests\             NUnit tests for the library and server
```

## Building

```
dotnet build EatMe.sln
```

The first build restores NuGet packages and the MonoGame content builder tool, then builds every asset in `EatMeClient/Content/Content.mgcb`.

## Running the tests

```
dotnet test EatMeTests
```

The tests need no network setup: the loopback tests use a free port on 127.0.0.1.

## Running a game locally

Start the server first, then the client.

**Server:**

```
dotnet run --project EatMeServer
```

The server reads `server.cfg` from its output folder, prints the settings it loaded, then runs until you type `quit` or press Ctrl+C. If `server.cfg` is missing or invalid it prints an error and exits with code 1. While no players are connected the game is paused; bots fill the teams up to `MinPlayers`.

**Client:**

```
dotnet run --project EatMeClient
```

Pick Join Multiplayer Game, type a name, pick a team, and choose the server from the LAN games list (or type `127.0.0.1`). The client opens full screen at the desktop resolution; the Display setting switches to a window. Settings are saved to `settings.ini` next to the executable. Only one client runs at a time per Windows login. F1 shows a performance overlay.

Controls and how to play are in [EatMeClient/readme.txt](EatMeClient/readme.txt).

## Publishing

| Script | Output |
|---|---|
| `~PublishPC.bat` | Windows x64 builds in `publish\pc_client` and `publish\pc_server` |
| `~PublishMac.bat` | macOS Apple silicon builds in `publish\mac_client` and `publish\mac_server` |

Both make Release, single-file, framework-dependent builds (players need the .NET 10 runtime) with no `.pdb` files. The client's content is packed into `Resource.pac` next to the exe. Each folder also gets its project's `readme.txt`.

`~StartServer.bat` and `~StartClient.bat` run the published Windows builds from their own folders, so run `~PublishPC.bat` first.

The client and server versions must match (`NetworkConstants.APP_VERSION`, shown in the top right of the title screen). Servers of another version are greyed out in the LAN list.

## Server configuration

### Files

| File | What it holds |
|---|---|
| `server.cfg` | Settings, in JSON. Game rules are in the `GameConfig` block. Edit `EatMeServer/server.cfg`; it is copied to the output folder on build. |
| `LoadingScreenTips.txt` | One tip per line. Every player gets them all, and one is shown at random between levels. A missing file means no tips. |
| `bot_names.txt` | Names given to bots, one per line. |
| `highscores.txt` | The server's high score table, one `Name,Score,Date` per line. The server rewrites it when it changes; a build doesn't overwrite it unless the source file is newer. |

### server.cfg

The shipped file lists every setting. Anything left out of the file keeps its built-in default. Out of range values are clamped, except `MazeSize`, which must be valid or the server won't start.

| Setting | Type | server.cfg | Description |
|---|---|---|---|
| `ServerName` | string | `Eat Me Server` | Name shown in the LAN games list. |
| `MessageOfTheDay` | string | | Scrolls across the bottom of each player's title screen after they've played on the server. Empty or missing uses a built-in message. |
| `Port` | int | 7777 | TCP port players connect to. |
| `DiscoveryPort` | int | 7778 | UDP port that answers LAN game searches. |
| `TickRate` | int | 60 | Simulation ticks per second. |
| `ClientUpdatesPerSecond` | int | 30 | Times per second the game state is sent to players. |
| `AutoBalanceTeams` | bool | true | Bots keep both teams the same size. |
| `StartingLevel` | int | 1 | Level the server starts on (and goes back to on `new game`). |
| `RandomLevels` | bool | false | Every level after the starting one is picked at random, never the one just played. |
| `GameConfig.MaxPlayers` | int | 128 | Most players at once. |
| `GameConfig.MinPlayers` | int | 4 | Players (humans plus bots) the server keeps; bots make up the difference. |
| `GameConfig.MaxScore` | int | 100 | Team score needed to win the level. |
| `GameConfig.FlagScore` | int | 20 | Points for capturing a flag. |
| `GameConfig.FlagRescue` | int | 3 | Points for returning your own dropped flag. |
| `GameConfig.PlayerKill` | int | 1 | Points for eating an enemy pac. |
| `GameConfig.GhostKill` | int | 2 | Points for killing a ghost. |
| `GameConfig.MazeSize` | int | 100 | Width and height of the maze in cells. Must be even and at least 22. |
| `GameConfig.WarpTunnelCount` | int | 4 | Warp tunnels through the outer wall. |
| `GameConfig.GhostCount` | int | 6 | Ghosts in the maze. |
| `GameConfig.PowerPillCount` | int | 8 | Power pills in the maze, split between the two halves. |
| `GameConfig.FlagCount` | int | 1 | Flags per team. |
| `GameConfig.PlayerBaseCount` | int | 1 | Spawn points per team. |
| `GameConfig.GhostVisionDistance` | int | 10 | Cells within which a ghost spots and chases a pac. |
| `GameConfig.GhostRegenTimeMs` | int | 5000 | ms before a dead ghost comes back. |
| `GameConfig.DotRegenPercent` | int | 50 | Percent (0–100) of a level's dots that stay eaten; past that, each dot eaten brings back the oldest eaten one. |
| `GameConfig.PowerPillRegenTimeMs` | int | 30000 | ms before an eaten power pill comes back. |
| `GameConfig.PlayerRespawnTimeMs` | int | 10000 | ms before a dead pac respawns. |
| `GameConfig.PacMaxSize` | float | 100.0 | The most dots can make a pac. A pac needs this size and a power pill working to eat ghosts. |
| `GameConfig.PacSpeed` | float | 6.0 | Cells per second of a size 0 pac; a full sized pac is 20% faster. |
| `GameConfig.GhostSpeed` | float | 6.0 | Ghost speed in cells per second. |
| `GameConfig.PowerUpSpeed` | float | 2.5 | Power up speed in cells per second. |
| `GameConfig.PacShrinkPerSecond` | float | 5.0 | Size a starving pac loses each second. |
| `GameConfig.MsUntilStarvation` | int | 1000 | ms after a pac last ate before it starts starving. |
| `GameConfig.DotSizeGain` | float | 1.0 | Size gained per dot. |
| `GameConfig.PowerPillSizeGain` | float | 100.0 | Size gained from a power pill, and lost again when it wears off. |
| `GameConfig.PowerUpDurationMs` | int | 10000 | ms every power up effect lasts. |
| `GameConfig.IntermissionTimeMs` | int | 10000 | ms the results are shown between levels. |
| `GameConfig.PowerUpSpawnTimerMs` | int | 30000 | ms between power up spawns; 0 for none. |
| `GameConfig.TurnBufferTimeMs` | int | 400 | ms a tapped direction is remembered for the next junction. 0 is off. |
| `GameConfig.TurnGraceDistance` | float | 0.3 | Cells past a junction a late turn still happens. 0 is off. |
| `GameConfig.LagCompensationMaxMs` | int | 250 | Most ms of a player's lag that widen their late turn window. 0 is off. |
| `GameConfig.ServerSeed` | int | 424242 | Seed for the maps; each level's map comes from this plus the level number. |

## Server console commands

Commands are typed into the server's window while it runs and ignore case. The prompt at the bottom shows the uptime as `[up hh.mm.ss]`; Up and Down bring back earlier commands and Esc clears the line. Nothing a command changes is written back to `server.cfg`, so a restart goes back to the file's values.

| Command | What it does |
|---|---|
| `help` (or `?`) | Lists the commands. |
| `status` | Level, scores and player counts. |
| `players` | Every player with their id, team, scores and lag. |
| `next level` (or `next`) | Starts the next level now (a random one with RandomLevels on). |
| `level {n}` | Jumps to level n. |
| `restart` | Restarts the current level. |
| `new game` | Goes back to the starting level. |
| `kick {name or id}` | Removes a player from the server. |
| `bots [n]` | Shows or sets MinPlayers. |
| `autobalance [on\|off]` | Shows or sets AutoBalanceTeams. |
| `motd [text]` | Shows or sets the message of the day for players who join from now on. |
| `settings` | Lists every setting with its command and value. |
| `perf [on\|off]` | Shows or sets perf logging: every 5 seconds, tick rate and times, load, GC, memory, traffic and each step's times. |
| `clear` (or `cls`) | Clears the console. |
| `quit` (or `stop`, `exit`) | Stops the server. |

### Setting commands

Every other `server.cfg` setting except `Port` and `DiscoveryPort` has its own command, its name in lower case. Typed alone it shows the value; typed with a value it changes it, e.g. `pacspeed 7.5`, `mazesize 60`, `randomlevels on`, `servername Friday Night Pac`. `[n]` is a whole number and `[x]` can have decimals.

| Command | Setting |
|---|---|
| `servername [text]` | ServerName |
| `tickrate [n]` | TickRate |
| `clientupdatespersecond [n]` | ClientUpdatesPerSecond |
| `startinglevel [n]` | StartingLevel |
| `randomlevels [on\|off]` | RandomLevels |
| `maxplayers [n]` | MaxPlayers |
| `maxscore [n]` | MaxScore |
| `flagscore [n]` | FlagScore |
| `flagrescue [n]` | FlagRescue |
| `playerkill [n]` | PlayerKill |
| `ghostkill [n]` | GhostKill |
| `mazesize [n]` | MazeSize |
| `warptunnelcount [n]` | WarpTunnelCount |
| `ghostcount [n]` | GhostCount |
| `powerpillcount [n]` | PowerPillCount |
| `flagcount [n]` | FlagCount |
| `playerbasecount [n]` | PlayerBaseCount |
| `ghostvisiondistance [n]` | GhostVisionDistance |
| `ghostregentimems [n]` | GhostRegenTimeMs |
| `dotregenpercent [n]` | DotRegenPercent |
| `powerpillregentimems [n]` | PowerPillRegenTimeMs |
| `playerrespawntimems [n]` | PlayerRespawnTimeMs |
| `pacmaxsize [x]` | PacMaxSize |
| `pacspeed [x]` | PacSpeed |
| `ghostspeed [x]` | GhostSpeed |
| `powerupspeed [x]` | PowerUpSpeed |
| `pacshrinkpersecond [x]` | PacShrinkPerSecond |
| `msuntilstarvation [n]` | MsUntilStarvation |
| `dotsizegain [x]` | DotSizeGain |
| `powerpillsizegain [x]` | PowerPillSizeGain |
| `powerupdurationms [n]` | PowerUpDurationMs |
| `intermissiontimems [n]` | IntermissionTimeMs |
| `powerupspawntimerms [n]` | PowerUpSpawnTimerMs |
| `turnbuffertimems [n]` | TurnBufferTimeMs |
| `turngracedistance [x]` | TurnGraceDistance |
| `lagcompensationmaxms [n]` | LagCompensationMaxMs |
| `serverseed [n]` | ServerSeed |

`MinPlayers`, `AutoBalanceTeams` and `MessageOfTheDay` are `bots`, `autobalance` and `motd` (`minplayers`, `autobalanceteams` and `messageoftheday` work too).

Values are checked as in `server.cfg`: a bad `MazeSize` (odd or too small) is ignored and the old value stays; anything else out of range is clamped.

When a change takes effect:

- **Now:** most settings. Game rule changes are sent to every connected player.
- **Next level** (or now, with `restart`): the map settings, `MazeSize`, `WarpTunnelCount`, `GhostCount`, `PowerPillCount`, `FlagCount`, `PlayerBaseCount` and `ServerSeed`.
- **Next level change:** `RandomLevels`.
- **Next `new game`:** `StartingLevel`.

## Hosting notes

The server listens on all IPv4 interfaces, so nothing in the code needs changing to host.

| Port | Protocol | Used for |
|---|---|---|
| `Port` (7777) | TCP | Players connect on it. The only port Internet players need. |
| `DiscoveryPort` (7778) | UDP | Answers LAN game searches. Don't forward it. |

**On a LAN**, players find the game in the Join Multiplayer Game screen's LAN games list. Allow the server through the PC's firewall; Windows usually asks the first time it runs.

**Over the Internet**, players type in an address, and your router has to let them through.

1. Find your PC's local IP: run `ipconfig` and take the IPv4 address of your network adapter (e.g. `192.168.0.3`).
2. Allow the game port through the firewall. From an administrator PowerShell:
   ```powershell
   New-NetFirewallRule -DisplayName "EatMe Server" -Direction Inbound -Protocol TCP -LocalPort 7777 -Action Allow
   ```
3. In the router's port forwarding page, forward TCP port `7777` to your PC's local IP, from all remote addresses.
4. Find your public IP (e.g. at whatismyip.com). Players enter it in the Join Multiplayer Game address box, adding `:port` if you changed `Port` (e.g. `203.0.113.5:7777`).
5. When you're done, delete the forwarding rule.

Your public IP can change, and connecting to it from inside your own network often fails (many routers lack NAT loopback), so test from another network such as a phone hotspot.

**Tunnel (no router changes):** if you can't change the router, or your ISP uses CGNAT (the router's WAN IP differs from your public IP), run a TCP tunnel such as playit.gg pointed at local port 7777. It gives you a `host:port` address that players type into the Join Multiplayer Game screen. Close the tunnel when you're done.

**Security:** there is no login and no ban list, so anyone who knows the address can join while the port is open. Use `kick` to remove a player.

## Further reading

- [Specs.md](Specs.md): the game design and rules.
- [EatMeClient/readme.txt](EatMeClient/readme.txt): player notes, controls and how to play.
- [EatMeServer/readme.txt](EatMeServer/readme.txt): host notes shipped with the server.
