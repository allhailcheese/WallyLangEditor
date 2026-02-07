using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using BrawlhallaLangReader;
using Hexa.NET.ImGui;
using NativeFileDialogSharp;
using Raylib_cs;
using WallyLangEditor.Resources.Config;

namespace WallyLangEditor.Gui;

public sealed class MainWindowContent(PathPreferences pathPrefs)
{
    private const uint maxTextLength = 4096;
    private enum LoadingStateEnum
    {
        None,
        Pending,
        Success,
        Error,
    }

    private SortedDictionary<string, string>? _langEntries = null;
    private LoadingStateEnum _loadingState = LoadingStateEnum.None;
    private string? _loadErrorMessage = null;
    private LoadingStateEnum _savingState = LoadingStateEnum.None;
    private string? _saveErrorMessage = null;

    public void Setup()
    {
        _ = LoadLang();
    }

    public void Gui()
    {
        if (ImGui.TreeNodeEx("Saving&Loading"u8, _langEntries is null ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None))
        {
            LoadButton();
            SaveButton();
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Filters"u8))
        {
            Filters();
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Add new entry"u8))
        {
            AddNewEntry();
            ImGui.TreePop();
        }

        if (ImGui.TreeNodeEx("Entries table"u8, ImGuiTreeNodeFlags.DefaultOpen))
        {
            Table();
            ImGui.TreePop();
        }
    }

    private void LoadButton()
    {
        ImGui.BeginDisabled(_loadingState == LoadingStateEnum.Pending);
        if (ImGui.Button("Select file"u8))
        {
            DialogResult result = Dialog.FileOpen("bin", pathPrefs.FilePath);
            if (result.IsOk)
            {
                pathPrefs.FilePath = result.Path;
                // TODO: async
                pathPrefs.Save();
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
                ImGui.Text("Please select a file"u8);
                break;
            case LoadingStateEnum.Pending:
                ImGui.SameLine();
                ImGui.Text("Loading..."u8);
                break;
            case LoadingStateEnum.Success:
                break;
            case LoadingStateEnum.Error:
                ImGui.SameLine();
                ImGui.Text("Error while loading: "u8);
                ImGui.SameLine();
                ImGui.TextWrapped(_loadErrorMessage);
                break;
        }
    }

    private void SaveButton()
    {
        ImGui.BeginDisabled(_savingState == LoadingStateEnum.Pending);
        if (ImGui.Button("Save file"u8))
        {
            DialogResult result = Dialog.FileSave("bin", pathPrefs.FilePath);
            if (result.IsOk)
            {
                pathPrefs.FilePath = result.Path;
                // TODO: async
                pathPrefs.Save();
                _ = SaveLang();
            }
        }
        ImGui.EndDisabled();

        switch (_savingState)
        {
            default:
            case LoadingStateEnum.None:
                break;
            case LoadingStateEnum.Pending:
                ImGui.SameLine();
                ImGui.Text("Saving..."u8);
                break;
            case LoadingStateEnum.Success:
                break;
            case LoadingStateEnum.Error:
                ImGui.SameLine();
                ImGui.Text("Error while saving: "u8);
                ImGui.SameLine();
                ImGui.TextWrapped(_saveErrorMessage);
                break;
        }
    }

    private string _filterKey = "";
    private string _filterText = "";
    private bool _filterTextCaseSensitive = false;
    private void Filters()
    {
        ImGui.SetNextItemWidth(300);
        ImGui.InputText("Filter by string key"u8, ref _filterKey, 256);

        ImGui.Spacing();

        ImGui.SetNextItemWidth(300);
        ImGui.InputText("Filter by text"u8, ref _filterText, 256);
        ImGui.SameLine();
        ImGui.Dummy(new(30, 0));
        ImGui.SameLine();
        ImGui.Checkbox("Case sensitive"u8, ref _filterTextCaseSensitive);
    }

    private string _newKey = "";
    private string _newText = "";
    private void AddNewEntry()
    {
        if (_langEntries is null)
            return;

        ImGui.InputText("Key"u8, ref _newKey, 256);
        ImGui.InputTextMultiline("Text"u8, ref _newText, maxTextLength);
        ImGui.BeginDisabled(string.IsNullOrWhiteSpace(_newKey));
        if (ImGui.Button("Add"u8))
        {
            _langEntries[_newKey] = _newText;
            _newKey = "";
            _newText = "";
        }
        ImGui.EndDisabled();
    }

    private readonly List<(string key, string text)> _tableUpdates = [];
    private readonly List<string> _tableRemovals = [];
    private void Table()
    {
        if (_langEntries is null)
            return;

        ImGui.BeginChild("##table"u8, new Vector2(ImGui.GetWindowWidth(), ImGui.GetWindowHeight() * 0.83f));
        if (ImGui.BeginTable("##entries"u8, 3, ImGuiTableFlags.SizingFixedFit))
        {
            ImGui.TableNextColumn();
            ImGui.TableHeader("String Key"u8);
            ImGui.TableNextColumn();
            ImGui.TableHeader("Text"u8);
            ImGui.TableNextColumn();
            ImGui.TableHeader(""u8);

            foreach ((string key, string text) in _langEntries)
            {
                if (!key.Contains(_filterKey, StringComparison.CurrentCultureIgnoreCase))
                    continue;
                // TODO: this should retrigger only on change, so text editing isn't annoying
                if (!text.Contains(_filterText, _filterTextCaseSensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase))
                    continue;

                ImGui.PushID(key);
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(key);

                ImGui.TableNextColumn();
                string text_ = text;
                if (ImGui.InputTextMultiline("##text"u8, ref text_, maxTextLength, new Vector2(ImGui.GetWindowWidth() * 0.5f, 60)))
                {
                    _tableUpdates.Add((key, text_));
                }

                ImGui.TableNextColumn();
                if (ImGui.Button("Delete"u8))
                {
                    _tableRemovals.Add(key);
                }

                ImGui.PopID();
            }

            foreach ((string key, string text) in _tableUpdates)
                _langEntries[key] = text;
            _tableUpdates.Clear();
            foreach (string key in _tableRemovals)
                _langEntries.Remove(key);
            _tableRemovals.Clear();

            ImGui.EndTable();
        }
        ImGui.EndChild();
    }

    private async Task LoadLang(CancellationToken cancellationToken = default)
    {
        if (pathPrefs.FilePath is not null)
        {
            _loadErrorMessage = null;
            _loadingState = LoadingStateEnum.Pending;
            try
            {
                LangFile langFile = await LangFile.LoadAsync(pathPrefs.FilePath, cancellationToken);
                _langEntries = new(langFile.Entries);
                _loadingState = LoadingStateEnum.Success;
            }
            catch (Exception e)
            {
                _loadingState = LoadingStateEnum.Error;
                _loadErrorMessage = e.Message;
                Rl.TraceLog(TraceLogLevel.Error, e.Message);
                Rl.TraceLog(TraceLogLevel.Trace, e.StackTrace);
            }
        }
    }

    private async Task SaveLang(CancellationToken cancellationToken = default)
    {
        if (pathPrefs.FilePath is not null && _langEntries is not null)
        {
            _saveErrorMessage = null;
            _savingState = LoadingStateEnum.Pending;
            try
            {
                LangFile langFile = new((uint)_langEntries.Count, new(_langEntries));
                await langFile.SaveAsync(pathPrefs.FilePath, cancellationToken);
                _savingState = LoadingStateEnum.Success;
            }
            catch (Exception e)
            {
                _savingState = LoadingStateEnum.Error;
                _saveErrorMessage = e.Message;
                Rl.TraceLog(TraceLogLevel.Error, e.Message);
                Rl.TraceLog(TraceLogLevel.Trace, e.StackTrace);
            }
        }
    }
}