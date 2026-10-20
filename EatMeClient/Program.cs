using EatMeLibrary;
using System.Threading;

// Only one client may run at a time; a second copy exits before opening a window.
using var instanceMutex = new Mutex(true, Const.CLIENT_INSTANCE_MUTEX_NAME, out bool isFirstInstance);

if (!isFirstInstance)
    return;

// read before the window opens, so it opens straight away in the saved display mode
EatMeClient.ConfigManager.Load();

using var game = new EatMeClient.TitleScreen();
game.Run();
