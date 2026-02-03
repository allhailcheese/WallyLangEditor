global using Rl = Raylib_cs.Raylib;
global using RlColor = Raylib_cs.Color;
using WallyLangEditor.Gui;
using WallyLangEditor.Resources.Config;

PathPreferences prefs = await PathPreferences.Load();
MainWindow mainWindow = new(prefs);
mainWindow.Run();