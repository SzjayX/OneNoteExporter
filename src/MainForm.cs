using System.Diagnostics;

namespace OneNoteExporter;

public partial class MainForm : Form
{
    private readonly AppSettingsEditor _settingsEditor = new();
    private readonly GuiSettings _guiSettings;
    private ExporterLocation? _location;
    private AppSettingsModel? _appSettings;
    private string? _lastExportPath;
    private TextBox txtDefaultOutputPath = null!;
    private TextBox txtCustomOutputPath = null!;
    private CheckBox chkUseCustomOutputPath = null!;
    private Button btnBrowseCustomOutput = null!;
    private CancellationTokenSource? _exportCancellation;
    private CancellationTokenSource? _scanCancellation;
    private ExportProcessRunner? _runner;

    public MainForm()
    {
        InitializeComponent();
        InitializeChoices();

        var version = typeof(MainForm).Assembly.GetName().Version;
        if (version is not null)
            Text += $" v{version.Major}.{version.Minor}.{version.Build}";

        _guiSettings = GuiSettingsStore.Load();
        ApplyGuiSettings();

        if (string.IsNullOrWhiteSpace(txtExporterDirectory.Text))
        {
            var detected = ExporterLocator.AutoDetect();
            if (detected is not null)
            {
                txtExporterDirectory.Text = detected;
            }
        }

        if (!string.IsNullOrWhiteSpace(txtExporterDirectory.Text))
        {
            DetectExporterDirectory(loadSettings: true);
        }

        if (ValidationService.IsRunningAsAdministrator())
        {
            AppendLog("警告：不建议以管理员身份运行本 GUI。原工具要求它和 OneNote 都以普通权限启动。");
        }
    }

    private void InitializeChoices()
    {
        cmbExportFormat.Items.Add(new ComboItem<ExportFormat>("Markdown", ExportFormat.Markdown));
        cmbExportFormat.Items.Add(new ComboItem<ExportFormat>("Joplin Raw Folder", ExportFormat.JoplinRawFolder));
        cmbExportFormat.SelectedIndex = 0;

        cmbNotebookPicker.SelectedIndexChanged += (_, _) => { };
    }

    private void ApplyGuiSettings()
    {
        if (!string.IsNullOrWhiteSpace(_guiSettings.ExporterDirectory))
            txtExporterDirectory.Text = _guiSettings.ExporterDirectory;

        if (_guiSettings.ExportFormatIndex >= 0 && _guiSettings.ExportFormatIndex < cmbExportFormat.Items.Count)
            cmbExportFormat.SelectedIndex = _guiSettings.ExportFormatIndex;

        chkUseCustomOutputPath.Checked = _guiSettings.UseCustomOutputPath;
        if (!string.IsNullOrWhiteSpace(_guiSettings.CustomOutputPath))
            txtCustomOutputPath.Text = _guiSettings.CustomOutputPath;

        if (_guiSettings.WindowX >= 0 && _guiSettings.WindowY >= 0)
        {
            var restored = new Point(_guiSettings.WindowX, _guiSettings.WindowY);
            bool onScreen = Screen.AllScreens.Any(s => s.WorkingArea.Contains(restored));
            if (onScreen)
            {
                StartPosition = FormStartPosition.Manual;
                Location = restored;
            }
        }

        if (_guiSettings.WindowWidth > 0 && _guiSettings.WindowHeight > 0)
        {
            Size = new Size(_guiSettings.WindowWidth, _guiSettings.WindowHeight);
        }
    }

    private void SaveGuiSettings()
    {
        _guiSettings.ExporterDirectory = txtExporterDirectory.Text.Trim();
        _guiSettings.ExportFormatIndex = cmbExportFormat.SelectedIndex;
        _guiSettings.UseCustomOutputPath = chkUseCustomOutputPath.Checked;
        _guiSettings.CustomOutputPath = txtCustomOutputPath.Text.Trim();
        _guiSettings.WindowX = Location.X;
        _guiSettings.WindowY = Location.Y;
        _guiSettings.WindowWidth = Size.Width;
        _guiSettings.WindowHeight = Size.Height;

        if (cmbNotebookPicker.SelectedItem is NotebookInfo nb)
            _guiSettings.LastNotebookNumber = nb.Number;

        GuiSettingsStore.Save(_guiSettings);
    }

