namespace OneNoteExporter;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private TextBox txtExporterDirectory;
    private Button btnBrowse;
    private Button btnDetect;
    private Label lblDirectoryStatus;
    private ComboBox cmbNotebookPicker;
    private Button btnScanNotebooks;
    private Button btnSettings;
    private ComboBox cmbExportFormat;
    private Button btnStartExport;
    private Button btnCancelExport;
    private Button btnOpenExportPath;
    private Label lblState;
    private Label lblCurrentItem;
    private ProgressBar progressOverall;
    private ProgressBar progressSection;
    private ProgressBar progressPage;
    private TextBox txtLog;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _runner?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        txtExporterDirectory = new TextBox();
        btnBrowse = new Button();
        btnDetect = new Button();
        lblDirectoryStatus = new Label();
        cmbNotebookPicker = new ComboBox();
        btnScanNotebooks = new Button();
        btnSettings = new Button();
        cmbExportFormat = new ComboBox();
        txtDefaultOutputPath = new TextBox();
        txtCustomOutputPath = new TextBox();
        chkUseCustomOutputPath = new CheckBox();
        btnBrowseCustomOutput = new Button();
        btnStartExport = new Button();
        btnCancelExport = new Button();
        btnOpenExportPath = new Button();
        lblState = new Label();
        lblCurrentItem = new Label();
        progressOverall = new ProgressBar();
        progressSection = new ProgressBar();
        progressPage = new ProgressBar();
        txtLog = new TextBox();
        SuspendLayout();

        // === Section 1: Exporter directory (y=22) ===
        txtExporterDirectory.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtExporterDirectory.Location = new Point(140, 24);
        txtExporterDirectory.Size = new Size(660, 25);
        txtExporterDirectory.TabIndex = 0;
        btnBrowse.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowse.Location = new Point(808, 23);
        btnBrowse.Size = new Size(80, 28);
        btnBrowse.Text = "浏览";
        btnBrowse.TabIndex = 1;
        btnBrowse.Click += BtnBrowse_Click;
        btnDetect.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnDetect.Location = new Point(894, 23);
        btnDetect.Size = new Size(80, 28);
        btnDetect.Text = "检测";
        btnDetect.TabIndex = 2;
        btnDetect.Click += BtnDetect_Click;
        lblDirectoryStatus.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblDirectoryStatus.AutoEllipsis = true;
        lblDirectoryStatus.ForeColor = Color.DimGray;
        lblDirectoryStatus.Location = new Point(140, 56);
        lblDirectoryStatus.Size = new Size(834, 22);
        lblDirectoryStatus.Text = "请选择 OneNoteMdExporter 文件夹，或点击「检测」自动查找。";

        // === Section 2: Notebook and format (y=120) ===
        cmbNotebookPicker.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbNotebookPicker.Location = new Point(140, 122);
        cmbNotebookPicker.Size = new Size(320, 27);
        cmbNotebookPicker.TabIndex = 3;
        btnScanNotebooks.Location = new Point(470, 121);
        btnScanNotebooks.Size = new Size(110, 29);
        btnScanNotebooks.Text = "扫描笔记本";
        btnScanNotebooks.TabIndex = 4;
        btnScanNotebooks.Click += BtnScanNotebooks_Click;
        cmbExportFormat.DropDownStyle = ComboBoxStyle.DropDownList;
        cmbExportFormat.Location = new Point(650, 122);
        cmbExportFormat.Size = new Size(170, 27);
        cmbExportFormat.TabIndex = 5;
        btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSettings.Location = new Point(860, 121);
        btnSettings.Size = new Size(114, 29);
        btnSettings.Text = "高级设置...";
        btnSettings.TabIndex = 6;
        btnSettings.Click += BtnSettings_Click;

        // === Section 3: Output path (y=200) ===
        txtDefaultOutputPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtDefaultOutputPath.Location = new Point(140, 202);
        txtDefaultOutputPath.ReadOnly = true;
        txtDefaultOutputPath.Size = new Size(370, 25);
        txtDefaultOutputPath.TabIndex = 7;
        chkUseCustomOutputPath.AutoSize = true;
        chkUseCustomOutputPath.Location = new Point(520, 204);
        chkUseCustomOutputPath.Text = "自定义";
        chkUseCustomOutputPath.TabIndex = 8;
        txtCustomOutputPath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtCustomOutputPath.Location = new Point(596, 202);
        txtCustomOutputPath.Size = new Size(284, 25);
        txtCustomOutputPath.TabIndex = 9;
        btnBrowseCustomOutput.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnBrowseCustomOutput.Location = new Point(886, 201);
        btnBrowseCustomOutput.Size = new Size(88, 28);
        btnBrowseCustomOutput.Text = "浏览...";
        btnBrowseCustomOutput.TabIndex = 10;
        btnBrowseCustomOutput.Click += BtnBrowseCustomOutput_Click;

        // === Section 4: Actions and progress (y=280) ===
        btnStartExport.Location = new Point(140, 282);
        btnStartExport.Size = new Size(130, 38);
        btnStartExport.Text = "开始导出";
        btnStartExport.TabIndex = 11;
        btnStartExport.Click += BtnStartExport_Click;
        btnCancelExport.Enabled = false;
        btnCancelExport.Location = new Point(280, 282);
        btnCancelExport.Size = new Size(100, 38);
        btnCancelExport.Text = "取消";
        btnCancelExport.TabIndex = 12;
        btnCancelExport.Click += BtnCancelExport_Click;
        btnOpenExportPath.Enabled = false;
        btnOpenExportPath.Location = new Point(390, 282);
        btnOpenExportPath.Size = new Size(130, 38);
        btnOpenExportPath.Text = "打开导出目录";
        btnOpenExportPath.TabIndex = 13;
        btnOpenExportPath.Click += BtnOpenExportPath_Click;
        lblState.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblState.AutoEllipsis = true;
        lblState.Location = new Point(540, 290);
        lblState.Size = new Size(434, 22);
        lblState.Text = "状态：空闲";
        lblCurrentItem.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        lblCurrentItem.AutoEllipsis = true;
        lblCurrentItem.Location = new Point(140, 332);
        lblCurrentItem.Size = new Size(834, 22);
        lblCurrentItem.Text = "当前项目：-";
        progressOverall.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressOverall.Location = new Point(140, 362);
        progressOverall.Size = new Size(834, 20);
        progressSection.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressSection.Location = new Point(140, 390);
        progressSection.Size = new Size(834, 20);
        progressPage.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        progressPage.Location = new Point(140, 418);
        progressPage.Size = new Size(834, 20);

        // === Section 5: Log (y=460) ===
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        txtLog.Font = new Font("Microsoft YaHei UI", 9.5F);
        txtLog.Location = new Point(14, 486);
        txtLog.Multiline = true;
        txtLog.ReadOnly = true;
        txtLog.ScrollBars = ScrollBars.Both;
        txtLog.Size = new Size(960, 360);
        txtLog.WordWrap = false;
        txtLog.TabIndex = 14;

        // Section labels and hints
        AddSectionLabel("1. 导出器目录", 14, 26);
        AddHint("选择原 OneNoteMdExporter 文件夹。", 140, 82, 834);

        AddSectionLabel("2. 笔记本 / 格式", 14, 124);
        AddHint("先点「扫描笔记本」，再从下拉框选择。", 140, 156, 834);
        AddAutoLabel("格式", 608, 125);

        AddSectionLabel("3. 导出目录", 14, 204);
        AddHint("默认输出到原工具 Exports；勾选「自定义」后，导出完成会移动到指定目录。", 140, 232, 834);

        AddSectionLabel("4. 执行 / 进度", 14, 284);
        AddAutoLabel("当前项目", 60, 332);
        AddAutoLabel("总进度", 68, 362);
        AddAutoLabel("章节", 84, 390);
        AddAutoLabel("页面", 84, 418);

        AddSectionLabel("5. 输出日志", 14, 462);

        ClientSize = new Size(988, 860);
        Controls.Add(txtLog);
        Controls.Add(progressPage);
        Controls.Add(progressSection);
        Controls.Add(progressOverall);
        Controls.Add(lblCurrentItem);
        Controls.Add(lblState);
        Controls.Add(btnOpenExportPath);
        Controls.Add(btnCancelExport);
        Controls.Add(btnStartExport);
        Controls.Add(btnBrowseCustomOutput);
        Controls.Add(chkUseCustomOutputPath);
        Controls.Add(txtCustomOutputPath);
        Controls.Add(txtDefaultOutputPath);
        Controls.Add(cmbExportFormat);
        Controls.Add(btnSettings);
        Controls.Add(btnScanNotebooks);
        Controls.Add(cmbNotebookPicker);
        Controls.Add(lblDirectoryStatus);
        Controls.Add(btnDetect);
        Controls.Add(btnBrowse);
        Controls.Add(txtExporterDirectory);
        MinimumSize = new Size(1004, 896);
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "OneNote Markdown Exporter";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        Font = new Font("Microsoft YaHei UI", 10F);
        FormClosing += MainForm_FormClosing;
        ResumeLayout(false);
        PerformLayout();
    }

    private void AddSectionLabel(string text, int x, int y)
    {
        Controls.Add(new Label
        {
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
            Location = new Point(x, y),
            Text = text
        });
    }

    private void AddAutoLabel(string text, int x, int y)
    {
        Controls.Add(new Label
        {
            AutoSize = true,
            Location = new Point(x, y),
            Text = text
        });
    }

    private void AddHint(string text, int x, int y, int width)
    {
        Controls.Add(new Label
        {
            AutoSize = false,
            ForeColor = Color.DimGray,
            Location = new Point(x, y),
            Size = new Size(width, 20),
            Text = text
        });
    }
}
