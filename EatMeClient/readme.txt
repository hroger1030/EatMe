EAT ME VERSION 0.9 - GAME CLIENT
====================

Eat Me is a multiplayer, retro-style arcade game for LAN and Internet play:
capture-the-flag Pac-Man in a large, randomly generated maze. Two teams, blue
on the left and red on the right, eat dots, grow, eat each other, dodge
ghosts and steal each other's flags.

Eat Me is free and open source. The source code is available at:
https://github.com/hroger1030/EatMe


REQUIREMENTS
------------
- The .NET 10 runtime (https://dotnet.microsoft.com/download).
- An Eat Me server of the same version to join. The version is shown in the
  top right corner of the title screen, and next to each server's name in the
  LAN games list. Servers of a different version are greyed out.


GETTING STARTED
---------------
1. Run the EatMeClient executable. Only one copy runs at a time.
2. Pick Join Game, type your name and pick a team (or Either).
3. Pick a game from the LAN games list, or type a server's address
   (add :port if it doesn't use 7777, e.g. 203.0.113.5:7777).


CONTROLS
--------
                    Keyboard                Gamepad
Move                Arrow keys or WASD      D-pad or left stick
Action (power up)   Space                   A
Menu / pause        Esc                     Back
Menu select         Enter or Space          A
Menu back           Esc                     B or Back

Text boxes (such as your name) are moved between with the arrow keys only,
since W, A, S and D can be typed.


HOW TO PLAY
-----------
- Eat dots to grow. A bigger pac eats a smaller enemy pac, and moves faster.
- Stop eating and you start to starve and shrink.
- Grab the enemy's flag and bring it back to your own flag base to score.
  Touch your own dropped flag to send it home.
- Ghosts kill pacs. A full sized pac with a power pill working can eat them.
- Power ups float through the maze toward the warp tunnels. Eat one for its
  effect; some are used with the action button. You can't carry a flag while
  a power up effect is working.
- The first team to the server's target score wins the level.


SETTINGS
--------
The Settings screen sets music and sound volume, brightness, color bloom,
controls and full screen or windowed display. In a game, Esc opens a menu
with the same settings; the game keeps running while it is open.


FILES
-----
EatMeClient.exe   The game.
Resource.pac      The game's images, sounds, music and fonts.
settings.ini      Your settings, saved next to the exe.
highscores.txt    The best scores from every server you have played on.
SDL2.dll,         Libraries the game needs; keep them next to the exe.
soft_oal.dll


CREDITS
-------
The UI font is JLS Space Gothic, freeware for non-commercial use only.
