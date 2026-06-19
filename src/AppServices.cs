using System.Diagnostics;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace OneNoteExporter;

public static class ExporterLocator
{
    public static ExporterLocation Locate(string directory)
    {
        var fullDirectory = Path.GetFullPath(directory.Trim());
        var exePath = Path.Combine(fullDirectory, "OneNoteMdExporter.exe");
        var settingsPath = Path.Combine(fullDirectory, "appSettings.json");
        var logsPath = Path.Combine(fullDirectory, "logs.txt");
        var pandocPath = Path.Combine(fullDirectory, "pandoc", "pandoc.exe");
        var resourcesPath = Path.Combine(fullDirectory, "Resources");

        var missing = new List<string>();
        if (!File.Exists(exePath)) missing.Add("OneNoteMdExporter.exe");
        if (!File.Exists(settingsPath)) missing.Add("appSettings.json");
        if (!File.Exists(pandocPath)) missing.Add(@"pandoc\pandoc.exe");
        if (!System.IO.Directory.Exists(resourcesPath)) missing.Add(@"Resources\");

        return new ExporterLocation(fullDirectory, exePath, settingsPath, logsPath, pandocPath, resourcesPath, missing.Count == 0, missing);
    }

    public static string? AutoDetect()
    {
        var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (File.Exists(Path.Combine(baseDir, "OneNoteMdExporter.exe")))
            return baseDir;

        var parent = Path.GetDirectoryName(baseDir);
        if (parent is null) return null;

        try
        {
            foreach (var dir in System.IO.Directory.GetDirectories(parent, "OneNoteMdExporter*"))
            {
                if (File.Exists(Path.Combine(dir, "OneNoteMdExporter.exe")))
                    return dir;
            }
        }
        catch { }

        return null;
    }
}

public static class ValidationService
{
    public static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static IReadOnlyList<string> ValidateBeforeExport(ExporterLocation location, string notebookNumber)
    {
        var errors = new List<string>();
        if (!location.IsComplete)
        {
            errors.Add("导出器目录不完整，缺少：" + string.Join("、", location.MissingFiles));
        }

        if (string.IsNullOrWhiteSpace(notebookNumber))
        {
            errors.Add("请输入笔记本编号，例如 1；输入 0 表示导出全部笔记本。");
        }
        else if (!int.TryParse(notebookNumber.Trim(), out var number) || number < 0)
        {
            errors.Add("笔记本编号必须是 0 或正整数。");
        }

        return errors;
    }
}

public static class NotebookListParser
{
    private static readonly Regex NotebookLineRegex = new(@"(?:^|\]\s*)\[(\d+)\]\s*(.+)$", RegexOptions.Compiled);

    public static NotebookInfo? TryParseLine(string line)
    {
        var match = NotebookLineRegex.Match(line.Trim());
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var number))
            return null;

        var name = match.Groups[2].Value.Trim();
        if (string.IsNullOrWhiteSpace(name)) return null;
        if (number == 0) name = "导出所有笔记本";

        return new NotebookInfo(number, name);
    }

    public static IReadOnlyList<NotebookInfo> ParseLatestFromLog(string logsPath)
    {
        if (!File.Exists(logsPath)) return Array.Empty<NotebookInfo>();

        var latest = new Dictionary<int, NotebookInfo>();
        foreach (var line in File.ReadLines(logsPath))
        {
            if (line.Contains("请输入要导出的笔记本编号", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("PleaseChooseNotebookToExport", StringComparison.OrdinalIgnoreCase))
            {
                latest.Clear();
                continue;
            }

            var notebook = TryParseLine(line);
            if (notebook is not null)
            {
                latest[notebook.Number] = notebook;
            }
        }

        return latest.Values.OrderBy(n => n.Number).ToList();
    }
}

public sealed class NotebookScanner
{
    public event Action<string>? LineReceived;

