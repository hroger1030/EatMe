# Eat Me

This is the feature manifest for the SRG client and server application. It will be the design doc.

## What is it 

- It is a multiplayer, retro-style game intended for LAN and Internet play.

- It uses the MonoGame API/framework.
- It has two applications: a headless server and a client application.
- Written in C# 10.
- All times are assumed to be in ms.
 
## Features 
- Supports up to 128 players.
- Retro-style arcade cabinet game with sprite-based graphics.

## Todo
- run optimization checks
- redo logo?
- what is shit like this:         public void UpdatePowerUps(float deltaSeconds, int elapsedMs)
- Rainbow shaders
- sliding on ice level
- alternate wall art
- publishing signing?
- more stun effects in power ups.
- level should have start and end step
- TAB button overlay needs help
- Hammer should be at 45%

## Game Client 
- Dumb game client; relies upon the server to process all packets and messages. The server decides everything that happens; the client only sends the direction the player is holding and draws what the server tells it.
- Bright, colorful graphics.
- Uses retro video game style fonts.
- Edit player profile: name and preferred team are set on the join screen and remembered between games.
- Search for and join local games. The list only shows servers running the same game version.
- Join games via IP address, typed as host or host:port (port defaults to 7777). The last address used is remembered.
- The name and address boxes support cut, copy and paste (Ctrl+X, Ctrl+C, Ctrl+V or Shift+Insert; Command on a Mac). Copy and cut take the whole box. A paste is added on the end, keeping only the characters the box allows, up to its length limit.
- Settings screen with music volume level, sound level, input type and a brightness fader (50% to 100%, default 100%). Brightness dims the whole screen (menus and game) as soon as it is moved, so players can soften the game's bright colours; black stays black. Below it, a colour bloom fader (0% to 100%, default 0%, off) makes the bright colours glow on every screen, also as soon as it is moved. A Display toggle switches between full screen (the default, at the desktop's resolution) and a window 80% of the desktop's size, as soon as it is changed; the window can be maximized or resized. A Pac toggle picks the player's pac from nine shapes (O-gee, Chomper, Hex, Boxie, Trey, Doc, Gears, LadyDi and Go; Chomper is the default), with a small chomping preview of the picked shape drawn to the right of its name; the shape is sent to the server when the player joins a game, so every player sees each pac in the shape its player picked (bots are always Gears), and a change made during a game is sent to the server when the player leaves the settings screen, so every player sees the new shape at once. The settings screen can also be opened during a game from the in-game menu, without leaving the server: the game is hidden and silent while it is open (and while the in-game menu is), the game carries on on the server, and Back returns to the in-game menu.
- The game board is drawn with a sharp bilinear filter: pixel art stays crisp, but scaled, turned and moving sprites get smooth edges. Pacs with Warp Speed (galaxian) or Invincible (starman) leave a short fading speed trail; a power pill leaves none.
- Supports keyboard (WASD-style control mapping or arrow keys) or gamepad controllers.
- Plays an 8-bit style looped soundtrack song.
- Escape / gamepad home button displays the in-game options menu (Continue?/settings menu/quit), on a half-transparent panel with rounded corners.
- tab will display the leader board, showing individual scores of players by team.
- game displays team scores. (color coded) Blue on left, red on right.
- A bar across the bottom of the game shows the player's own pac size on the left and, on the right, their counts of Flags (flag captures), Ghosts (ghosts eaten), Chomps (enemy pacs eaten), Rescues (own flag returned) and Power Ups (power ups eaten). The counts last for as long as the player stays connected.
- A mini-map with a yellow border sits in the top right corner, just under the score bar. It shows the whole play area (but not the maze), with a red or blue outlined box with rounded corners around every red or blue spawn room, a yellow one around the ghost generator, a red or blue dot for every living player (a small Pac-Man facing the way they're going for the player themselves, in the same color as their pac on the board), a small white ghost (a dome on top, two eye holes and a notched skirt) for every living ghost, drawn under the players, a purple cherry for every power up, a grey upside down U on every warp door, and a flag blinking between yellow and the flag's team color wherever each flag is, carried or not. Each flag is drawn once: a carried flag is drawn on its carrier in place of the carrier's dot (over the player's own Pac-Man, which is always drawn). A flag sealed in a force field (triforce) blinks gold instead.
- Every flag, on screen or off and carried or not (except one the player carries), always has a pointer in its team's color (blue for blue flags, red for red ones) on a circle around the player, pointing straight at it, one per flag. The circle reaches from the player to the mini-map's bottom left corner. The pointers float above everything on the board except the mini-map, which is drawn over them, and never cover the score bar or the stats bar.
- The game screen is drawn in layers, bottom to top: background (the board's color and image, scenery that is not part of the game), maze (floors, walls, dots and power pills), items (loose flags, power ups and weapon effects), actors (pacs and ghosts), effects (death animations and wall debris), overhead (things that fly over the walls, such as ghost eyes), HUD (the score and stats bars and the flag pointers) and UI (the mini-map, the player state panel, the respawn countdown and the in-game menu). Nothing is drawn over the UI layer.
- There are sprites in the content images folder for all the game elements except the walls.
- The title menu has Join Game, Game Settings, High Scores and Quit.
- Attract screen: when nothing is pressed on the title menu for 10 seconds (no key, gamepad button, mouse button or mouse move), it fades to black over a second and the attract screen appears. On a black background, top to bottom: the title "Story" and the story text; the title "Enemies" and every ghost in a list sorted alphabetically, spread across the centre third of the screen: its sprite (in its color) at the left edge, its name (in its color) at the right edge, and under them, in a smaller grey font, a short friendly description of how it behaves; the title "Power-Ups" and every power up that can appear (not the power pill) the same way, each with a short friendly description of what it does; and the title "Starring" with a blue Chomper pac facing right and a red pac facing left at the bottom. It starts at the top and scrolls slowly up until the bottom of it is at the bottom of the screen, waits 5 seconds there and goes back to the title menu. Pressing any key, gamepad button or mouse button goes back to the title menu at once. The title music keeps playing.

## Player profile
- Each player can select their player name (1-20 characters long: ASCII letters, digits, '.', '-' and '_').
- Player selects their preferred team: either (the default), red or blue.

## Server 
- Uses Winsock connections: TCP for all gameplay, UDP broadcast only for finding games on the LAN.
- Allows up to 128 players (arbitrary limit).
- Bot players fill empty places. Server setting: MinPlayers (int) defaults to 0 (no bots). The server keeps humans plus bots at MinPlayers (never more than MaxPlayers): a bot leaves when a human joins and one joins when a human leaves. Only humans count toward MaxPlayers and the player count shown in the LAN server list, so a human can always take a bot's place.
- The game pauses while no humans are connected: bots, ghosts, timers and the intermission countdown all stop until a human joins, and play picks up where it left off.
- New bots join the smaller team. When a human joins, a bot leaves the human's preferred team if it has one, so the human can join that team.
- Server setting: AutoBalanceTeams (bool) defaults to true. When on, the server adds or removes as many bots as it takes, whenever a player joins or leaves, so both teams always have the same number of players. Each team gets as many players as the bigger team has humans, and the two teams together make at least MinPlayers (rounded up to an even number). Humans are never moved between teams.
- Bot names are picked at random from bot_names.txt (one per line, blank lines skipped) in the server folder, never repeating a name in use. They don't have to follow the player name rules; the server operator manages the list. With no names left, bots are named Bot1, Bot2 and so on.
- Bots play by the same rules as humans. Each time a bot reaches a cell centre it picks where to go, in this order:
  - Run from ghosts (unless it can kill them), bigger enemies and invincible enemies that are close. A stunned ghost or enemy can't hurt it, so it doesn't run from one.
  - Carrying the enemy flag: take it home. If its own flag isn't at base, get it back first when it's close.
  - Return its own dropped flag when it's close.
  - Chase a smaller enemy carrying its flag.
  - Pick up a dropped enemy flag within 30 steps, if it has no power up effect working (it couldn't pick the flag up otherwise). Every bot does this, attacker or not.
  - Grab a nearby power up within 15 steps when it doesn't have one working.
  - Chase any smaller enemy nearby, or a nearby ghost when it can kill ghosts.
  - Half the bots are attackers and go for the enemy flag; the rest stay to eat and hunt.
  - Eat the nearest dot, unless it is already at max size.
  - Otherwise wander, never turning back unless it has to.
- Bots use every power up that works on the action button, even when it may kill them too: with the white sword, fire flower or BFG 9000 a bot presses it when the nearest thing straight ahead of it, within 10 cells and before any wall, is an enemy pac that isn't invincible or a ghost (never with a teammate in the way), and fires the BFG even when the burst will catch it. With Gotcha it throws poke balls whenever any ghost is alive. With the POW block it presses it when a ghost or an enemy pac that isn't invincible, neither stunned already, is within the effect radius. With the blue shell it fires its shell as soon as there is an enemy alive to send it after. With the bomb it drops its bomb when a ghost or an enemy pac that isn't stunned is within the effect radius. With the banana or tetromino it presses it when a ghost or enemy pac that isn't stunned is within 5 cells (across and down). With the pear it warps away, and with the cherry it drops its decoy, when something that can kill it is within 5 cells. Bots steer clear of holy flames (other than their own) within 2 steps, and a bot passing through things (super leaf) doesn't run from ghosts or enemies, nor chase enemies.
- An invincible bot (starman) goes into chase mode: it runs from nothing, counts every enemy pac (except an invincible one) and every ghost as prey, and chases the nearest one it can find (30 steps), flag carriers first, before rescuing its flag or anything else except taking a flag home. It leaves power pills and power ups alone, since they would replace the effect.
- Bots stick to a plan. A bot keeps the goal it picks (a dot, a pac to chase, the enemy flag and so on) for 2 to 4 seconds, a random time for each goal, before it thinks again, so bots blunder a little like humans and don't change their minds every step. It drops a goal sooner only when the goal is done or gone (the dot is eaten, the prey dies or gets away), or when something urgent comes up: it picks up a flag, loses the one it carries, its own flag is dropped nearby, or an enemy flag it can pick up is dropped nearby.
- Running away always comes first once something that can kill it is close (5 steps). Like a human, a bot takes 150 ms to react to a danger it has just seen, carrying on with what it was doing until then; the same goes for turning back between cell centres. A danger that goes out of sight and comes back takes the full time again. Once running, a bot keeps running for at least a second and until the danger is 8 steps away. After that it keeps out of the area where the danger was (2 cells around it) for 3 seconds, going another way round or after something else, so it doesn't walk straight back into a ghost standing still.
- While running away a bot takes the way furthest from the danger, keeping straight on when two ways are as good. It only turns back when it has no other way or turning back takes it at least 2 steps further from the danger than the best way on, so a bot and a ghost chasing it don't keep flipping each other back and forth.
- Except when running away or chasing something that moves, a bot doesn't turn back while it keeps the same goal.
- Between cell centres a bot turns back at once if it is heading straight at something that can kill it with no wall between them, but not again within half a second.
- So bots don't all do the same thing, at each cell centre a bot with nothing to run from and no flag to carry home has a 15% chance of wandering instead of heading for its target.
- Bots can't judge other pacs' sizes exactly: each bot sees each other pac as up to 10% of PacMaxSize bigger or smaller than it really is, by an amount that stays the same for the whole level and changes each level. A bot may run from a pac it could have eaten, or chase one it can't.
- Game is capture-the-flag Pac-Man in a large, randomly generated maze.
- Players are assigned to the red or blue team according to their preference, but the game auto-balances teams as needed: a player joins their preferred team unless it already has more players than the other team. A player who picked either joins the smaller team (red on a tie). The server remembers each player's preference so it can rebalance teams between levels.
- The server turns a player away if the server is full, their name breaks the name rules, or their game version doesn't match the server's.
- Server settings are stored in the server.cfg file in JSON format.
- Server setting: MaxScore (int) defaults to 100 points.
- Server setting: FlagScore (int) defaults to 20 points.
- Server setting: FlagRescue (int) defaults to 3 points.
- Server setting: PlayerKill (int) defaults to 1 point.
- Server setting: GhostKill (int) defaults to 2 points.
- Server setting: MazeSize (int) defaults to 100. It must be an even number, so the map splits into two equal halves with no center column, and at least 22, which leaves room for the ghost pen, the flag rooms and the spawn rooms. The server refuses to start with an odd or smaller MazeSize.
- Server setting: GhostCount (int) defaults to 6.
- Server setting: ghost vision distance (int) defaults to 10.
- Server setting: ghost regen time (int) defaults to 5000, time in ms before a dead ghost reappears.
- Server setting: dot regen percent (int) defaults to 50, from 0 to 100. Eaten dots are queued oldest first; once that percent of the level's dots has been eaten, each dot eaten makes the oldest eaten dot reappear, so that share of the dots stays eaten. At 0 a dot comes back as soon as it is eaten; at 100 dots never come back.
- Server setting: power pill regen time (int) defaults to 5000, time in ms before a power pill reappears.
- Server setting: player respawn timer (int) defaults to 10000, time in ms before a player respawns at base.
- Server setting: PacMaxSize (float) defaults to 100.0, the most dots can make a pac (power pills can take it past this).
- Server setting: PacSpeed (float) defaults to 6.0, the speed in map cells per second of a size 0 pac.
- Server setting: GhostSpeed (float) defaults to 5.0, ghost speed in map cells per second.
- Server setting: PowerUpSpeed (float) defaults to 2.5, power up speed in map cells per second.
- Server setting: PacShrinkPerSecond (float) defaults to 3.0, the size a starving pac loses each second.
- Server setting: MsUntilStarvation (int) defaults to 1000, time in ms after a pac last ate a dot or power pill before it starts starving.
- Server setting: DotSizeGain (float) defaults to 1.0, the size a pac gains for each dot it eats.
- Server setting: PowerPillSizeGain (float) defaults to 100.0, the size a pac gains from a power pill, and loses again when the pill wears off.
- Server setting: PowerUpDurationMs (int) defaults to 10000, time in ms every power up effect lasts (a power pill is a power up effect).
- Server setting: IntermissionTimeMs (int) defaults to 10000, time in ms the results screen is shown between levels.
- Server setting: PowerUpSpawnTimerMs (int) defaults to 30000, time in ms between power ups, counted from the start of the level. 0 or less means no power ups.
- Server setting: Port (int) defaults to 7777, the TCP port players connect to.
- Server setting: DiscoveryPort (int) defaults to 7778, the UDP port that answers LAN game searches.
- Server setting: TickRate (int) defaults to 60, times per second the server updates the game.
- Server setting: ClientUpdatesPerSecond (int) defaults to 30, times per second the server sends the game state to clients.
- The server console takes typed commands (case doesn't matter). Nothing a command changes is saved to server.cfg.
- The typing prompt, `[up hh.mm.ss] ` (how long the server has been up), stays on the console's bottom row with the line being typed, and everything the server logs scrolls above it, so it never breaks into a half typed command. Left/Right, Home/End, Backspace and Delete edit the line, Up/Down bring back earlier lines, Esc clears it.

| Command | Does |
|---|---|
| `help` | Lists the commands. |
| `status` | The level, both team scores, and how many players and bots there are. |
| `players` | Every player with their id, name, team, bot or human, level score, total score and lag. |
| `next level` | Starts the next level now (a random one with RandomLevels on). |
| `level {n}` | Jumps to level n (1 or more). |
| `restart` | Restarts the current level. |
| `new game` | Goes back to the starting level. |
| `kick {name or id}` | Removes a human from the server; they are told they were kicked. Bots can't be kicked. If two players share the name, kick by id. |
| `add bot [n]` / `remove bot [n]` | Keeps n more or fewer players (humans plus bots, default 1, per team with AutoBalanceTeams on), adding or removing bots to match. |
| `bots [n]` | Shows or sets MinPlayers. |
| `autobalance [on\|off]` | Shows or sets AutoBalanceTeams. |
| `motd [text]` | Shows or sets the message of the day for players who join from then on. |
| `settings` | Lists every setting with the command that changes it and its value. |
| `perf [on\|off]` | Shows or sets perf logging (off at startup). While on, every 5 seconds the server logs ticks a second, the tick's average, 99th percentile and slowest time, its load (the share of the time spent working rather than waiting for the next tick; over 100% it can't keep up with TickRate), garbage collections, memory allocated a second, heap size, bytes sent and received a second, and the average and slowest time of each step of the tick. |
| `clear` | Clears the console. |
| `quit` | Stops the server, as Ctrl+C does. |

- Every other server.cfg setting has its own command, named after the setting in lower case (`servername`, `tickrate`, `clientupdatespersecond`, `startinglevel`, `randomlevels`, `maxplayers`, and one for every GameConfig setting, e.g. `pacspeed`, `mazesize`, `ghostcount`). Typed alone it shows the setting's value; typed with a value it changes it. MinPlayers, AutoBalanceTeams and MessageOfTheDay are `bots`, `autobalance` and `motd`. Port and DiscoveryPort can't be changed, since they only count at startup.
- A setting command checks a value like server.cfg is checked when it loads: a bad MazeSize is refused and the setting keeps its value, and out of range values are clamped. Game rule changes are sent to every player at once. The settings a map is built from (MazeSize, WarpTunnelCount, GhostCount, PowerPillCount, FlagCount, PlayerBaseCount, ServerSeed) count from the next level (or `restart`), so everyone keeps playing the same map until then; StartingLevel counts on the next `new game`; everything else counts at once.

- Server setting: MessageOfTheDay (string) in server.cfg, sent to every player who joins and shown on the scroller at the bottom of their title screen. Empty or missing means the default, "Find Friends, Play Games. Visit https://GamrTable.com".
- Loading screen tips are in the LoadingScreenTips.txt file, one tip per line, any number of tips. Blank lines are skipped. When a player joins, the tips are sent to them, and the intermission screen shows one picked at random at its bottom centre.

## Game rules

This is basically a game of capture the flag. There are numerous notes about it on the internet, along with video game implementations.

- Players have unlimited lives.
- Each team has a score. The first team to X points wins the level.
- Every point a player wins for their team (eating enemy pacs, killing ghosts, rescues, captures) also counts towards that player's own score for the level.
- Each player also has a total score: every point they have won since connecting. It never resets between levels and is shown in the bottom bar.
- At the end of each level, any human player whose total score is in the top 100 goes on the server's high score list (highscores.txt), one entry per player connection, updated as their total grows. Bots never go on the list.
- Each client keeps its own top 100 high scores (its own highscores.txt), built from every list any server sends it, so scores from every server it has played on are kept. It keeps one entry per player name per date, raised when a higher score comes in; a kept score only drops off when 100 higher ones push it out. The High Scores screen shows the client's list, highest first, ranked 1), 2) and so on, in two columns side by side (left to right, then down) that scroll when they don't fit.
- When a level is won, the game pauses for IntermissionTimeMs (10 seconds by default) and shows the results over the winning team's victory picture: which team won, both team scores, and each team's players ranked by their score for the level (blue on the left, red on the right, in as many columns as it takes to fit up to 128 players). Each team's score and player list sit on their own black panel fitted tightly around them. "Next Level in" and a countdown from 10 to 0 sit at the bottom, with a loading screen tip under it; both have a black drop shadow so they read over the picture. The level music stops and an 8-bit chiptune "Charge!" fanfare plays once if the player's team won, or a chiptune sad trumpet if it lost.
- When the countdown reaches 0, the game loads the next level in the level list and team and player scores start again from 0.
- Server setting: RandomLevels (bool) defaults to false. When on, the game still starts on StartingLevel, but every level after it (when a level is won, and the `next level` command) is picked at random from the whole level list, never the level just played.
- Pacs (players) are either red or blue.
- Ghost(s) roam the maze, killing any player they come into contact with. Each ghost has a personality (see Ghost personalities) that decides who it chases and how it moves.
- The normal ghost (Norm): when it gets within N units of a pac, it chases it until it kills it, the pac dies, it dies, or another pac gets closer than the target. It picks flag carriers over all other targets.
- Ghosts never chase a pac with a power pill working. When one is within N units, a ghost runs from it instead, taking the way through the maze that leads furthest from it.
- Ghosts ignore each other and pass through each other.
- When a ghost dies its eyes (the ghost sprite without its body) fly back in a straight line at three times the ghost speed, through walls, to the ghost pen, and circle around it until the ghost respawns, when they disappear. The eyes are only drawn: they don't touch or affect anything in the game.
- Ghosts cannot use the warp tunnels (the holes in the outer wall); to them a tunnel is a wall. Only pacs can warp.
- Ghosts cannot enter spawn tiles of either team; to them a spawn tile is a wall.
- Spawn tiles are walls to the other team's pacs: a pac can only walk on its own team's spawn tiles.
- Each maze cell is either open or a wall.
- Walls are indestructible and cannot be passed through.
- Open spaces have either a dot, a power pill, a bonus item, or nothing.
- Friendly pacs pass through each other.
- Pacs start (and respawn) at size 0. A size 0 pac is one maze cell wide, and every 100 points of size make it one cell wider (a size 100 pac is two cells wide).
- Pacs get bigger by DotSizeGain (1) for each dot they eat.
- A pac that hasn't eaten a dot or power pill for MsUntilStarvation ms is starving, and shrinks by PacShrinkPerSecond (3) each second until it eats again. A pac never shrinks below its starting size (0).
- A pac's own team's spawn room feeds it: while it stands on one of its own spawn tiles it grows at the same rate as eating dots (one DotSizeGain for every cell it would move in that time at its speed, both scaled by its power up effect as for dots), up to max size, and it doesn't get hungry there, even at max size. The enemy's spawn tiles don't feed it, and nor does its own while it has an effect that can't eat (Screw Attack). While the player's own pac is growing this way, the client loops a rising "charging" hum instead of the siren. Any pac growing this way, of either team, blinks purple while it charges.
- Bigger pacs are faster: a pac moves at PacSpeed at size 0, rising evenly to 120% of PacSpeed at max size.
- Only dots, power pills and its own spawn change a pac's size; eating anything else (an enemy pac, a ghost) does not.
- Pacs keep moving in the direction they are going until they hit a wall. A held direction turns the pac at the next cell where that way is open; turning back happens at once.
- Turning is forgiving for human players (bots and ghosts turn exactly):
  - A direction pressed and let go early is remembered for TurnBufferTimeMs (400 ms), so a quick tap still turns the pac at the next open junction.
  - A turn pressed just after the pac passed an open junction still happens: the pac is put back on the junction, turned, and loses no ground. The window is TurnGraceDistance (0.3 cells) past the junction.
  - The server measures each player's lag (a ping every second, averaged) and widens the window by how far their pac travels in that time, counting at most LagCompensationMaxMs (250 ms) of lag. The window is never wider than 0.9 cells.
  - Each of the three settings turns its part off at 0.
- When enemy pacs collide, the bigger pac will eat the smaller pac, sending it back to base.
- Equal-size pacs will bounce off each other.
- Killed players respawn at base after an N ms timeout.
- While the player's own pac is dead, the middle of the board shows "Respawning in n seconds" (counting down in whole seconds, rounded up) in white in the game font, on a half-transparent panel with rounded corners. While an enemy's Lockdown (key) keeps the player's team from respawning it says "Locked out!" instead. It goes away when the pac respawns, and isn't shown while the in-game menu is open.
- Dots can't take a pac past max size (100).
- A power pill adds PowerPillSizeGain (100) to a pac's size, which can take it past max size, and works for PowerUpDurationMs (10000 ms). When it wears off the pac loses PowerPillSizeGain again (never going below 0). A power pill eaten while one is working only restarts the timer; it adds no size. Starving goes on as normal while a pill works.
- Pacs never explode, however big they get.
- A pac pulses white while a power pill is working on it.
- A pac with a power pill working and at least max size kills any ghost it collides with. Any other pac that touches a ghost is killed.
- Touching the enemy flag will pick up the flag, regardless of where it is. A pac touches a flag when the flag's cell overlaps the pac's circle.
- A pac can carry only one flag, and a flag already being carried can't be taken. Dead pacs can't pick up flags.
- A pac with any buff working (a power pill counts) can't pick up the enemy flag. Grabbing a power pill or a power up while carrying the enemy flag drops it where the pac is.
- A carried flag is shown over its carrier's shoulder, drawn on top of the pac, up and behind its centre by half its radius; a flag nobody is carrying is shown where it lies. Both use flag0.png, tinted in the flag's team color.
- If a flag carrier dies, or drops from the server for any reason, the flag drops where they last were.
- A flag can never be on a spawn tile. A carrier who steps onto a spawn tile drops the flag in the cell they came from, and a pac standing on a spawn tile can't pick a flag up.
- Touching your own flag while it is away from your base returns it to your base. This is a flag rescue and scores FlagRescue points for your team. It happens whether or not you are carrying the enemy flag, and you keep any enemy flag you carry.
- Touching your own flag while it is on your base, while carrying the enemy flag, is a flag capture. It scores FlagScore points for your team and sends the enemy flag back to its base. Touching your flag at base without the enemy flag does nothing.
- You can only capture at your own base, with your own flag there. If your flag has been taken, you have to get it back first.
- An announcer voice tells every player, at full volume wherever they are, when a flag is dropped ("Red flag lost"), returned to its base ("Red flag returned") or captured ("Red flag captured" when blue captures the red flag). Each names the flag's team. The player who captured the flag also hears a capture sting just before the announcement; nobody else does. Announcements never talk over each other: they play one at a time in the order they happened, the same one is not repeated while it is still waiting, and one that would be heard more than 3 seconds late is skipped. The level end fanfare cuts off any announcement still playing.
- Sounds from things happening in the maze (a pac or ghost being eaten) are loudest at the middle of the screen and fade towards its edge; anything off screen can't be heard. Flag captures, flags lost and returned, and a team about to win are heard by everyone at full volume.
- The first time in a level a team's score passes 90% of MaxScore (without winning outright), the announcer tells everyone "Red is about to win" or "Blue is about to win". It is said once per team per level.
- Eating a dot or power pill triggers a timer, after N ms, it will respawn again.
- Power ups: every PowerUpSpawnTimerMs ms of play in a level (by default at 30, 60, 90 seconds and so on, until the level ends) a power up appears on one of the map's four power up spawn points, picked at random. It holds one bonus item, picked at random from every item except the power pill (any of them can appear on any level; every maze has its own power pills), and drifts at PowerUpSpeed (2.5 cells a second by default) by the shortest way to a warp hole, also picked at random. If it gets there without being eaten it leaves the level. It can't pass through walls, but it crosses spawn tiles of either team.
- Any pac, of either team, eats a power up by touching it. Eating one plays a short "blorp" (heard like other sounds in the maze) and will give a special effect that depends on the item (see Power up effects). Ghosts ignore power ups.
- Bots go after a power up when it is near (within 10 steps), unless they are running from something, rescuing their flag, chasing a flag carrier, going for a power pill, carrying a flag or invincible.
- Power ups are drawn with their bonus item's sprite, bobbing gently up and down over a shadow that stays at their position, so the bob shows whichever way they move.
- Things effects put in the maze (shots, peels, blocks, the hammer and so on) are two frame animations, each frame showing for 0.25 seconds.
- Power pills and power up items give power up effects (buffs):
  - A buff lasts PowerUpDurationMs (10 seconds). A pac only ever has one: a new buff replaces the one it has, and death removes it.
  - An instant buff has no duration: it does its one thing when the item is eaten and leaves the pac's current buff alone.
  - The standard radius is a square area reaching 15 cells horizontally and vertically from the pac (walls don't block it). Area effects use it, and like the 10 second duration, new effects use it wherever they can.
  - When two effects seem to conflict, the pac's own buff wins over an effect from someone else.
  - Enemy pacs and ghosts killed by an effect count as normal kills: the pac whose effect it was scores the usual points and kill counts. Killing a teammate or itself scores nothing.
- The action button is space on the keyboard (A on a gamepad). Pressing it triggers whatever the pac's buff does on action; with a buff that does nothing on action, or none, it does nothing. After a press that does something, the button does nothing for a second, whatever the buff, so it can't be spammed.
- Holding TAB on the keyboard (either shoulder button on a gamepad) shows the player list over the middle of the board, on a half-transparent panel with rounded corners. It lists every player and bot in the game: blue on the left and red on the right, each team under a heading with its player count ("Blue (4)"), alphabetical (ignoring case), each name in its team's color (the player's own in its lighter own shade) with its ping on the right, or `<bot>` for a bot (`--` for a player not measured yet). The list is what the server sent when the button was pressed; it doesn't change while it is held, and pressing again gets a fresh one. A team too long to fit the board in one column is split into as many columns as it needs, side by side, filled top to bottom then left to right. It isn't shown while paused or during the results.
- Under the mini map is the player state panel, with the same yellow outline. It shows the name of the player's power up effect (at most 12 characters, so it fits), left aligned, above a greyed out meter, with a yellow bar that counts down the time left until the effect runs out. With no effect, the label says "No power up" and the meter is empty. Under the meter it shows the player's own size ("Size n"), also left aligned, then a size bar that fills green from size 0 to 100, turns bright orange above 100 and bright red above 150, and under that a hunger meter that fills up red as the player gets closer to starving (MsUntilStarvation since they last ate). It is full while they shrink and empties whenever they eat a dot or power pill. Under that, "Stunned!" is written centred in a large font, a barely visible dark grey that turns bright red while the player is stunned.
- Each player's name is displayed below their pac.

## Ghost personalities
- Every ghost has a personality: a name, drawn below it in its body color, a body color, and a way of behaving. No two ghosts in a level share a personality, except Norm: once every personality is in use, every other ghost is a Norm, and every Norm is just called Norm.
- Each level's ghosts are picked as part of building its map, so the same server seed and level always give the same ghosts. Odd levels always have a Norm; on even levels Norm is three times as likely to be picked as any other ghost.
- Whatever their personality, all ghosts follow the ghost rules above: they never chase a pac with a power pill working and run from one that comes within their vision, never warp, never enter spawn tiles, and pass through each other.
- On a rainbow level every ghost has the rainbow body instead of its own color (its name is drawn in white), but keeps its behavior.
- The personalities ("vision" is the GhostVisionDistance setting, "speed" GhostSpeed):

| Name | Color | Personality | Behavior |
|------|-------|-------------|----------|
| Norm | Sand | The Normal Ghost | Chases the nearest pac it can see, flag carriers first. |
| Fang | Magenta | The Hunter | Sees 20% further, and never swaps its target for a closer pac, only for a flag carrier. |
| Sly | Hot pink | The Ambusher | Heads for the cell 4 steps ahead of where its target is going, cutting it off. |
| Dither | Pumpkin orange | The Ditherer | Chases until within 6 cells of its target, then runs back to its home corner and comes back. |
| Snitch | Gold | The Flag Hound | Sees flag carriers from twice as far and ignores other pacs unless they are within 3 cells. With no carrier about, it patrols between the flag bases. |
| Warden | Silver | The Guard | Never chases a pac more than 12 cells from its spawn cell, and goes back when it strays that far. |
| Drift | Mint | The Wanderer | 80% speed and half the vision. |
| Zip | Electric yellow-green | The Sprinter | 25% faster, but gives up a target after 4 seconds and rests 2 seconds before picking another. |
| Murk | Deep violet | The Lurker | Waits without moving on a junction until a pac comes in sight, then charges at 115% speed. When it loses the pac it finds another junction to wait on. |
| Brute | Teal | The Bully | Always chases the smallest pac it can see. |
| Glutton | Purple | The Glutton | Always chases the biggest pac it can see. |
| Spite | Coral orange | The Grudge | Hunts the pac that last killed it, from three times as far, until it gets them. |
| Frenzy | Ivory | The Elroy | Starts at 90% speed and speeds up to 130% as the leading team's score nears MaxScore. |
| Dopey | Pastel pink | The Slow Thinker | Heads for where its target was 3 seconds ago. |
| Bonk | Bronze | The Straight Liner | Doesn't find its way; at each junction it takes the way pointing most straight at its target, so walls stop it. |
| Boo | Lilac | The Shy Ghost | Freezes while its target faces it down a clear straight line, and only moves in while it looks away. |
| Mimic | Lemon | The Copycat | Copies the direction of the pac it is watching whenever it can. |
| Snooze | Olive | The Sleeper | Stops dead and naps for 3 seconds out of every 10. |
| Wobble | Raspberry | The Confused | Picks a new random pac in sight at every junction. |
| Dizzy | Peach | The Dizzy Ghost | Wanders the map, picking a random cell and travelling to it, then another. It goes for any pac that comes within 5 cells, and once none is that close it picks a new cell to wander to. |
| Tagalong | Plum | The Sidekick | Follows the nearest other ghost, and joins in when that ghost chases. |

## Power up effects
- Every item's effect is built.
- Eating a power up gives its item's buff, following the buff rules above. Unless it says "instant", it is a normal 10 second buff.
- A smashed wall breaks into its four quarters, which fly up and outward, each on its own arc, spinning, and fall off the bottom of the screen. The pieces are only shown on the client: they never touch anything. A player who joins part way through a level sees no debris from walls smashed before they joined.
- "Stunned" means it can't move until the stun wears off, and spins in place so players can see it. A stunned ghost or pac can't hurt anything: a stunned ghost doesn't kill a pac that touches it, a stunned pac can't eat an enemy (the two pass through each other), and a stunned pac's hammer does nothing. A stunned ghost or pac can still be killed: a pac that can kill ghosts still kills a stunned ghost, and a stunned pac can still be eaten. Shots it fired before the stun still hit. Unless an item says otherwise, a stun lasts 2 seconds; being stunned again starts the stun over.

| Item | Effect | What it does |
|------|--------|--------------|
| Cherry | Cherry Twin | The action button drops a decoy twin of the pac where it stands, which runs off the way the pac came, at the pac's speed, wandering the maze like a pac (it can't enter the enemy spawn or the ghost generator). Only one decoy is out at a time. Every ghost that would chase the pac chases the decoy instead, and enemy bots see it as another copy of the pac. Everyone sees it as an exact copy of the pac (shape, team color, size, name); the owner sees it in the plain team color, not their own lighter one, and enemies see it as a pac dot on their mini map. The first enemy pac or ghost (not stunned) to touch it pops it, hurting nobody. It is gone when the buff ends or the pac dies. |
| Strawberry | Sweet Tooth | Dots give three times as much growth (still never past PacMaxSize). |
| Orange | Vitamin C | Never gets hungry while it works; the hunger meter stays empty. |
| Apple | Doctor Away | Every ghost within the standard radius of the pac dies, for as long as the buff lasts, each a normal ghost kill (points and kill count), respawned ghosts included. Ghosts never chase the pac and run from it when it comes within their vision, as from a power pill. |
| Melon | Melon Belly | Grows by 50 at once (past PacMaxSize if need be) and loses 50 again when the effect ends (never below 0). Never gets hungry while it works; the hunger meter stays empty. |
| Galaxian | Warp Speed | Moves 50% faster and leaves a fading speed trail. |
| Bell | Dinner Bell | Every ghost in the level moves at double speed while the effect works (two pacs with it don't make it faster). |
| Key | Lockdown | Locks the enemy team's spawn: enemy pacs that die can't respawn until the effect ends or the pac with it dies, then they come back as normal. |
| Pretzel | Twister | Every pac (either team) and ghost within the effect radius, other than the pac itself, moves at half speed. Two pacs with it don't make anyone slower still. |
| Pear | Warp | The action button teleports the pac to a random cell with a dot that no pac or ghost is in. |
| Banana | Banana Peel | The action button drops a banana peel on the pac's cell, at most one peel per cell. A peel stays, even after the buff ends, until something touches it: any pac (the one that dropped it once it has stepped off) or ghost, which is stunned. Then the peel is gone. |
| Mushroom | Super Size | The pac's size becomes 300 and it moves through walls, smashing every wall its body covers for good (its reach matches its size, so a 4 cell wide pac wrecks walls up to 2 cells from its centre); each cell becomes an open cell with a dot. The outer wall is never smashed (the pac passes through it and wraps to the other side). Whenever walls are smashed every player hears the POW boom and sees their board shake. It kills any ghost or enemy pac it touches, whatever the sizes, and ghosts touching it don't kill it; an invincible enemy beats it, and it bounces off a screw attacker or another Super Size pac. Weapons still kill it. When the effect ends the pac's size goes back to 0, and if it is inside a wall it is put in the nearest open cell. |
| Power pill | Power Pill | The same as eating a power pill from the maze (see above). |
| Fire flower | Fireball | The pac flashes white rapidly. The action button fires a fireball the way the pac faces, at most one every half second, as many in flight as you like. Fireballs fly straight on much faster than a pac (3.75 times PacSpeed), stop at walls, and kill the first pac (either team, the shooter too once the fireball has left it) or ghost they hit unless it is invincible; either way the fireball is gone. |
| Starman | Invincible | Moves 25% faster, can't be killed, and kills any ghost or enemy pac it touches, whatever its size (scoring as usual). Two invincible enemies bounce off each other. Ghosts run from it as from a power pill. It leaves a fading speed trail and blinks between its team color and yellow. |
| POW block | POW | Each press of the action button stuns every ghost and every pac of either team within the effect radius of the pac, except the pac itself, for 3 seconds. It goes off with a low boom (heard like other sounds in the maze), and every player whose pac is inside the radius, the one who set it off included, sees the board bounce for a moment, like a shockwave. |
| Super leaf | Raccoon Tail | The pac passes through enemy pacs, ghosts and walls (no fights and no ghost contact, either way). Running off the edge of the map wraps to the other side like a warp tunnel. If the buff ends while it is inside a wall, the pac is put in the nearest open cell. |
| Hammer | Hammer Time | A swinging hammer is drawn half a cell past the edge of the pac's body, the way the player is pressing (or the way the pac faces), so it moves out as the pac grows (one cell from the pac's centre at size 0). It kills any enemy pac or ghost it hits, a ghost before the ghost can reach the pac, whatever its size, unless it is invincible, and smashes any wall it hits for good into an open cell with a dot (never the outer wall). It doesn't protect the pac. |
| Triforce | Force Field | When it is eaten, every flag of the pac's team that is away from its base goes home at once, taken off any enemy carrying it; each one is a flag rescue for the pac (FlagRescue points, the Rescues count and the "flag returned" announcement). While the buff works the team's flags are sealed in a force field, a pulsing gold bubble over the flag (its mini map flag blinks gold): enemies can't pick a sealed flag up, and enemy bots leave it alone. The pac's own team still captures onto it as normal. The seal is only about flags; it changes nothing else. |
| Heart container | Full Heal | Size goes up to PacMaxSize (if smaller) and hunger resets when it starts; the pac never starves while it lasts, and its size doesn't change when it ends. |
| White sword | Sword Swing | The action button throws a sword the way the pac faces, at twice PacSpeed, in a straight line; walls stop it. It kills the first thing it hits (any pac, teammates and the thrower too once it has left it, or a ghost) unless that is invincible, and is gone either way. A pac can have at most two swords in flight. |
| Energy tank | Energy Tank | Moves 1.3 times as fast, and dots (and its own spawn) can grow it to twice PacMaxSize (200) instead of PacMaxSize; eating it doesn't change the pac's size. Never gets hungry while it works; the hunger meter stays empty. When it ends (or a new buff replaces it) a pac bigger than PacMaxSize drops to PacMaxSize. |
| Fairy | Reinforcements | While the pac lives, its team has no respawn timer: dead teammates come back at their base at once, Lockdown or not, and each one hears an angelic chorus. |
| Screw attack | Screw Attack | The pac flashes white and spins fast as it moves (half a turn per cell, clockwise moving right or down, counter clockwise moving left or up, still when it stops). It kills any ghost or enemy pac it touches, whatever the sizes, and ghosts touching it don't kill it; an invincible enemy beats it, and it bounces off another screw attacker or a Super Size pac. Weapons still kill it. It can't eat dots or power pills. |
| Holy water | Holy Fire | The pac leaves a trail of blue holy flames, one in each cell it leaves. A flame kills anything that touches it unless it is invincible: enemies, teammates, ghosts, and the pac that laid it once it has moved off. All its flames go out when the buff ends, is replaced, or the pac dies. |
| Spread gun | Spread Shot | Fires a shot up, down, left and right every half second, on its own. Shots fly straight on at 1.5 times PacSpeed; a wall destroys a shot, and the first pac (either team, the shooter too once the shot has left it) or ghost it hits is stunned and the shot is gone. |
| Blue shell | Blue Shell | The action button fires one very fast shell, one shell per buff (eating another Blue Shell gives another) (3 times PacSpeed) that follows the maze to the enemy carrying a flag, or if nobody is, the enemy with the highest score, picking again as that changes. It ignores everything else, and touching its target kills it unless it is invincible; the shell is gone either way, or when there is no enemy alive. |
| Tetromino | Block Drop | The pac leaves a trail of Tetris blocks, one in each cell it leaves, and each acts as a wall until the buff ends. Only a plain open cell can take a block (not a spawn tile, flag base or ghost generator), and a cell with a pac or ghost in it is skipped. Paths aren't worked out again, so things can get stuck or confused for a few seconds. |
| BFG 9000 | BFG 9000 | The action button fires a ball the way the pac faces that travels slowly (ghost speed) until it strikes a pac (the shooter once it has left it), ghost or wall. Then it bursts with a sizzling, crackling electric sound (heard like other sounds in the maze), killing everything within the standard radius that isn't invincible: enemies, teammates, ghosts and the shooter too. |
| Bomb | Bomb | The action button drops a lit bomb on the centre of the pac's cell, one bomb per buff (eating another Bomb gives another). 5 seconds later it explodes, whether or not the buff or the pac is still around, stunning every enemy pac and every ghost within the effect radius for 2 seconds; the pac and its teammates aren't stunned. It goes off with the POW's low boom (heard like other sounds in the maze), and every player whose pac is inside the radius sees the board bounce for a moment. |
| Pepper shaker | Pepper | Ghosts that touch the pac are stunned instead of killing it. |
| Poke Ball | Gotcha | The action button throws a poke ball, at most one a second, as many in flight as you like. A poke ball (as fast as the blue shell) follows the maze to the nearest ghost; with no ghost about it wanders the corridors at random. Every ball still flying is gone when the buff ends; one holding a ghost stays. The first ghost it touches is caught: the ghost is gone and the ball sits still where it was. When anything (any pac or ghost) touches the ball, the ghost comes back out right there and the ball is gone. A "bloop" plays (heard like other sounds in the maze) when it catches a ghost and when the ghost gets out. |

## Levels
- Levels come from a fixed list. Each level has a name, a wall color, a wall fill color and a background color, and may have a background image.
- After the last level in the list, play starts over from the first.
- Power pills appear on every level.
- Level 32 (Pac Paradise) is a rainbow level. Its dots and power pills are in diagonal stripes of the seven rainbow shades (red, orange, yellow, green, blue, indigo, violet), running from the top left corner towards the bottom right and repeating. Its walls have a rainbow gradient running the same way, lined up with the dots, and are hollow (a transparent fill). Behind the maze is a starfield on black, which shows through the walls. Its ghosts are twice the normal size, for catching pacs as well as on screen, and their bodies are a left to right rainbow gradient that scrolls as they move.
- Bonus items aren't tied to levels: every item can appear on every level.
- Level music is not tied to levels. The client plays its music tracks as a looping playlist, one track per level: every normal track in number order, then every "b" track in number order, then back to the first. Adding a track makes the playlist longer. The level's track loops while the level is being played.

## Maps
- The map generator will take in a game seed to generate maps in a deterministic way.
- The map is never sent over the network; each client generates its own copy from the level's seed, which is the server seed (`ServerSeed` in the server config, sent to clients with the game configuration) plus the level number.
- Each level has a color that tints all the walls, and a dark background color (a darker shade of the wall color, its complement, or another color; black is allowed) that fills the board behind the maze. Both colors are passed to clients to render client side.
- Walls are hollow tubes. Each level also has a wall fill color that fills the inside of every wall, joining up across thick blocks; a transparent fill leaves the walls hollow, so whatever is behind the maze shows through. A level may also have a background image, tiled over the background color and scrolling with the maze. The fill color is passed to clients with the other two; the image is known to the client from the level number.
- Each level is symmetrical for both sides. Blue owns the left half and red the right; the right half is either a mirror image or a 180° rotation of the left, picked by the seed.
- Holes in the outer perimeter will warp to the hole on the opposite side.
- Maps should play like Pac-Man, not like a puzzle maze: plenty of intersections so players always have a choice of route. Choices make it fun.
- There are no dead ends, every open cell can be reached, and there are no lone single-cell pillars; walls are at least 3 cells long, and some are thick blocks.
- A map is built in this order: the maze, solid areas, random rooms, the solid outer wall, the warp holes in it, then the special rooms (ghost pen, flag rooms, spawn rooms), the items, and last the power up spawn points.
- Each map has 0 to 4 random rooms per side to make open spaces. A random room is any rectangle from 2x2 to 5x5 cells, never on the outer wall, and joins the maze.
- Each map has 2 to 4 solid areas per side to break up the maze. A solid area is any rectangle from 2x2 to 4x4 cells, never on the outer wall, joined to a wall of the maze, and only placed where it leaves no dead ends, cuts nothing off and keeps at least 2 passages between the halves; one that fits nowhere is left out.
- Special rooms are carved into the finished maze. Whatever maze is around them stays, and if a room touches no open cell a passage is dug to the nearest one, so it always has at least one way in.
- The ghost generator sits in a ghost pen: an open room in the center of the map, one cell bigger all round than the generator (6x6).
- The ghost generator is a 4x4 block of cells in the middle of the pen. Ghosts spawn on any of its cells. Pacs can't enter it, as if it were wall (wall-passing buffs included); the ring of the pen around it is open.
- Each flag base sits in the middle of its own open room in the back quarter of its team's half. A flag room is 3x3 to 5x5 cells (not always square).
- Each team spawns on a 4x4 square of spawn tiles in the middle of its own open 6x6 spawn room in its half, in the back quarter where it fits, so there is always an open ring one cell wide round the spawn tiles. PlayerBaseCount sets how many spawn rooms each team gets.
- Spawn rooms are kept at least one cell clear of flag rooms and the ghost pen, so a flag base is always at least 3 cells from a spawn tile.
- There are two power up spawn points on each half, near the ghost pen: on the left half, the plain dot cells outside the pen nearest its centre above and below its middle, copied to the right half like everything else. They look like normal cells with a dot.
- Dots and power pills on blue's half of the map are drawn in a light blue, and on red's half in a light red.
- Each spawn area (its 4x4 square of spawn tiles, the same size as the ghost generator) is drawn as one image of its team's spawn sprite stretched over the whole area, blinking slowly between two frames (half a second each). The ghost pen floor over the ghost generator is drawn the same way, in its own colors (untinted), as one image stretched over the whole area, blinking between its two frames. The spawn and ghost pen sprites come from base0.png (48x48x6).png, a 2x3 grid with red's two frames on top, blue's in the middle and the ghost pen's at the bottom.

## Performance
- F1 shows or hides the client's performance overlay, on every screen, in the top left corner (under the score bar in a game) on a half-transparent panel with rounded corners. Once a second it shows frames a second with the average and slowest frame time, the average, 99th percentile and slowest update and draw times, garbage collections, memory allocated a second and heap size, and in a game, snapshots received a second, KB received and sent a second and the ping. It starts hidden, and brightness and bloom don't change it.
- Use multiplies over divides.
- Use structs for smaller data structures over classes. in particular ones that are short lived.


## Output and publish
- `~PublishPC.bat` publishes the client and server for Windows (win-x64) to `publish\pc_client` and `publish\pc_server`; `~PublishMac.bat` publishes them for macOS on Apple silicon (osx-arm64) to `publish\mac_client` and `publish\mac_server`. Each deletes its folders first.
- Builds are Release, framework-dependent (players need the .NET runtime installed) and have no `.pdb` files.
- Each app is a single-file executable: the app, its .NET libraries and the runtime settings (`.deps.json`, `.runtimeconfig.json`) are all inside the exe. The client's native SDL and OpenAL libraries stay next to the exe (`SDL2.dll` and `openal.dll` on Windows, `libSDL2-2.0.0.dylib` and `libopenal.dylib` on macOS), because MonoGame only loads them from the exe's folder.
- The client folder holds only the exe, the two native libraries and `Resource.pac`. All the built content, music included, is packed into `Resource.pac`, a plain zip; there is no `Content` folder.
- The client reads each asset from `Resource.pac` when it has one, and from the loose built files otherwise, so normal builds and debugging use loose files.
- The server folder holds only the exe, `server.cfg`, `LoadingScreenTips.txt`, `bot_names.txt` and `highscores.txt`.
- Both apps read and write their files (settings, high scores, server config) next to the exe.
- `~StartClient.bat` and `~StartServer.bat` run the published Windows builds.
