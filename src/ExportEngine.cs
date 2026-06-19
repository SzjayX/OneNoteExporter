using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace OneNoteExporter;

public sealed class LogTailer
{
    private readonly string _path;
    private long _position;

    public LogTailer(string path)
    {
        _path = path;
        _position = File.Exists(path) ? new FileInfo(path).Length : 0;
    }

    public async Task TailAsync(Action<string> onLine, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (File.Exists(_path))
            {
                using var stream = new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                if (stream.Length < _position)
                {
                    _position = 0;
                }

                stream.Seek(_position, SeekOrigin.Begin);
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (line is not null)
                    {
                        onLine(line);
                    }
                }

                _position = stream.Position;
            }

            await Task.Delay(500, cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed class ExportLogParser
{
    private static readonly Regex SectionRegex = new(@"Section\s+\((\d+)\/(\d+)\)\s*:\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PageRegex = new(@"(?:页面|Page)\s+(\d+)\/(\d+)\s*:\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ExportPathRegex = new(@"(?:导出路径为|Export path)\s*:\s*'(.+?)'", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly object _lock = new();
    private int? _currentSection;
    private int? _totalSections;
    private int? _currentPage;
    private int? _totalPages;
    private bool _hasWarnings;
    private string _phase = "准备中";
    private string _currentItem = string.Empty;
    private string? _exportPath;
    private ExportState _state = ExportState.Idle;

    public string? ExportPath { get { lock (_lock) return _exportPath; } }

    public ExportProgress ParseLine(string line)
    {
        lock (_lock)
        {
            if (line.Contains("WRN", StringComparison.OrdinalIgnoreCase))
            {
                _hasWarnings = true;
            }

            if (line.Contains("Phase 1", StringComparison.OrdinalIgnoreCase))
            {
                _phase = "阶段 1：扫描笔记本结构";
                _state = ExportState.Exporting;
            }
            else if (line.Contains("Phase 2", StringComparison.OrdinalIgnoreCase))
            {
                _phase = "阶段 2：导出并转换页面";
                _state = ExportState.Exporting;
            }

            var sectionMatch = SectionRegex.Match(line);
            if (sectionMatch.Success)
            {
                _currentSection = int.Parse(sectionMatch.Groups[1].Value);
                _totalSections = int.Parse(sectionMatch.Groups[2].Value);
                _currentItem = sectionMatch.Groups[3].Value.Trim();
                _phase = "阶段 1：扫描笔记本结构";
                _state = ExportState.Exporting;
            }

            var pageMatch = PageRegex.Match(line);
            if (pageMatch.Success)
            {
                _currentPage = int.Parse(pageMatch.Groups[1].Value);
                _totalPages = int.Parse(pageMatch.Groups[2].Value);
                _currentItem = pageMatch.Groups[3].Value.Trim();
                _phase = "阶段 2：导出并转换页面";
                _state = ExportState.Exporting;
            }

            var pathMatch = ExportPathRegex.Match(line);
            if (pathMatch.Success)
            {
                _exportPath = pathMatch.Groups[1].Value.Trim();
            }
            else
            {
                var movedPath = TryParseMovedExportPath(line);
                if (!string.IsNullOrWhiteSpace(movedPath))
                {
                    _exportPath = movedPath;
                }
            }

            if (_state != ExportState.Completed &&
                (line.Contains("笔记本导出成功", StringComparison.OrdinalIgnoreCase) ||
                 line.Contains("Notebook exported", StringComparison.OrdinalIgnoreCase)))
            {
                _state = ExportState.Completed;
                _phase = _hasWarnings ? "导出完成（有警告）" : "导出完成";
            }
            else if (_state != ExportState.Completed && IsErrorLine(line))
            {
                _state = ExportState.Failed;
            }

            return Snapshot(line);
        }
    }

    public ExportProgress MarkState(ExportState state, string message)
    {
        lock (_lock)
        {
            _state = state;
            return Snapshot(message);
        }
    }

    private ExportProgress Snapshot(string? message = null)
    {
        return new ExportProgress
        {
            State = _state,
            Phase = _phase,
            CurrentSection = _currentSection,
            TotalSections = _totalSections,
            CurrentPage = _currentPage,
            TotalPages = _totalPages,
            OverallPercent = CalculatePercent(),
            CurrentItem = _currentItem,
            HasWarnings = _hasWarnings,
            ExportPath = _exportPath,
            Message = message
        };
    }

    private static string? TryParseMovedExportPath(string line)
    {
        const string marker = "导出结果已移动到 : '";
        var markerIndex = line.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0) return null;
        var start = markerIndex + marker.Length;
        var end = line.IndexOf('\'', start);
        return end > start ? line[start..end] : null;
    }

    private int CalculatePercent()
    {
        if (_currentPage.HasValue && _totalPages is > 0)
        {
            return Math.Clamp(20 + (int)Math.Round(_currentPage.Value * 80.0 / _totalPages.Value), 20, 100);
        }

        if (_currentSection.HasValue && _totalSections is > 0)
        {
            return Math.Clamp((int)Math.Round(_currentSection.Value * 20.0 / _totalSections.Value), 0, 20);
        }

        return 0;
    }

    private static bool IsErrorLine(string line)
    {
        if (line.Contains("WRN", StringComparison.OrdinalIgnoreCase)) return false;

        if (line.Contains("[ERR]", StringComparison.OrdinalIgnoreCase) ||
            line.Contains("[FTL]", StringComparison.OrdinalIgnoreCase))
            return true;

        return line.Contains("出错", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("出现错误", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("失败", StringComparison.OrdinalIgnoreCase) ||
               line.Contains("没有找到", StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class ConsoleConversationDriver
{
    private enum ConversationState
    {
        WaitingForEnter,
        WaitingForNotebookNumber,
        WaitingForExportFormat,
        WaitingForAdvancedSettings,
        Exporting
    }

    private readonly ExportOptions _options;
    private ConversationState _state = ConversationState.WaitingForEnter;

    public ConsoleConversationDriver(ExportOptions options)
    {
        _options = options;
    }

    public string? GetResponse(string output)
    {
        if (_state == ConversationState.WaitingForEnter && ContainsAny(output, "按下回车", "Press Enter"))
        {
            _state = ConversationState.WaitingForNotebookNumber;
            return string.Empty;
        }

        if (_state == ConversationState.WaitingForNotebookNumber &&
            ContainsAny(output, "笔记本编号", "notebook number"))
        {
            _state = ConversationState.WaitingForExportFormat;
            return _options.NotebookNumber.Trim();
        }

        if (_state == ConversationState.WaitingForExportFormat &&
            ContainsAny(output, "选择导出的格式", "ChooseExportFormat", "export format"))
        {
            _state = ConversationState.WaitingForAdvancedSettings;
            return ((int)_options.Format).ToString();
        }

        if (_state == ConversationState.WaitingForAdvancedSettings &&
            ContainsAny(output, "高级设置", "advanced settings"))
        {
            _state = ConversationState.Exporting;
            return _options.SkipAdvancedSettings ? "n" : "yes";
        }

        if (_state == ConversationState.Exporting && ContainsAny(output, "按下任何按键退出", "Press any key", "any key to exit"))
        {
            return string.Empty;
        }

        return null;
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(needle => value.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class ExportProcessRunner : IDisposable
{
    private readonly ExportLogParser _parser = new();
    private readonly object _seenLock = new();
    private readonly Queue<string> _seenOrder = new();
    private readonly HashSet<string> _seenLines = new(StringComparer.Ordinal);
    private Process? _process;
    private CancellationTokenSource? _runCancellation;

    public event Action<string>? LineReceived;
    public event Action<ExportProgress>? ProgressChanged;
    public event Action<int?>? Exited;

    public bool IsRunning => _process is { HasExited: false };

    public async Task RunAsync(ExportOptions options, CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            throw new InvalidOperationException("已有导出任务正在运行。");
        }

        var location = ExporterLocator.Locate(options.ExporterDirectory);
        if (!location.IsComplete)
        {
            throw new InvalidOperationException("导出器目录不完整，缺少：" + string.Join("、", location.MissingFiles));
        }

        _runCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _runCancellation.Token;
        var driver = new ConsoleConversationDriver(options);
        var tailer = new LogTailer(location.LogsPath);

        var startInfo = new ProcessStartInfo
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
        };

        _process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        ProgressChanged?.Invoke(_parser.MarkState(ExportState.Starting, "正在启动 OneNoteMdExporter..."));

        if (!_process.Start())
        {
            throw new InvalidOperationException("无法启动 OneNoteMdExporter.exe。");
        }

        ProgressChanged?.Invoke(_parser.MarkState(ExportState.WaitingForInput, "已启动，正在等待控制台提示..."));

        var outputTask = ReadOutputAsync(_process.StandardOutput, driver, token);
        var errorTask = ReadOutputAsync(_process.StandardError, driver, token);
        var tailTask = tailer.TailAsync(line => HandleLine(line, driver), token);

        try
        {
            await _process.WaitForExitAsync(token).ConfigureAwait(false);
            await Task.WhenAny(Task.WhenAll(outputTask, errorTask), Task.Delay(1500, token)).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            Cancel();
            ProgressChanged?.Invoke(_parser.MarkState(ExportState.Cancelled, "导出已取消。"));
            throw;
        }
        finally
        {
            _runCancellation.Cancel();
            try { await tailTask.ConfigureAwait(false); } catch (OperationCanceledException) { }
            int? exitCode = _process?.HasExited == true ? _process.ExitCode : null;

            if (exitCode is not 0 and not null)
            {
                ProgressChanged?.Invoke(_parser.MarkState(ExportState.Failed, $"进程已退出，退出码：{exitCode}"));
            }
            else if (exitCode == 0)
            {
                MoveExportIfRequested(options);
            }

            Exited?.Invoke(exitCode);
        }
    }

    public void Cancel()
    {
        _runCancellation?.Cancel();
        if (_process is { HasExited: false })
        {
            try { _process.Kill(entireProcessTree: true); }
            catch { }
        }
    }

    private async Task ReadOutputAsync(StreamReader reader, ConsoleConversationDriver driver, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (line is null) break;
            HandleLine(line, driver);
        }
    }

    private void HandleLine(string line, ConsoleConversationDriver? driver = null)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        if (RememberLine(line))
        {
            LineReceived?.Invoke(line);
        }

        ProgressChanged?.Invoke(_parser.ParseLine(line));

        var response = driver?.GetResponse(line);
        if (response is not null && _process is { HasExited: false })
        {
            _process.StandardInput.WriteLine(response);
            _process.StandardInput.Flush();
            HandleLine($"> {(string.IsNullOrEmpty(response) ? "[回车]" : response)}");
        }
    }

    private void MoveExportIfRequested(ExportOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.CustomOutputDirectory) || string.IsNullOrWhiteSpace(_parser.ExportPath))
            return;

        var sourcePath = _parser.ExportPath;
        if (!Directory.Exists(sourcePath)) return;

        Directory.CreateDirectory(options.CustomOutputDirectory);
        var destinationPath = Path.Combine(options.CustomOutputDirectory,
            Path.GetFileName(sourcePath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)));
        if (Directory.Exists(destinationPath))
        {
            destinationPath += "-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
        }

        Directory.Move(sourcePath, destinationPath);
        HandleLine($"导出结果已移动到 : '{destinationPath}'");
    }

    private bool RememberLine(string line)
    {
        lock (_seenLock)
        {
            if (!_seenLines.Add(line)) return false;
            _seenOrder.Enqueue(line);
            while (_seenOrder.Count > 500)
            {
                _seenLines.Remove(_seenOrder.Dequeue());
            }
            return true;
        }
    }

    public void Dispose()
    {
        _runCancellation?.Dispose();
        _process?.Dispose();
    }
}