    public async Task<IReadOnlyList<NotebookInfo>> ScanAsync(ExporterLocation location, CancellationToken cancellationToken = default)
    {
        if (!location.IsComplete)
        {
            throw new InvalidOperationException("导出器目录不完整，缺少：" + string.Join("、", location.MissingFiles));
        }

        var notebooks = new Dictionary<int, NotebookInfo>();
        var sawNotebookPrompt = false;
        var sentEnter = false;
        var handleLock = new object();

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linkedCts.CancelAfter(TimeSpan.FromSeconds(30));
        var token = linkedCts.Token;

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = location.ExePath,
                WorkingDirectory = location.Directory,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        void HandleLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return;

            lock (handleLock)
            {
                LineReceived?.Invoke(line);

                if (!sentEnter && (line.Contains("按下回车", StringComparison.OrdinalIgnoreCase) ||
                                   line.Contains("Press Enter", StringComparison.OrdinalIgnoreCase)))
                {
                    sentEnter = true;
                    process.StandardInput.WriteLine();
                    process.StandardInput.Flush();
                    LineReceived?.Invoke("> [回车] 开始扫描笔记本列表");
                    return;
                }

                if (line.Contains("请输入要导出的笔记本编号", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("notebook number", StringComparison.OrdinalIgnoreCase))
                {
                    sawNotebookPrompt = true;
                }

                if (!sawNotebookPrompt) return;

                var notebook = NotebookListParser.TryParseLine(line);
                if (notebook is not null)
                {
                    notebooks[notebook.Number] = notebook;
                }
            }
        }

        process.Start();
        var tailer = new LogTailer(location.LogsPath);
        var stdoutTask = ReadLinesAsync(process.StandardOutput, HandleLine, token);
        var stderrTask = ReadLinesAsync(process.StandardError, HandleLine, token);
        var tailTask = tailer.TailAsync(HandleLine, token);

        try
        {
            while (!token.IsCancellationRequested)
            {
                bool ready;
                lock (handleLock)
                {
                    ready = sawNotebookPrompt && notebooks.Any(n => n.Key > 0);
                }

                if (ready)
                {
                    await Task.Delay(800, token).ConfigureAwait(false);
                    break;
                }

                if (process.HasExited) break;

                await Task.Delay(200, token).ConfigureAwait(false);
            }
        }
        finally
        {
            linkedCts.Cancel();
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
            }

            try { await Task.WhenAll(stdoutTask, stderrTask, tailTask).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
        }

        return notebooks.Values.OrderBy(n => n.Number).ToList();
    }

    private static async Task ReadLinesAsync(StreamReader reader, Action<string> onLine, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            onLine(line);
        }
    }
}

public sealed class AppSettingsEditor
{
    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    private static readonly HashSet<string> ProcessingOfPageHierarchyValues = new(StringComparer.OrdinalIgnoreCase)
        { "HierarchyAsFolderTree", "HierarchyAsPageTitlePrefix", "IgnoreHierarchy" };
    private static readonly HashSet<string> ResourceFolderLocationValues = new(StringComparer.OrdinalIgnoreCase)
        { "RootFolder", "PageParentFolder" };
    private static readonly HashSet<string> OneNoteLinksHandlingValues = new(StringComparer.OrdinalIgnoreCase)
        { "KeepOriginal", "ConvertToMarkdown", "ConvertToWikilink", "Remove" };
    private static readonly HashSet<string> IndentingStyleValues = new(StringComparer.OrdinalIgnoreCase)
        { "LeaveAsIs", "ConvertToEmSpaces", "ConvertToBullets" };

    public AppSettingsModel Load(string settingsPath)
    {
        var json = File.ReadAllText(settingsPath, Encoding.UTF8);
        using var document = JsonDocument.Parse(json, JsonOptions);
        var root = document.RootElement;

        return new AppSettingsModel
        {
            ResourceFolderName = ReadString(root, nameof(AppSettingsModel.ResourceFolderName), "resources"),
            PageTitleMaxLength = ReadInt(root, nameof(AppSettingsModel.PageTitleMaxLength), 50),
            AddFrontMatterHeader = ReadBool(root, nameof(AppSettingsModel.AddFrontMatterHeader), true),
            MdMaxFileLength = ReadInt(root, nameof(AppSettingsModel.MdMaxFileLength), 50),
            ProcessingOfPageHierarchy = ReadString(root, nameof(AppSettingsModel.ProcessingOfPageHierarchy), "HierarchyAsFolderTree"),
            ResourceFolderLocation = ReadString(root, nameof(AppSettingsModel.ResourceFolderLocation), "RootFolder"),
            OneNoteLinksHandling = ReadString(root, nameof(AppSettingsModel.OneNoteLinksHandling), "ConvertToWikilink"),
            PanDocMarkdownFormat = ReadString(root, nameof(AppSettingsModel.PanDocMarkdownFormat), "gfm"),
            UseHtmlStyling = ReadBool(root, nameof(AppSettingsModel.UseHtmlStyling), true),
            IndentingStyle = ReadString(root, nameof(AppSettingsModel.IndentingStyle), "LeaveAsIs"),
            PostProcessingRemoveOneNoteHeader = ReadBool(root, nameof(AppSettingsModel.PostProcessingRemoveOneNoteHeader), true)
        };
    }

    public string Save(string settingsPath, AppSettingsModel model)
    {
        var errors = Validate(model);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }

        var original = File.ReadAllText(settingsPath, Encoding.UTF8);
        var updated = original;

