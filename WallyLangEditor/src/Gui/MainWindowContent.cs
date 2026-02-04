using System;
using System.Linq;
using System.Threading.Tasks;
using BrawlhallaLangReader;
using ImGuiNET;
using NativeFileDialogSharp;
using Raylib_cs;
using WallyLangEditor.Resources.Config;

namespace WallyLangEditor.Gui;

public sealed class MainWindowContent(PathPreferences pathPrefs)
{
    private enum LoadingStateEnum
    {
        None,
        Pending,
        Success,
        Error,
    }

    private readonly record struct LangEntry(string Key, string Text);

    private LangEntry[]? _langEntries = null;
    private LoadingStateEnum _loadingState = LoadingStateEnum.None;
    private string? _errorMessage = null;

    private string _filterKey = "";
    private string _filterText = "";
    private bool _filterTextCaseSensitive = false;

    public void Setup()
    {
        _ = LoadLang();
    }

    public void Gui()
    {
        ImGui.BeginDisabled(_loadingState == LoadingStateEnum.Pending);
        if (ImGui.Button("Select file"))
        {
            DialogResult result = Dialog.FileOpen("bin", pathPrefs.FilePath);
            if (result.IsOk)
            {
                pathPrefs.FilePath = result.Path;
                _ = LoadLang();
            }
        }
        ImGui.EndDisabled();

        if (pathPrefs.FilePath is not null)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(pathPrefs.FilePath);
        }

        switch (_loadingState)
        {
            default:
            case LoadingStateEnum.None:
                ImGui.SameLine();
                ImGui.Text("Please select a file");
                break;
            case LoadingStateEnum.Pending:
                ImGui.SameLine();
                ImGui.Text("Loading...");
                break;
            case LoadingStateEnum.Success:
                break;
            case LoadingStateEnum.Error:
                ImGui.SameLine();
                ImGui.Text("ERROR: ");
                ImGui.SameLine();
                ImGui.Text(_errorMessage);
                break;
        }

        Table();
    }

    private void Table()
    {
        if (_langEntries is null)
            return;

        uint maxTextLength = (uint)_langEntries.Max((s) => s.Text.Length);

        ImGui.SetNextItemWidth(300);
        ImGui.InputText("Filter by string key", ref _filterKey, 256);

        ImGui.Spacing();

        ImGui.SetNextItemWidth(300);
        ImGui.InputText("Filter by text", ref _filterText, 256);
        ImGui.SameLine();
        ImGui.Dummy(new(30, 0));
        ImGui.SameLine();
        ImGui.Checkbox("Case sensitive", ref _filterTextCaseSensitive);

        ImGui.Spacing();

        ImGui.BeginChild("##table", new(0, ImGui.GetWindowHeight() * 0.83f));
        if (ImGui.BeginTable("##entries", 2, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableNextColumn();
            ImGui.TableHeader("String Key");
            ImGui.TableNextColumn();
            ImGui.TableHeader("Text");

            for (int i = 0; i < _langEntries.Length; ++i)
            {
                string key = _langEntries[i].Key;
                string text = _langEntries[i].Text;

                if (!key.Contains(_filterKey, StringComparison.CurrentCultureIgnoreCase))
                    continue;
                if (!text.Contains(_filterText, _filterTextCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase))
                    continue;

                ImGui.PushID(key);
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(key);
                ImGui.TableNextColumn();

                if (ImGui.InputTextMultiline("##text", ref text, maxTextLength, new(ImGui.GetWindowWidth() * 0.6f, 60)))
                {
                    _langEntries[i] = new(key, text);
                }

                ImGui.PopID();
            }

            ImGui.EndTable();
        }
        ImGui.EndChild();
    }

    private async Task LoadLang()
    {
        if (pathPrefs.FilePath is not null)
        {
            _errorMessage = null;
            _loadingState = LoadingStateEnum.Pending;
            try
            {
                LangFile langFile = await LangFile.LoadAsync(pathPrefs.FilePath);
                _langEntries = [.. langFile.Entries.Select((entry) => new LangEntry(entry.Key, entry.Value)).OrderBy((entry) => entry.Key)];
                _loadingState = LoadingStateEnum.Success;
            }
            catch (Exception e)
            {
                _loadingState = LoadingStateEnum.Error;
                _errorMessage = e.Message;
                Rl.TraceLog(TraceLogLevel.Error, e.Message);
                Rl.TraceLog(TraceLogLevel.Trace, e.StackTrace);
            }
        }
    }
}