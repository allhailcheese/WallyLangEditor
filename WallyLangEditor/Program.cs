global using Rl = Raylib_cs.Raylib;
global using RlColor = Raylib_cs.Color;
global using RlImage = Raylib_cs.Image;
using WallyLangEditor.Gui;
using WallyLangEditor.Resources.Config;

PathPreferences prefs = await PathPreferences.Load();
MainWindow mainWindow = new(prefs);
mainWindow.Run();