        updated = Patch(updated, nameof(AppSettingsModel.ResourceFolderName), model.ResourceFolderName);
        updated = Patch(updated, nameof(AppSettingsModel.PageTitleMaxLength), model.PageTitleMaxLength);
        updated = Patch(updated, nameof(AppSettingsModel.AddFrontMatterHeader), model.AddFrontMatterHeader);
        updated = Patch(updated, nameof(AppSettingsModel.MdMaxFileLength), model.MdMaxFileLength);
        updated = Patch(updated, nameof(AppSettingsModel.ProcessingOfPageHierarchy), model.ProcessingOfPageHierarchy);
        updated = Patch(updated, nameof(AppSettingsModel.ResourceFolderLocation), model.ResourceFolderLocation);
        updated = Patch(updated, nameof(AppSettingsModel.OneNoteLinksHandling), model.OneNoteLinksHandling);
        updated = Patch(updated, nameof(AppSettingsModel.PanDocMarkdownFormat), model.PanDocMarkdownFormat);
        updated = Patch(updated, nameof(AppSettingsModel.UseHtmlStyling), model.UseHtmlStyling);
        updated = Patch(updated, nameof(AppSettingsModel.IndentingStyle), model.IndentingStyle);
        updated = Patch(updated, nameof(AppSettingsModel.PostProcessingRemoveOneNoteHeader), model.PostProcessingRemoveOneNoteHeader);

        using (JsonDocument.Parse(updated, JsonOptions)) { }

        var backupPath = settingsPath + ".bak." + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        File.Copy(settingsPath, backupPath, overwrite: true);
        File.WriteAllText(settingsPath, updated, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return backupPath;
    }

    public IReadOnlyList<string> Validate(AppSettingsModel model)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(model.ResourceFolderName))
            errors.Add("资源文件夹名称不能为空。");
        else if (model.ResourceFolderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            errors.Add("资源文件夹名称包含非法字符。");

        if (model.PageTitleMaxLength <= 0) errors.Add("页面标题最大长度必须大于 0。");
        if (model.MdMaxFileLength <= 0) errors.Add("Markdown 文件名最大长度必须大于 0。");
        if (!ProcessingOfPageHierarchyValues.Contains(model.ProcessingOfPageHierarchy)) errors.Add("页面层级处理方式无效。");
        if (!ResourceFolderLocationValues.Contains(model.ResourceFolderLocation)) errors.Add("资源文件夹位置无效。");
        if (!OneNoteLinksHandlingValues.Contains(model.OneNoteLinksHandling)) errors.Add("OneNote 链接处理方式无效。");
        if (string.IsNullOrWhiteSpace(model.PanDocMarkdownFormat)) errors.Add("Pandoc Markdown 格式不能为空。");
        if (!IndentingStyleValues.Contains(model.IndentingStyle)) errors.Add("缩进处理方式无效。");

        return errors;
    }

    private static string Patch(string text, string key, object value)
    {
        var pattern = $"(\\\"{Regex.Escape(key)}\\\"\\s*:\\s*)(\\\"(?:\\\\.|[^\\\"\\\\])*\\\"|true|false|-?\\d+)(\\s*,?)";
        var regex = new Regex(pattern, RegexOptions.IgnoreCase);
        var replacement = regex.Replace(text, match =>
            match.Groups[1].Value + FormatValue(value) + match.Groups[3].Value,
            count: 1);

        if (ReferenceEquals(replacement, text) || replacement == text && !regex.IsMatch(text))
        {
            throw new InvalidOperationException($"appSettings.json 中没有找到字段：{key}");
        }

        return replacement;
    }

    private static string FormatValue(object value)
    {
        return value switch
        {
            string s => JsonSerializer.Serialize(s),
            bool b => b ? "true" : "false",
            int i => i.ToString(),
            _ => JsonSerializer.Serialize(value)
        };
    }

    private static string ReadString(JsonElement root, string key, string defaultValue) =>
        root.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? defaultValue : defaultValue;

    private static int ReadInt(JsonElement root, string key, int defaultValue) =>
        root.TryGetProperty(key, out var v) && v.TryGetInt32(out var r) ? r : defaultValue;

    private static bool ReadBool(JsonElement root, string key, bool defaultValue) =>
        root.TryGetProperty(key, out var v) && (v.ValueKind == JsonValueKind.True || v.ValueKind == JsonValueKind.False) ? v.GetBoolean() : defaultValue;
}

public static class GuiSettingsStore
{
    private static string GetPath() => Path.Combine(AppContext.BaseDirectory, "gui-settings.json");

    public static GuiSettings Load()
    {
        var path = GetPath();
        if (!File.Exists(path)) return new GuiSettings();

        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            return JsonSerializer.Deserialize<GuiSettings>(json) ?? new GuiSettings();
        }
        catch
        {
            return new GuiSettings();
        }
    }

    public static void Save(GuiSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GetPath(), json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }
        catch { }
    }
}
