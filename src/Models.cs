namespace OneNoteExporter;

public sealed record NotebookInfo(int Number, string Name)
{
    public override string ToString() => Number == 0 ? "[0] 导出所有笔记本" : $"[{Number}] {Name}";
}

public enum ExportFormat
{
    Markdown = 1,
    JoplinRawFolder = 2
}

public sealed class ExportOptions
{
    public required string ExporterDirectory { get; init; }
    public required string NotebookNumber { get; init; }
    public string? CustomOutputDirectory { get; init; }
    public ExportFormat Format { get; init; } = ExportFormat.Markdown;
    public bool SkipAdvancedSettings { get; init; } = true;
}

public enum ExportState
{
    Idle,
    Starting,
    WaitingForInput,
    Exporting,
    Completed,
    Failed,
    Cancelled
}

public sealed class ExportProgress
{
    public ExportState State { get; init; } = ExportState.Idle;
    public string Phase { get; init; } = "空闲";
    public int? CurrentSection { get; init; }
    public int? TotalSections { get; init; }
    public int? CurrentPage { get; init; }
    public int? TotalPages { get; init; }
    public int OverallPercent { get; init; }
    public string CurrentItem { get; init; } = string.Empty;
    public bool HasWarnings { get; init; }
    public string? ExportPath { get; init; }
    public string? Message { get; init; }
}

public sealed record ExporterLocation(
    string Directory,
    string ExePath,
    string SettingsPath,
    string LogsPath,
    string PandocPath,
    string ResourcesPath,
    bool IsComplete,
    IReadOnlyList<string> MissingFiles);

public sealed class AppSettingsModel
{
    public string ResourceFolderName { get; set; } = "resources";
    public int PageTitleMaxLength { get; set; } = 50;
    public bool AddFrontMatterHeader { get; set; } = true;
    public int MdMaxFileLength { get; set; } = 50;
    public string ProcessingOfPageHierarchy { get; set; } = "HierarchyAsFolderTree";
    public string ResourceFolderLocation { get; set; } = "RootFolder";
    public string OneNoteLinksHandling { get; set; } = "ConvertToWikilink";
    public string PanDocMarkdownFormat { get; set; } = "gfm";
    public bool UseHtmlStyling { get; set; } = true;
    public string IndentingStyle { get; set; } = "LeaveAsIs";
    public bool PostProcessingRemoveOneNoteHeader { get; set; } = true;
}

public sealed class GuiSettings
{
    public string? ExporterDirectory { get; set; }
    public int? LastNotebookNumber { get; set; }
    public int ExportFormatIndex { get; set; }
    public bool UseCustomOutputPath { get; set; }
    public string? CustomOutputPath { get; set; }
    public int WindowX { get; set; } = -1;
    public int WindowY { get; set; } = -1;
    public int WindowWidth { get; set; } = 1004;
    public int WindowHeight { get; set; } = 896;
}
