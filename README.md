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
- `..\SpatialTrees\SpatialTrees\SpatialTrees.csproj` (quadtree)

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

The server loads `server.cfg` from its output directory, prints the loaded settings, then runs its tick loop until Ctrl+C or `quit`. Type `help` in its window for the other console commands (next level, kick, add bot and so on). If `server.cfg` is missing or invalid, it prints an error and exits with code 1.

### server.cfg

JSON, deserialized into `ServerSettings`. Edit `EatMeServer/server.cfg`; it is copied to the output directory on build. The shipped file lists every setting at its default value. Game rules live in the `GameConfig` block, which maps to `EatMeLibrary.GameConfiguration`; any game setting left out of the file keeps its default.

| Setting | Type | Default | Description |
|---|---|---|---|
| `ServerName` | string | `Eat Me Server` | Display name of the server. |
| `MessageOfTheDay` | string | `Find Friends, Play Games. Visit https://GamrTable.com` | Scrolls across the bottom of each player's title screen after they've played on the server. Empty or missing uses the default. |
| `Port` | int | 7777 | TCP port players connect to. |
| `DiscoveryPort` | int | 7778 | UDP port that answers LAN game searches. |
| `TickRate` | int | 60 | Simulation ticks per second. |
| `TargetFps` | int | 30 | Times per second the game state is sent to clients. |
| `GameConfig.MaxPlayers` | int | 128 | Maximum connected players. |
| `GameConfig.MaxScore` | int | 100 | Team score needed to win the level. |
| `GameConfig.FlagScore` | int | 20 | Points for capturing a flag. |
| `GameConfig.FlagRescue` | int | 3 | Points for returning your own dropped flag to base. |
| `GameConfig.PlayerKill` | int | 1 | Points for eating an enemy pac. |
| `GameConfig.GhostKill` | int | 2 | Points for killing a ghost. |
| `GameConfig.MazeSize` | int | 100 | Width and height of the maze in cells. Must be even, minimum 18. |
| `GameConfig.WarpTunnelCount` | int | 4 | Warp tunnels through the outer wall; a hole warps to the hole directly opposite. Symmetry may add one extra. |
| `GameConfig.GhostCount` | int | 4 | Number of ghosts in the maze. Each gets a personality; past the 21 unique ones, the rest are Norm. |
| `GameConfig.PowerPillCount` | int | 4 | Power pills in the maze, split evenly between the two halves. |
| `GameConfig.FlagCount` | int | 1 | Flags per team. |
| `GameConfig.PlayerBaseCount` | int | 1 | Player spawn points per team. |
| `GameConfig.GhostVisionDistance` | int | 10 | Distance within which a ghost spots and chases a pac. |
| `GameConfig.GhostRegenTimeMs` | int | 5000 | ms before a dead ghost reappears. |
| `GameConfig.DotRegenPercent` | int | 50 | Percent (0-100) of a level's dots that stay eaten: once that many are eaten, each dot eaten brings back the oldest eaten one. |
| `GameConfig.PowerPillRegenTimeMs` | int | 5000 | ms before an eaten power pill reappears. |
| `GameConfig.PlayerRespawnTimeMs` | int | 5000 | ms before a dead player respawns at base. |
| `GameConfig.PacMaxSize` | float | 100.0 | The most dots can make a pac (power pills can take it past this). A pac needs at least this size and a power pill working to eat ghosts. |
| `GameConfig.PacSpeed` | float | 6.0 | Speed in map cells per second of a size 0 pac; a pac at max size is 20% faster. |
| `GameConfig.GhostSpeed` | float | 5.0 | Ghost speed in map cells per second. |
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

**Port forwarding (good for a session or two):**

1. Find your PC's local IP: run `ipconfig` and take the IPv4 address of your real network adapter (e.g. `192.168.0.3`). Ignore `vEthernet` adapters; those are Hyper-V virtual switches. The adapter's Default Gateway (e.g. `192.168.0.1`) is your router's admin page.
2. Allow the port through Windows Firewall. Windows usually asks the first time the server runs; if not, from an admin PowerShell:
   ```powershell
   New-NetFirewallRule -DisplayName "EatMe Server" -Direction Inbound -Protocol TCP -LocalPort 7777 -Action Allow
   ```
3. In the router's port forwarding page, forward to your PC's local IP:
   - Device / IP address: your PC's local IP
   - Starting and ending port: `7777`
   - Protocol: `TCP`
   - Remote IP: all IP addresses
4. Find your public IP (e.g. at whatismyip.com). Players enter it in the Join screen's address box, adding `:port` if you changed `Port` (e.g. `203.0.113.5:7777`).
5. When you're done, delete the forwarding rule.

Your public IP can change, and connecting to it from inside your own network often fails (many routers lack NAT loopback), so test from another network such as a phone hotspot.

**Tunnel (no router changes):** if you can't change the router, or your ISP uses CGNAT (the router's WAN IP differs from the public IP), run a TCP tunnel such as playit.gg pointed at local port 7777. It gives you a `host:port` address that players type into the Join screen. Close the tunnel when you're done.

