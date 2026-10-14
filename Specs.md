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

## Game Client 
- Dumb game client; relies upon the server to process all packets and messages. The server decides everything that happens; the client only sends the direction the player is holding and draws what the server tells it.
- Bright, colorful graphics.
- Uses retro video game style fonts.
- Edit player profile: name and preferred team are set on the join screen and remembered between games.
- Search for and join local games. The list only shows servers running the same game version.
- Join games via IP address, typed as host or host:port (port defaults to 7777). The last address used is remembered.
- Settings screen with music volume level, sound level, input type and a brightness fader (50% to 100%, default 100%). Brightness dims the whole screen (menus and game) as soon as it is moved, so players can soften the game's bright colours; black stays black. Below it, a colour bloom fader (0% to 100%, default 0%, off) makes the bright colours glow on every screen, also as soon as it is moved. The settings screen can also be opened during a game from the in-game menu, without leaving the server: the game is hidden and silent while it is open (and while the in-game menu is), the game carries on on the server, and Back returns to the in-game menu.
- The game board is drawn with a sharp bilinear filter: pixel art stays crisp, but scaled, turned and moving sprites get smooth edges. Pacs with Sugar Rush (cherry) leave a short fading speed trail; a power pill leaves none.
- Supports keyboard (WASD-style control mapping or arrow keys) or gamepad controllers.
- Plays an 8-bit style looped soundtrack song.
- Escape / gamepad home button displays the in-game options menu (Continue?/settings menu/quit).
- tab will display the leader board, showing individual scores of players by team.
- game displays team scores. (color coded) Blue on left, red on right.
- A bar across the bottom of the game shows the player's own pac size on the left and, on the right, their counts of Flags (flag captures), Ghosts (ghosts eaten), Chomps (enemy pacs eaten) and Rescues (own flag returned). The counts last for as long as the player stays connected.
- A mini-map with a yellow border sits in the top right corner, just under the score bar. It shows the whole play area (but not the maze), with a red or blue dot for every living player (a star for the player themselves), a purple X for every power up, a grey upside down U on every warp door, and a dot blinking between yellow and the flag's team color wherever each flag is, carried or not.
- Every flag, on screen or off and carried or not (except one the player carries), always has a pointer in its team's color (blue for blue flags, red for red ones) on a circle around the player, pointing straight at it, one per flag. The circle reaches from the player to the mini-map's bottom left corner. The pointers float above everything on the board except the mini-map, which is drawn over them, and never cover the score bar or the stats bar.
- There are sprites in the content images folder for all the game elements except the walls.

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
  - Run from ghosts (unless it can kill them) and bigger enemies that are close.
  - Carrying the enemy flag: take it home. If its own flag isn't at base, get it back first when it's close.
  - Return its own dropped flag when it's close.
  - Chase a smaller enemy carrying its flag, then any smaller enemy nearby, or a nearby ghost when it can kill ghosts.
  - Grab a nearby power pill when it doesn't have one working.
  - Half the bots are attackers and go for the enemy flag; the rest stay to eat and hunt.
  - Eat the nearest dot, unless it is already at max size.
  - Otherwise wander, never turning back unless it has to.