    private void BtnBrowse_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择 OneNoteMdExporter 所在目录",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(txtExporterDirectory.Text) ? txtExporterDirectory.Text : Environment.CurrentDirectory
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtExporterDirectory.Text = dialog.SelectedPath;
            DetectExporterDirectory(loadSettings: true);
        }
    }

    private void BtnDetect_Click(object? sender, EventArgs e)
    {
        DetectExporterDirectory(loadSettings: true);
    }

    private async void BtnScanNotebooks_Click(object? sender, EventArgs e)
    {
        if (!EnsureLocation()) return;

        btnScanNotebooks.Enabled = false;
        btnStartExport.Enabled = false;
        _scanCancellation = new CancellationTokenSource();
        AppendLog("开始扫描 OneNote 笔记本列表...");

        try
        {
            var scanner = new NotebookScanner();
            scanner.LineReceived += line => SafeUi(() => AppendLog(line));
            var notebooks = await scanner.ScanAsync(_location!, _scanCancellation.Token);
            ApplyNotebookChoices(notebooks);

            if (notebooks.Count == 0)
            {
                MessageBox.Show(this, "没有扫描到笔记本。请确认桌面版 OneNote 已打开，并且笔记本已加载。", "扫描结果", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                AppendLog($"已扫描到 {notebooks.Count} 个可选项。");
            }
        }
        catch (OperationCanceledException)
        {
            AppendLog("笔记本扫描已取消。");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "扫描笔记本失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _scanCancellation.Dispose();
            _scanCancellation = null;
            btnScanNotebooks.Enabled = true;
            btnStartExport.Enabled = true;
        }
    }

    private void BtnSettings_Click(object? sender, EventArgs e)
    {
        if (!EnsureLocation()) return;

        var model = _appSettings ?? new AppSettingsModel();
        using var form = new SettingsForm(model);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _appSettings = form.Result;
            try
            {
                var backupPath = _settingsEditor.Save(_location!.SettingsPath, _appSettings);
                AppendLog($"高级设置已保存，备份文件：{backupPath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "保存配置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private void BtnBrowseCustomOutput_Click(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "选择自定义导出目录",
            UseDescriptionForTitle = true,
            SelectedPath = Directory.Exists(txtCustomOutputPath.Text) ? txtCustomOutputPath.Text : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtCustomOutputPath.Text = dialog.SelectedPath;
            chkUseCustomOutputPath.Checked = true;
        }
    }

    private async void BtnStartExport_Click(object? sender, EventArgs e)
    {
        if (!EnsureLocation()) return;

        var validationErrors = ValidationService.ValidateBeforeExport(_location!, GetSelectedNotebookNumber());
        if (validationErrors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, validationErrors), "不能开始导出", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_appSettings is not null)
        {
            try
            {
                _settingsEditor.Save(_location!.SettingsPath, _appSettings);
                AppendLog("开始导出前已保存当前配置。");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "配置保存失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        var options = new ExportOptions
        {
            ExporterDirectory = _location!.Directory,
            NotebookNumber = GetSelectedNotebookNumber(),
            CustomOutputDirectory = chkUseCustomOutputPath.Checked ? txtCustomOutputPath.Text.Trim() : null,
            Format = ((ComboItem<ExportFormat>)cmbExportFormat.SelectedItem!).Value,
            SkipAdvancedSettings = true
        };

        _exportCancellation = new CancellationTokenSource();
        _runner?.Dispose();
        _runner = new ExportProcessRunner();
        _runner.LineReceived += line => SafeUi(() => AppendLog(line));
        _runner.ProgressChanged += progress => SafeUi(() => ApplyProgress(progress));
        _runner.Exited += exitCode => SafeUi(() => FinishExport(exitCode));

        SetExportingUi(isExporting: true);
        txtLog.Clear();
        progressOverall.Value = 0;
        progressSection.Value = 0;
        progressPage.Value = 0;
        _lastExportPath = null;
        btnOpenExportPath.Enabled = false;

        try
        {
            await _runner.RunAsync(options, _exportCancellation.Token);
        }
        catch (OperationCanceledException)
        {
            AppendLog("导出已取消。请检查 OneNote 是否留下临时窗口。");
        }
        catch (Exception ex)
        {
            AppendLog("导出失败：" + ex.Message);
            MessageBox.Show(this, ex.Message, "导出失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetExportingUi(isExporting: false);
            _exportCancellation.Dispose();
            _exportCancellation = null;
        }
    }

    private void BtnCancelExport_Click(object? sender, EventArgs e)
    {
        if (_runner?.IsRunning == true && MessageBox.Show(this, "确定要取消当前导出吗？", "取消导出", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            _exportCancellation?.Cancel();
            _runner.Cancel();
        }
    }

    private void BtnOpenExportPath_Click(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_lastExportPath) && Directory.Exists(_lastExportPath))
        {
            Process.Start(new ProcessStartInfo { FileName = _lastExportPath, UseShellExecute = true });
        }
        else
        {
            MessageBox.Show(this, "导出目录不存在或尚未导出成功。", "无法打开", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_runner?.IsRunning == true)
        {
            var result = MessageBox.Show(this, "导出仍在进行。关闭窗口会取消导出，是否继续？", "确认关闭", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result != DialogResult.Yes)
            {
                e.Cancel = true;
                return;
            }
        }

        _exportCancellation?.Cancel();
        _scanCancellation?.Cancel();
        _runner?.Cancel();
        SaveGuiSettings();
    }

    private void DetectExporterDirectory(bool loadSettings)
    {
        try
        {
            _location = ExporterLocator.Locate(txtExporterDirectory.Text);
            if (_location.IsComplete)
            {
                lblDirectoryStatus.Text = "已找到 OneNoteMdExporter.exe、appSettings.json、pandoc.exe 和 Resources。";
                lblDirectoryStatus.ForeColor = Color.DarkGreen;
                AppendLog("导出器目录检测通过：" + _location.Directory);
                txtDefaultOutputPath.Text = Path.Combine(_location.Directory, "Exports");

                if (loadSettings)
                {
                    LoadAppSettings();
                    LoadNotebookChoicesFromLog();
                }
            }
            else
            {
                lblDirectoryStatus.Text = "目录不完整，缺少：" + string.Join("、", _location.MissingFiles);
                lblDirectoryStatus.ForeColor = Color.DarkRed;
            }
        }
        catch (Exception ex)
        {
            _location = null;
            lblDirectoryStatus.Text = "目录检测失败：" + ex.Message;
            lblDirectoryStatus.ForeColor = Color.DarkRed;
        }
    }

    private bool EnsureLocation()
    {
        if (_location is null || !string.Equals(_location.Directory, Path.GetFullPath(txtExporterDirectory.Text.Trim()), StringComparison.OrdinalIgnoreCase))
        {
            DetectExporterDirectory(loadSettings: false);
        }

        if (_location?.IsComplete == true) return true;

        MessageBox.Show(this, lblDirectoryStatus.Text, "导出器目录不可用", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    private void LoadNotebookChoicesFromLog()
    {
        if (_location is null) return;

        var notebooks = NotebookListParser.ParseLatestFromLog(_location.LogsPath);
        if (notebooks.Count > 0)
        {
            ApplyNotebookChoices(notebooks);
            AppendLog("已从历史日志加载笔记本列表。需要刷新时点击「扫描笔记本」。");
        }
    }

    private void ApplyNotebookChoices(IReadOnlyList<NotebookInfo> notebooks)
    {
        cmbNotebookPicker.Items.Clear();
        foreach (var notebook in notebooks)
        {
            cmbNotebookPicker.Items.Add(notebook);
        }

        if (cmbNotebookPicker.Items.Count > 0)
        {
            var preferredIndex = -1;

            if (_guiSettings.LastNotebookNumber.HasValue)
            {
                preferredIndex = cmbNotebookPicker.Items.Cast<NotebookInfo>().ToList()
                    .FindIndex(n => n.Number == _guiSettings.LastNotebookNumber.Value);
            }

            if (preferredIndex < 0)
            {
                preferredIndex = cmbNotebookPicker.Items.Cast<NotebookInfo>().ToList()
                    .FindIndex(n => n.Number > 0);
            }

            cmbNotebookPicker.SelectedIndex = preferredIndex >= 0 ? preferredIndex : 0;
        }
    }

    private string GetSelectedNotebookNumber()
    {
        if (cmbNotebookPicker.SelectedItem is NotebookInfo notebook)
            return notebook.Number.ToString();
        return string.Empty;
    }

    private void LoadAppSettings()
    {
        if (_location is null || !_location.IsComplete) return;

        try
        {
            _appSettings = _settingsEditor.Load(_location.SettingsPath);
            AppendLog("已读取配置：" + _location.SettingsPath);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "读取配置失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyProgress(ExportProgress progress)
    {
        lblState.Text = $"状态：{progress.Phase} ({progress.OverallPercent}%)";
        if (!string.IsNullOrWhiteSpace(progress.CurrentItem))
        {
            lblCurrentItem.Text = "当前项目：" + progress.CurrentItem;
        }

        progressOverall.Value = Math.Clamp(progress.OverallPercent, 0, 100);

        if (progress.CurrentSection.HasValue && progress.TotalSections is > 0)
        {
            progressSection.Value = Math.Clamp((int)Math.Round(progress.CurrentSection.Value * 100.0 / progress.TotalSections.Value), 0, 100);
        }

        if (progress.CurrentPage.HasValue && progress.TotalPages is > 0)
        {
            progressPage.Value = Math.Clamp((int)Math.Round(progress.CurrentPage.Value * 100.0 / progress.TotalPages.Value), 0, 100);
        }

        if (!string.IsNullOrWhiteSpace(progress.ExportPath))
        {
            _lastExportPath = progress.ExportPath;
            btnOpenExportPath.Enabled = Directory.Exists(_lastExportPath);
        }

        if (progress.State == ExportState.Completed)
        {
            progressOverall.Value = 100;
        }
    }

    private void FinishExport(int? exitCode)
    {
        if (exitCode == 0)
        {
            AppendLog("进程已正常退出。" + (_lastExportPath is null ? string.Empty : " 导出目录：" + _lastExportPath));
            lblState.Text = "状态：导出完成，可以开始下一次导出";
            btnStartExport.Enabled = true;
            btnCancelExport.Enabled = false;
        }
        else if (exitCode.HasValue)
        {
            AppendLog("进程退出码：" + exitCode.Value);
        }

        btnOpenExportPath.Enabled = !string.IsNullOrWhiteSpace(_lastExportPath) && Directory.Exists(_lastExportPath);
    }

    private void SetExportingUi(bool isExporting)
    {
        btnStartExport.Enabled = !isExporting;
        btnCancelExport.Enabled = isExporting;
        btnBrowse.Enabled = !isExporting;
        btnDetect.Enabled = !isExporting;
        btnScanNotebooks.Enabled = !isExporting;
        btnSettings.Enabled = !isExporting;
        chkUseCustomOutputPath.Enabled = !isExporting;
        txtCustomOutputPath.Enabled = !isExporting;
        btnBrowseCustomOutput.Enabled = !isExporting;
    }

    private void AppendLog(string message)
    {
        if (txtLog.TextLength > 250_000)
        {
            txtLog.Text = txtLog.Text[^120_000..];
            txtLog.SelectionStart = txtLog.TextLength;
        }

        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void SafeUi(Action action)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(action); return; }
        action();
    }

    private sealed record ComboItem<T>(string Text, T Value)
    {
        public override string ToString() => Text;
    }
}
