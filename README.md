# EatMe

A multiplayer, retro-style arcade game for LAN and Internet play: capture-the-flag Pac-Man in a large, randomly generated maze. Two teams (red and blue) of pacs eat dots, grow, eat each other, and dodge ghosts.

See [Specs.md](Specs.md) for the full design and game rules.

## Projects

| Project | Type | Description |
|---|---|---|
| `EatMeClient` | MonoGame DesktopGL app | Game client: title, settings, story, high scores, credits and game screens. |
| `EatMeServer` | Console app (headless) | Game server. Reads its configuration from `server.cfg` on startup. |
| `EatMeLibrary` | Class library | Code shared by the client and server. Referenced by both. |
| `EatMeTests` | NUnit test project | Unit tests for the library and server. Run with `dotnet test EatMeTests`. |

The client also references two sibling projects that must be checked out next to this repo:

- `..\FastG\FastG\FastG.csproj` (geometry)

## Requirements

- .NET 10 SDK
- MonoGame 3.8 (restored via NuGet; the content builder runs as a local dotnet tool)

## Building

```
dotnet build EatMe.sln
```

## Running the server

```
dotnet run --project EatMeServer
```

The server loads `server.cfg` from its output directory, prints the loaded settings, then runs its tick loop until Ctrl+C or `quit`. Type `help` in its window for the console commands; the prompt shows how long the server has been up, as `[up hh.mm.ss]`, and stays on the bottom row with the log scrolling above it, so a busy log never breaks into what you are typing (Up/Down bring back earlier commands, Esc clears the line). If `server.cfg` is missing or invalid, it prints an error and exits with code 1.

### Console commands

Commands are typed into the server's window while it runs. Command names ignore case. Nothing a command changes is written back to `server.cfg`, so a restart goes back to the file's values.

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
| `add bot [n]` | Keeps n more players (default 1; per team with autobalance on). |
| `remove bot [n]` | Keeps n fewer players (default 1; per team with autobalance on). |
| `bots [n]` | Shows or sets MinPlayers. |
| `autobalance [on\|off]` | Shows or sets AutoBalanceTeams. |
| `motd [text]` | Shows or sets the message of the day. |
| `settings` | Lists every setting with the command that changes it and its value. |
| `perf [on\|off]` | Shows or sets perf logging: every 5 seconds, tick rate and times, load, GC, memory, traffic and each step's times. |
| `clear` (or `cls`) | Clears the console. |
| `quit` (or `stop`, `exit`) | Stops the server. |

Every other `server.cfg` setting has its own command, its name in lower case: typed alone it shows the value, typed with a value it changes it, e.g. `pacspeed 7.5`, `mazesize 60`, `tickrate 30`, `servername Friday Night Pac`. `MinPlayers`, `AutoBalanceTeams` and `MessageOfTheDay` are `bots`, `autobalance` and `motd` (`minplayers`, `autobalanceteams` and `messageoftheday` work too). `Port` and `DiscoveryPort` have none, since they only count at startup. `help` lists them all.

| Setting command | Setting |
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

`[n]` is a whole number and `[x]` a number with a decimal point. A value is checked like the file is when it loads: an odd or too small `MazeSize` is refused and the setting keeps its value, and out of range values are clamped (for example `DotRegenPercent` to 0 to 100 and `TickRate` to 1 or more). Most changes count at once, and game rule changes are sent to every connected player. The settings the map is built from (`MazeSize`, `WarpTunnelCount`, `GhostCount`, `PowerPillCount`, `FlagCount`, `PlayerBaseCount` and `ServerSeed`) count from the next level, or at once with `restart`. `StartingLevel` counts on the next `new game`. `RandomLevels` (`on` or `off`) counts from the next level change: with it on, every level after the starting one is picked at random, never the one just played.

### server.cfg

JSON, deserialized into `ServerSettings`. Edit `EatMeServer/server.cfg`; it is copied to the output directory on build. The shipped file lists every setting at its default value. Game rules live in the `GameConfig` block, which maps to `EatMeLibrary.GameConfiguration`; any game setting left out of the file keeps its default.

