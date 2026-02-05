using System.Numerics;
using Hexa.NET.ImGui;
using Raylib_cs;
using rlImGui_cs;
using WallyLangEditor.Logging;
using WallyLangEditor.Resources.Config;
using WallyLangEditor.Resources.Gui;

namespace WallyLangEditor.Gui;

public sealed class MainWindow(PathPreferences pathPrefs)
{
    public const string WINDOW_NAME = nameof(WallyLangEditor);
    public const int INITIAL_SCREEN_WIDTH = 1280;
    public const int INITIAL_SCREEN_HEIGHT = 720;

    private readonly MainWindowContent _content = new(pathPrefs);

    public void Run()
    {
        Setup();

        while (!Rl.WindowShouldClose())
        {
            Draw();
        }

        pathPrefs.Save();

        rlImGui.Shutdown();
        Rl.CloseWindow();
    }

    private void Setup()
    {
        LogCallback.Init();

        Rl.SetConfigFlags(ConfigFlags.ResizableWindow | ConfigFlags.MaximizedWindow);
        Rl.InitWindow(INITIAL_SCREEN_WIDTH, INITIAL_SCREEN_HEIGHT, WINDOW_NAME);
        Rl.MaximizeWindow();

        Rl.SetExitKey(KeyboardKey.Null);
        rlImGui.Setup(true, false);
        Style.Apply();

        _content.Setup();
    }

    private void Draw()
    {
        Rl.BeginDrawing();
        Rl.ClearBackground(RlColor.Black);
        rlImGui.Begin();

        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowViewport(viewport.ID);
        ImGui.SetNextWindowPos(viewport.Pos);
        ImGui.SetNextWindowSize(viewport.Size);
        bool open = true;
        ImGui.Begin("##root", ref open, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(30, 10));
        ImGui.BeginChild("##content");

        _content.Gui();

        ImGui.EndChild();
        ImGui.PopStyleVar();
        ImGui.End();

        Rl.EndMode2D();
        Rl.EndTextureMode();
        rlImGui.End();
        Rl.EndDrawing();
    }
}