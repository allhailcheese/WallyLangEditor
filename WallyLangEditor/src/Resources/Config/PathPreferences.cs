using System;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Raylib_cs;

namespace WallyLangEditor.Resources.Config;

public class PathPreferences
{
    public const string APPDATA_DIR_NAME = nameof(WallyLangEditor);
    public const string FILE_NAME = "PathPreferences.xml";

    public string? FilePath { get; set; }

    public static string ConfigFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        APPDATA_DIR_NAME,
        FILE_NAME
    );

    private PathPreferences() { }

    private PathPreferences(XElement e)
    {
        FilePath = e.Element(nameof(FilePath))?.Value;
    }

    public static async Task<PathPreferences> Load()
    {
        if (!File.Exists(ConfigFilePath)) return new();

        try
        {
            using XmlReader reader = XmlReader.Create(ConfigFilePath, new() { Async = true });
            XElement element = await XElement.LoadAsync(reader, LoadOptions.None, new());
            reader.Dispose();
            return new(element);
        }
        catch (Exception e)
        {
            Rl.TraceLog(TraceLogLevel.Error, e.Message);
            Rl.TraceLog(TraceLogLevel.Trace, e.StackTrace);
            return new();
        }
    }

    public void Save()
    {
        // create dir
        string? dir = Path.GetDirectoryName(ConfigFilePath);
        if (dir is not null) Directory.CreateDirectory(dir);

        try
        {
            SerializeToXML().Save(ConfigFilePath);
        }
        catch (Exception e)
        {
            Rl.TraceLog(TraceLogLevel.Error, e.Message);
            Rl.TraceLog(TraceLogLevel.Trace, e.StackTrace);
        }
    }

    private XElement SerializeToXML()
    {
        XElement e = new(nameof(PathPreferences));
        e.SetElementValue(nameof(FilePath), FilePath);
        return e;
    }
}