- Bots stick to a plan. A bot keeps the goal it picks (a dot, a pac to chase, the enemy flag and so on) for 2 to 4 seconds, a random time for each goal, before it thinks again, so bots blunder a little like humans and don't change their minds every step. It drops a goal sooner only when the goal is done or gone (the dot is eaten, the prey dies or gets away), or when something urgent comes up: it picks up a flag, loses the one it carries, or its own flag is dropped nearby.
- Running away always comes first, the moment something that can kill it is close (5 steps). Once running, a bot keeps running for at least a second and until the danger is 8 steps away. After that it keeps out of the area where the danger was (2 cells around it) for 3 seconds, going another way round or after something else, so it doesn't walk straight back into a ghost standing still.
- Except when running away or chasing something that moves, a bot doesn't turn back while it keeps the same goal.
- Between cell centres a bot turns back at once if it is heading straight at something that can kill it, but not again within half a second.
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
- Server setting: MazeSize (int) defaults to 100. It must be an even number, so the map splits into two equal halves with no center column, and at least 18, which leaves room for the ghost pen and the flag rooms. The server refuses to start with an odd or smaller MazeSize.
- Server setting: GhostCount (int) defaults to 4.
- Server setting: ghost vision distance (int) defaults to 10.
- Server setting: ghost regen time (int) defaults to 5000, time in ms before a dead ghost reappears.
- Server setting: dot regen percent (int) defaults to 50, from 0 to 100. Eaten dots are queued oldest first; once that percent of the level's dots has been eaten, each dot eaten makes the oldest eaten dot reappear, so that share of the dots stays eaten. At 0 a dot comes back as soon as it is eaten; at 100 dots never come back.
- Server setting: power pill regen time (int) defaults to 5000, time in ms before a power pill reappears.
- Server setting: player respawn timer (int) defaults to 5000, time in ms before a player respawns at base.
- Server setting: PacMaxSize (float) defaults to 100.0, the most dots can make a pac (power pills can take it past this).
- Server setting: PacSpeed (float) defaults to 6.0, the speed in map cells per second of a size 0 pac.
- Server setting: GhostSpeed (float) defaults to 5.0, ghost speed in map cells per second.
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
- Server setting: TargetFps (int) defaults to 30, times per second the server sends the game state to clients.
- Server accepts command line input. (start a new game, stop current game, kick player, ban player)
- Server setting: MessageOfTheDay (string) in server.cfg, sent to every player who joins and shown on the scroller at the bottom of their title screen. Empty or missing means the default, "Find Friends, Play Games. Visit https://GamrTable.com".
- Loading screen tips are in the LoadingScreenTips.txt file, one tip per line, any number of tips. Blank lines are skipped. When a player joins, the tips are sent to them, and the intermission screen shows one picked at random at its bottom centre.
- kicked players get their name/ip added to banned.txt. 
- players on the banned.txt list cannot join the server.

## Game rules

This is basically a game of capture the flag. There are numerous notes about it on the internet, along with video game implementations.

- Players have unlimited lives.
- Each team has a score. The first team to X points wins the level.
- Every point a player wins for their team (eating enemy pacs, killing ghosts, rescues, captures) also counts towards that player's own score for the level.
- Each player also has a total score: every point they have won since connecting. It never resets between levels and is shown in the bottom bar.
- At the end of each level, any human player whose total score is in the top 100 goes on the server's high score list (highscores.txt), one entry per player connection, updated as their total grows. Bots never go on the list.
- Each client keeps its own top 100 high scores (its own highscores.txt), built from every list any server sends it, so scores from every server it has played on are kept. It keeps one entry per player name per date, raised when a higher score comes in; a kept score only drops off when 100 higher ones push it out. The High Scores screen shows the client's list, highest first, ranked 1), 2) and so on, in two columns side by side (left to right, then down) that scroll when they don't fit.
- When a level is won, the game pauses for IntermissionTimeMs (10 seconds by default) and shows the results over the winning team's victory picture: which team won, both team scores, and each team's players ranked by their score for the level (blue on the left, red on the right, in as many columns as it takes to fit up to 128 players). Each team's score and player list sit on their own black panel fitted tightly around them. "Next Level in" and a countdown from 10 to 0 sit at the bottom. The level music stops and an 8-bit chiptune "Charge!" fanfare plays once if the player's team won, or a chiptune sad trumpet if it lost.
- When the countdown reaches 0, the game loads the next level in the level list and team and player scores start again from 0.
- Pacs (players) are either red or blue.
- Ghost(s) roam the maze, killing any player they come into contact with. Each ghost has a personality (see Ghost personalities) that decides who it chases and how it moves.
- The normal ghost (Norm): when it gets within N units of a pac, it chases it until it kills it, the pac dies, it dies, or another pac gets closer than the target. It picks flag carriers over all other targets.
- Ghosts never chase a pac with a power pill working. When one is within N units, a ghost runs from it instead, taking the way through the maze that leads furthest from it.
- Ghosts ignore each other and pass through each other.
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
- Bigger pacs are faster: a pac moves at PacSpeed at size 0, rising evenly to 120% of PacSpeed at max size.
- Only dots and power pills change a pac's size; eating anything else (an enemy pac, a ghost) does not.
- Pacs keep moving in the direction they are going until they hit a wall. A held direction turns the pac at the next cell where that way is open; turning back happens at once.
- Turning is forgiving for human players (bots and ghosts turn exactly):
  - A direction pressed and let go early is remembered for TurnBufferTimeMs (400 ms), so a quick tap still turns the pac at the next open junction.
  - A turn pressed just after the pac passed an open junction still happens: the pac is put back on the junction, turned, and loses no ground. The window is TurnGraceDistance (0.3 cells) past the junction.
  - The server measures each player's lag (a ping every second, averaged) and widens the window by how far their pac travels in that time, counting at most LagCompensationMaxMs (250 ms) of lag. The window is never wider than 0.9 cells.
  - Each of the three settings turns its part off at 0.