| Setting | Type | Default | Description |
|---|---|---|---|
| `ServerName` | string | `Eat Me Server` | Display name of the server. |
| `MessageOfTheDay` | string | `Find Friends, Play Games. Visit https://GamrTable.com` | Scrolls across the bottom of each player's title screen after they've played on the server. Empty or missing uses the default. |
| `Port` | int | 7777 | TCP port players connect to. |
| `DiscoveryPort` | int | 7778 | UDP port that answers LAN game searches. |
| `TickRate` | int | 60 | Simulation ticks per second. |
| `ClientUpdatesPerSecond` | int | 30 | Times per second the game state is sent to clients. |
| `GameConfig.MaxPlayers` | int | 128 | Maximum connected players. |
| `GameConfig.MaxScore` | int | 100 | Team score needed to win the level. |
| `GameConfig.FlagScore` | int | 20 | Points for capturing a flag. |
| `GameConfig.FlagRescue` | int | 3 | Points for returning your own dropped flag to base. |
| `GameConfig.PlayerKill` | int | 1 | Points for eating an enemy pac. |
| `GameConfig.GhostKill` | int | 2 | Points for killing a ghost. |
| `GameConfig.MazeSize` | int | 100 | Width and height of the maze in cells. Must be even, minimum 22. |
| `GameConfig.WarpTunnelCount` | int | 4 | Warp tunnels through the outer wall; a hole warps to the hole directly opposite. Symmetry may add one extra. |
| `GameConfig.GhostCount` | int | 6 | Number of ghosts in the maze. Each gets a personality; past the 21 unique ones, the rest are Norm. |
| `GameConfig.PowerPillCount` | int | 4 | Power pills in the maze, split evenly between the two halves. |
| `GameConfig.FlagCount` | int | 1 | Flags per team. |
| `GameConfig.PlayerBaseCount` | int | 1 | Player spawn points per team. |
| `GameConfig.GhostVisionDistance` | int | 10 | Distance within which a ghost spots and chases a pac. |
| `GameConfig.GhostRegenTimeMs` | int | 5000 | ms before a dead ghost reappears. |
| `GameConfig.DotRegenPercent` | int | 50 | Percent (0-100) of a level's dots that stay eaten: once that many are eaten, each dot eaten brings back the oldest eaten one. |
| `GameConfig.PowerPillRegenTimeMs` | int | 5000 | ms before an eaten power pill reappears. |
| `GameConfig.PlayerRespawnTimeMs` | int | 10000 | ms before a dead player respawns at base. |
| `GameConfig.PacMaxSize` | float | 100.0 | The most dots can make a pac (power pills can take it past this). A pac needs at least this size and a power pill working to eat ghosts. |
| `GameConfig.PacSpeed` | float | 6.0 | Speed in map cells per second of a size 0 pac; a pac at max size is 20% faster. |
| `GameConfig.GhostSpeed` | float | 5.0 | Ghost speed in map cells per second. |
| `GameConfig.PowerUpSpeed` | float | 2.5 | Power up speed in map cells per second. |
| `GameConfig.PacShrinkPerSecond` | float | 3.0 | Size a starving pac loses each second. |
| `GameConfig.MsUntilStarvation` | int | 1000 | ms after a pac last ate a dot or power pill before it starts starving (shrinking). |
| `GameConfig.DotSizeGain` | float | 1.0 | Size a pac gains for each dot it eats. |
| `GameConfig.PowerPillSizeGain` | float | 100.0 | Size a pac gains from a power pill, and loses again when it wears off. |
| `GameConfig.PowerUpDurationMs` | int | 10000 | ms every power up effect (a power pill included) lasts. A pac has one effect at a time; a new one replaces it and restarts the timer. |
| `GameConfig.TurnBufferTimeMs` | int | 400 | ms a human pac remembers a direction pressed and let go, so an early tap still turns at the next open junction. 0 is off. |
| `GameConfig.TurnGraceDistance` | float | 0.3 | Cells past an open junction a late turn still happens there. Lag widens it, up to 0.9. 0 is off. |
| `GameConfig.LagCompensationMaxMs` | int | 250 | Most ms of a player's measured lag that widen their late turn window. 0 is off. |

Map seeds aren't a server setting; each level has its own seed in `EatMeLibrary/Objects/Levels.cs`.

### LoadingScreenTips.txt

Loading screen tips. Edit `EatMeServer/LoadingScreenTips.txt`; it is copied to the output directory on build. One tip per line, as many as you like; blank lines are skipped. Every player gets the tips when they join, and one picked at random is shown at the bottom of each intermission screen. A missing file just means no tips.

### Hosting over the Internet

The server listens on all IPv4 interfaces, so nothing in the code needs changing. Players on the same LAN find the game through discovery (UDP `DiscoveryPort`), but players on the Internet must type in an address, and your router has to let them through. Only the TCP game port (`Port`, 7777 by default) needs to be reachable; don't forward the discovery port.

**Port forwarding (for home users):**

1. Find your PC's local IP: run `ipconfig` and take the IPv4 address of your real network adapter (e.g. `192.168.0.3`). 
2. Allow the port through any firewalls. An OS will usually asks the first time the server runs; if set it manually. Here is and an example of how to do it from Windows PowerShell:
   ```powershell
   New-NetFirewallRule -DisplayName "EatMe Server" -Direction Inbound -Protocol TCP -LocalPort 7777 -Action Allow
   ```
3. In the router's port forwarding page, forward to your PC's local IP:
   - Device / IP address: your PC's local IP
   - Starting and ending port: `7777`
   - Protocol: `TCP`
   - Remote IP: all IP addresses
4. Find your public IP (e.g. at whatismyip.com). Players enter it in the Join screen's address box, adding `:port` if you changed `Port` (e.g. `222.0.143.15:7777`).
5. When you're done, delete the forwarding rule.

Your public IP can change, and connecting to it from inside your own network often fails (many routers lack NAT loopback), so test from another network such as a phone hotspot.

**Tunnel (no router changes):** if you can't change the router, or your ISP uses CGNAT (the router's WAN IP differs from the public IP), run a TCP tunnel such as playit.gg pointed at local port 7777. It gives you a `host:port` address that players type into the Join screen. Close the tunnel when you're done.

The protocol has no login, so anyone who knows the address can join while the port is open.

## Running the client

```
dotnet run --project EatMeClient
```

The client runs fullscreen at the desktop resolution. Settings (music volume, sound volume, controller type) are saved to `settings.ini` next to the executable. F1 shows or hides a performance overlay (frame rate, update and draw times, memory, and in a game snapshot rate, traffic and ping).

