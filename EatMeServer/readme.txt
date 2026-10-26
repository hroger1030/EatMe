EAT ME VERSION 0.92 - GAME SERVER
====================

The headless server for Eat Me, a multiplayer capture-the-flag Pac-Man game.
Players join it with the Eat Me client; the client and server versions must
match.

Eat Me is free and open source. The source code is available at:
https://github.com/hroger1030/EatMe

REQUIREMENTS
------------
- The .NET 10 runtime (https://dotnet.microsoft.com/download).


RUNNING
-------
Run the EatMeServer executable from this folder. It reads server.cfg, prints
the settings it loaded, then runs until you type quit (or press Ctrl+C).
If server.cfg is missing or invalid the server prints an error and stops.

While no players are connected the game is paused. Bots fill the teams up to
MinPlayers.


CONSOLE COMMANDS
----------------
Type commands into the server's window. Type help for the full list.

help (or ?)           List the commands.
status                Level, scores and player counts.
players               Every player with their id, team, scores and lag.
next level (or next)  Start the next level now.
level n               Jump to level n.
restart               Restart the current level.
new game              Go back to the starting level.
kick name-or-id       Remove a player.
add bot [n]           Keep n more players (default 1).
remove bot [n]        Keep n fewer players (default 1).
bots [n]              Show or set MinPlayers.
autobalance [on|off]  Show or set AutoBalanceTeams.
motd [text]           Show or set the message of the day.
settings              List every setting and its value.
perf [on|off]         Show or set logging performance every 5 seconds.
clear (or cls)        Clear the console.
quit (or stop, exit)  Stop the server.

Every other server.cfg setting (except Port and DiscoveryPort) has its own
command, its name in lower case: typed alone it shows the value, typed with a
value it changes it, e.g. pacspeed 7.5 or servername Friday Night Pac.
Nothing typed is saved back to server.cfg, so a restart goes back to the
file's values. Map settings take effect from the next level.


FILES
-----
EatMeServer.exe          The server.
server.cfg               Settings (JSON). Game rules are in the GameConfig
                         block. Edit it while the server is stopped.
LoadingScreenTips.txt    One tip per line; one is shown to players between
                         levels.
bot_names.txt            Names given to bots, one per line.
highscores.txt           The server's high score table (Name,Score,Date per
                         line). The server rewrites it when it changes.


NETWORK
-------
Port           TCP 7777 by default. Players connect on it.
DiscoveryPort  UDP 7778 by default. Answers LAN game searches.

Players on your LAN find the game in their LAN games list. For players on the
Internet, forward the TCP game port (only that one) on your router to this PC
and allow it through the firewall, then give them your public IP address.
There is no login, so anyone who knows the address can join while the port
is open.