- When enemy pacs collide, the bigger pac will eat the smaller pac, sending it back to base.
- Equal-size pacs will bounce off each other.
- Killed players respawn at base after an N ms timeout.
- Dots can't take a pac past max size (100).
- A power pill adds PowerPillSizeGain (100) to a pac's size, which can take it past max size, and works for PowerUpDurationMs (10000 ms). When it wears off the pac loses PowerPillSizeGain again (never going below 0). A power pill eaten while one is working only restarts the timer; it adds no size. Starving goes on as normal while a pill works.
- Pacs never explode, however big they get.
- A pac pulses white while a power pill is working on it.
- A pac with a power pill working and at least max size kills any ghost it collides with. Any other pac that touches a ghost is killed.
- Touching the enemy flag will pick up the flag, regardless of where it is. A pac touches a flag when the flag's cell overlaps the pac's circle.
- A pac can carry only one flag, and a flag already being carried can't be taken. Dead pacs can't pick up flags.
- A carried flag is shown over its carrier's shoulder, drawn on top of the pac, up and behind its centre by half its radius; a flag nobody is carrying is shown where it lies. Both use flag0.png, tinted in the flag's team color.
- If a flag carrier dies, or drops from the server for any reason, the flag drops where they last were.
- A flag can never be on a spawn tile. A carrier who steps onto a spawn tile drops the flag in the cell they came from, and a pac standing on a spawn tile can't pick a flag up.
- Touching your own flag while it is away from your base returns it to your base. This is a flag rescue and scores FlagRescue points for your team. It happens whether or not you are carrying the enemy flag, and you keep any enemy flag you carry.
- Touching your own flag while it is on your base, while carrying the enemy flag, is a flag capture. It scores FlagScore points for your team and sends the enemy flag back to its base. Touching your flag at base without the enemy flag does nothing.
- You can only capture at your own base, with your own flag there. If your flag has been taken, you have to get it back first.
- An announcer voice tells every player, at full volume wherever they are, when a flag is dropped ("Red flag lost"), returned to its base ("Red flag returned") or captured ("Red flag captured" when blue captures the red flag). Each names the flag's team. Announcements never talk over each other: they play one at a time in the order they happened, the same one is not repeated while it is still waiting, and one that would be heard more than 3 seconds late is skipped. The level end fanfare cuts off any announcement still playing.
- Sounds from things happening in the maze (a pac or ghost being eaten) are loudest at the middle of the screen and fade towards its edge; anything off screen can't be heard. Flag captures, flags lost and returned, and a team about to win are heard by everyone at full volume.
- The first time in a level a team's score passes 90% of MaxScore (without winning outright), the announcer tells everyone "Red is about to win" or "Blue is about to win". It is said once per team per level.
- Eating a dot or power pill triggers a timer, after N ms, it will respawn again.
- Power ups: every PowerUpSpawnTimerMs ms of play in a level (by default at 30, 60, 90 seconds and so on, until the level ends) a power up appears on one of the map's four power up spawn points, picked at random. It holds one of the level's bonus items, picked at random, and drifts slowly (3 cells a second) by the shortest way to a warp hole, also picked at random. If it gets there without being eaten it leaves the level. It can't pass through walls, but it crosses spawn tiles of either team.
- Any pac, of either team, eats a power up by touching it. Eating one plays a short "blorp" (heard like other sounds in the maze) and will give a special effect that depends on the item (see Power up effects). Ghosts ignore power ups.
- Bots go after a power up when it is near (within 10 steps), unless they are running from something, rescuing their flag, chasing a flag carrier or carrying a flag.
- Power ups are drawn with their bonus item's sprite, bobbing gently up and down as they move.
- Power pills and power up items give power up effects. A pac has only one effect at a time: a new one replaces the one it has and restarts the timer. An effect normally lasts PowerUpDurationMs (10 seconds) and is lost when it runs out or the pac dies. A permanent effect never runs out (its meter stays full) and is lost only when the pac dies, a new level starts, or another effect replaces it.
- Under the mini map is the player state panel, with the same yellow outline. It shows "Power Up:" and the name of the player's power up effect, left aligned, above a greyed out meter, with a yellow bar that counts down the time left until the effect runs out. With no effect, the label says "Power Up: None" and the meter is empty. Under the meter it shows the player's own size ("Size n"), also left aligned, and under that a hunger meter that fills up red as the player gets closer to starving (MsUntilStarvation since they last ate). It is full while they shrink and empties whenever they eat a dot or power pill.
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
| Fang | Lime green | The Hunter | Sees 20% further, and never swaps its target for a closer pac, only for a flag carrier. |
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
| Wobble | Forest green | The Confused | Picks a new random pac in sight at every junction. |
| Panic | Peach | The Scaredy Ghost | Runs from every pac it can see, powered or not, and chases nobody. It still kills any pac that touches it. |
| Tagalong | Plum | The Sidekick | Follows the nearest other ghost, and joins in when that ghost chases. |