The protocol has no login, so anyone who knows the address can join while the port is open.

## Running the client

```
dotnet run --project EatMeClient
```

The client runs fullscreen at the desktop resolution. Settings (music volume, sound volume, controller type) are saved to `settings.ini` next to the executable.

## Announcer voice clips

The announcer clips in `EatMeClient/Content/audio` were made with Windows' built-in text-to-speech (`System.Speech`, SAPI) from a throwaway PowerShell script, with the processing written in a small inline C# class (`Add-Type`). The voice is left clean, with no effects. To remake or add one, rebuild the script from these notes.

| File | Says |
|---|---|
| `SoundFX_Msg_RedFlagDropped.wav` | Red flag dropped |
| `SoundFX_Msg_BlueFlagDropped.wav` | Blue flag dropped |
| `SoundFX_Msg_RedFlagReturned.wav` | Red flag returned |
| `SoundFX_Msg_BlueFlagReturned.wav` | Blue flag returned |
| `SoundFX_Msg_RedFlagCaptured.wav` | Red flag captured |
| `SoundFX_Msg_BlueFlagCaptured.wav` | Blue flag captured |
| `SoundFX_Msg_RedAboutToWin.wav` | Red is about to win |
| `SoundFX_Msg_BlueAboutToWin.wav` | Blue is about to win |

For each line, in order:

1. Speak it with a new `SpeechSynthesizer`: voice `Microsoft Zira Desktop`, `Rate = 0`, output to a `MemoryStream` with `SetOutputToAudioStream` at 22,050 Hz, 16-bit mono (`SpeechAudioFormatInfo`).
2. Convert the samples to doubles (-1 to 1).
3. Normalize so the peak is 0.9 of full scale.
4. Trim: after normalizing, cut leading and trailing samples quieter than 0.003 (0.3% of full volume), then keep 60 ms more after the last sound, and fade in and out over 10 ms at each end so the cuts don't click.
5. Write a PCM WAV at 22,050 Hz, 16-bit stereo (each sample written to both channels), matching the other sound effects.

A new clip also needs an entry in `Content.mgcb` (`WavImporter`, `SoundEffectProcessor`), a `SFX_MSG_*` constant in `Const - Audio.cs`, a place in `TitleScreen.LoadAllAssets` and a case in `Announcements.GetSound`.

## Level end stings

`SoundFX_LevelWon.wav` (a "Charge!" fanfare) and `SoundFX_LevelLost.wav` (a sad "wah wah wah waaah") are 8-bit chiptunes synthesised in the style of the NES sound chip, written as 22,050 Hz, 16-bit stereo PCM (the same sample in both channels) and normalized to a 0.9 peak. Every note's volume envelope is stepped to 16 levels like the NES's 4-bit volume.

- **Level won** (about 2.3 s): a noise snare roll (six 50 ms hits), then G4 C5 E5 G5 E5 G5 at 130 ms a beat (beats 1, 1, 1, 2, 1, 6) on a 25% pulse wave, a third below it on a 12.5% pulse (E4 G4 C5 E5 C5 E5), a stepped triangle bass (C3, G2, C3), a short noise tick on each note and a crash on the last, which is held with vibrato and fades out.
- **Level lost** (about 3.1 s): G4, F#4 and F4 for 420 ms each, then E4 for 1.7 s, on a 50% square wave with a triangle bass an octave and a half below (G2 F#2 F2 E2). Each note bends down slightly (0.6 semitones, 1 on the last) and the last one wobbles with vibrato and fades out.

## POW block boom

`SoundFX_Pow.wav` (2 s) is a low, percussive boom with a short room tail, synthesised as 44,100 Hz, 16-bit stereo PCM (the same sample in both channels) and normalized to a 0.9 peak. The dry sound is the sum of six parts:

- a sine whose pitch drops from 120 Hz to 40 Hz (exponential, 60 ms time constant), decaying with a 350 ms time constant, like a big drum
- a 32 Hz sub sine with a 10 ms fade in, decaying over 500 ms
- a mid "thunk", a sine at 0.55 level dropping from 320 Hz to 160 Hz (50 ms time constant) and decaying over 120 ms
- a mid "smack", white noise through a band pass at 600 Hz (Q 1.2) at 1.6 level, decaying over 70 ms
- a burst of white noise put through two one-pole low pass filters (coefficient 0.08) and decaying over 30 ms, for the crack of the hit
- a 180 Hz click decaying over 2 ms, at half level, so the front punches through

The reverb is a Schroeder reverb: four damped comb filters in parallel (29.7, 37.1, 41.1 and 43.7 ms, feedback 0.78, damping 0.35, averaged), then two all pass filters in series (5 ms and 1.7 ms, gain 0.7). It is mixed in at 0.3 under the dry sound, the sum is soft clipped with `tanh(1.8 x)` for weight, and the last 150 ms fade out.