## Power up effects
- Draft: only the effects of the cherry, strawberry, orange, apple, melon, galaxian, bell, key and POW block are built; the rest are placeholders.
- Eating a power up gives its item's effect. Unless it says "instant" or "permanent", an effect lasts PowerUpDurationMs (10 seconds), is lost on death, and follows the power up effect rules above (one at a time, a new one replaces the old). An instant effect happens once, when the item is eaten, and leaves the pac's current effect alone.
- "Stunned" means it can't move until the stun wears off, and spins in place so players can see it. Nothing else changes: a stunned ghost still kills a pac that runs into it, and a stunned pac can still be eaten. Unless an item says otherwise, a stun lasts 2 seconds; being stunned again starts the stun over.

| Item | Effect | What it does |
|------|--------|--------------|
| Cherry | Sugar Rush | Moves 50% faster and leaves a fading speed trail. |
| Strawberry | Sweet Tooth | Dots give three times as much growth (still never past PacMaxSize). |
| Orange | Vitamin C | Permanent. Never starves; the hunger meter stays empty. |
| Apple | Doctor Away | Permanent. Ghosts never chase the pac and run from it when it comes within their vision, as from a power pill, but it can't kill them, and touching one still kills it. |
| Melon | Melon Belly | Grows by 50 at once (past PacMaxSize if need be) and loses 50 again when the effect ends (never below 0). Never gets hungry while it works; the hunger meter stays empty. |
| Galaxian | Rocket Science | Every point the pac scores counts double, for the pac and for its team. |
| Bell | Dinner Bell | Every ghost in the level moves at double speed while the effect works (two pacs with it don't make it faster). |
| Key | Lockdown | Locks the enemy team's spawn: enemy pacs that die can't respawn until the effect ends or the pac with it dies, then they come back as normal. |
| Pretzel | Twister | Ghosts within vision move at half speed. |
| Pear | Shield | The first ghost to touch the pac doesn't kill it; the ghost is sent back to its spawn and the effect ends. |
| Banana | Banana Peel | Instant: drops a peel on the pac's cell. The first enemy pac or ghost to cross it is stunned. |
| Mushroom | Super Size | Sets size = 300, and lets the pac move through walls. sets size back to starting value when effect is over. |
| 1-up mushroom | Extra Life | The next time the pac would die it doesn't; it drops any flag it carries, and the effect ends. |
| Fire flower | Fireball | Throws a fireball straight ahead every 2 seconds; it kills the first ghost or player it hits and stops at walls. |
| Starman | Invincible | Moves 30% faster and kills any ghost or enemy pac it touches, whatever its size. |
| POW block | POW | Instant: stuns every ghost and every pac of either team within 15 cells (straight line) of the pac, except the pac itself, for 3 seconds. |
| Super leaf | Raccoon Tail | Passes through ghosts unharmed. |
| Hammer | Hammer Time | Kills anything it touches at any size. Hammer animation is played. |
| Triforce | Triforce | Works as a power pill, and the pac moves 20% faster. |
| Heart container | Full Heal | Instant: size goes up to PacMaxSize (if smaller) and hunger resets. |
| White sword | Sword Swing | Space bar throws a white sword straight ahead and it kills anything it hits. |
| Energy tank | Energy Tank | Max size is now 150 until killed. |
| Morph ball | Morph Ball | Shrinks to size 0 and moves 50% faster; ghosts cannot see it. Its size comes back when the effect ends. |
| Screw attack | Screw Attack | Kills any ghost or enemy pac it touches, but can't eat dots or pick up flags. |
| Holy water | Holy Fire | Leaves a trail of flames, each lasting 10 seconds, that kill anything it touches. |
| Spread gun | Spread Shot | Fires three shots ahead, straight and to each side, every second; each stuns the first ghost or enemy pac it hits. |
| Blue shell | Blue Shell | Instant: flies after and kills the enemy flag carrier, or if there is none, the enemy pac with the highest score. |
| Tetromino | Block Drop | Instant: fills the cell behind the pac with a wall for 5 seconds. |
| BFG 9000 | BFG 9000 | Instant: kills every enemy on the player's screen. |
| Mega Man 1-up | Reinforcements | Instant: every dead pac on the pac's team respawns at once. |
| Pepper shaker | Pepper | Ghosts that touch the pac are stunned instead of killing it. |
| Poke Ball | Gotcha | All ghosts follow the pac, until the effect ends. |

## Levels
- Levels come from a fixed list. Each level has a name, a wall color, a wall fill color, a background color, and a set of bonus items, and may have a background image.
- After the last level in the list, play starts over from the first.
- Power pills appear on every level.
- Level 32 (Pac Paradise) is a rainbow level. Its dots and power pills are in diagonal stripes of the seven rainbow shades (red, orange, yellow, green, blue, indigo, violet), running from the top left corner towards the bottom right and repeating. Its walls have a rainbow gradient running the same way, lined up with the dots, and are hollow (a transparent fill). Behind the maze is a starfield on black, which shows through the walls. Its ghosts are twice the normal size, for catching pacs as well as on screen, and their bodies are a left to right rainbow gradient that scrolls as they move.
- There is one bonus item for each level: every level introduces exactly one item no earlier level has. Later levels also bring back items from earlier levels, so they have more of them; the last level has every item.
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
- A map is built in this order: the maze, random rooms, the solid outer wall, the warp holes in it, then the special rooms (ghost pen, flag rooms, spawn rooms), the items, and last the power up spawn points.
- Each map has 0 to 4 random rooms per side to make open spaces. A random room is any rectangle from 2x2 to 5x5 cells, never on the outer wall, and joins the maze.
- Special rooms are carved into the finished maze. Whatever maze is around them stays, and if a room touches no open cell a passage is dug to the nearest one, so it always has at least one way in.
- The ghost generator sits in a ghost pen: an open room in the center of the map, one cell bigger all round than the generator (4x4).
- The ghost generator is a 2x2 block of cells in the middle of the pen. Ghosts spawn on any of its cells.
- Each flag base sits in the middle of its own open room in the back quarter of its team's half. A flag room is 3x3 to 5x5 cells (not always square).
- Each team spawns in its own open 3x3 room of spawn tiles in its half, in the back quarter where it fits. PlayerBaseCount sets how many spawn rooms each team gets.
- Spawn rooms are kept at least one cell clear of flag rooms and the ghost pen, so a flag base is always at least 3 cells from a spawn tile.
- There are two power up spawn points on each half, near the ghost pen: on the left half, the plain dot cells outside the pen nearest its centre above and below its middle, copied to the right half like everything else. They look like normal cells with a dot.
- The ghost pen floor is drawn in yellow, and each spawn room's floor in its team's color (the pen uses GhostBase0.png, the spawn rooms Pacbase0.png).
- Dots and power pills on blue's half of the map are drawn in a light blue, and on red's half in a light red.
- About 28-30% of open cells are junctions (3 or 4 ways out). This is fixed in the shared code, not a server setting, so every client builds the same map as the server.

## Performance
- Use multiplies over divides.
- Use structs for smaller data structures over classes. in particular ones that are short lived.
