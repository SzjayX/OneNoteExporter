namespace OneNoteExporter;

public sealed class SettingsForm : Form
{
    private readonly AppSettingsModel _model;
    private readonly ComboBox cmbPageHierarchy;
    private readonly ComboBox cmbIndentingStyle;
    private readonly ComboBox cmbResourceLocation;
    private readonly TextBox txtResourceFolderName;
    private readonly ComboBox cmbLinksHandling;
    private readonly TextBox txtPandocFormat;
    private readonly CheckBox chkHtmlStyling;
    private readonly CheckBox chkFrontMatter;
    private readonly CheckBox chkRemoveHeader;
    private readonly NumericUpDown numTitleMaxLength;
    private readonly NumericUpDown numFileMaxLength;

    public AppSettingsModel Result => BuildResult();

    public SettingsForm(AppSettingsModel model)
    {
        _model = model;
        Text = "高级设置";
        Size = new Size(580, 480);
        MinimumSize = new Size(500, 440);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Font = new Font("Microsoft YaHei UI", 9F);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        var panelBottom = new Panel { Dock = DockStyle.Bottom, Height = 50 };

        // Tab 1: Page Structure
        var tab1 = new TabPage("页面结构") { Padding = new Padding(12) };
        int y = 16;

        AddLabel(tab1, "页面层级处理", 12, y);
        cmbPageHierarchy = AddCombo(tab1, 140, y, 380,
            ("文件夹树形结构", "HierarchyAsFolderTree"),
            ("页面标题前缀", "HierarchyAsPageTitlePrefix"),
            ("忽略层级", "IgnoreHierarchy"));
        SelectByTag(cmbPageHierarchy, model.ProcessingOfPageHierarchy);
        y += 36;

        AddLabel(tab1, "缩进处理方式", 12, y);
        cmbIndentingStyle = AddCombo(tab1, 140, y, 380,
            ("保持原样", "LeaveAsIs"),
            ("转换为全角空格", "ConvertToEmSpaces"),
            ("转换为列表", "ConvertToBullets"));
        SelectByTag(cmbIndentingStyle, model.IndentingStyle);
        y += 36;

        AddLabel(tab1, "资源文件夹位置", 12, y);
        cmbResourceLocation = AddCombo(tab1, 140, y, 380,
            ("根目录", "RootFolder"),
            ("页面父目录", "PageParentFolder"));
        SelectByTag(cmbResourceLocation, model.ResourceFolderLocation);
        y += 36;

        AddLabel(tab1, "资源文件夹名", 12, y);
        txtResourceFolderName = new TextBox { Location = new Point(140, y), Size = new Size(380, 23), Text = model.ResourceFolderName };
        tab1.Controls.Add(txtResourceFolderName);
        y += 36;

        AddDescription(tab1, "资源文件（图片等）存放的子文件夹名称。", 140, y);

        // Tab 2: Conversion Options
        var tab2 = new TabPage("转换选项") { Padding = new Padding(12) };
        y = 16;

        AddLabel(tab2, "链接处理", 12, y);
        cmbLinksHandling = AddCombo(tab2, 140, y, 380,
            ("保持原始链接", "KeepOriginal"),
            ("转换为 Markdown", "ConvertToMarkdown"),
            ("转换为 Wikilink", "ConvertToWikilink"),
            ("移除链接", "Remove"));
        SelectByTag(cmbLinksHandling, model.OneNoteLinksHandling);
        y += 36;

        AddLabel(tab2, "Pandoc 格式", 12, y);
        txtPandocFormat = new TextBox { Location = new Point(140, y), Size = new Size(380, 23), Text = model.PanDocMarkdownFormat };
        tab2.Controls.Add(txtPandocFormat);
        y += 36;

        AddDescription(tab2, "Pandoc 输出格式标识，如 gfm、markdown、commonmark 等。", 140, y);
        y += 30;

        chkHtmlStyling = new CheckBox { Text = "保留 HTML 样式标签", Location = new Point(140, y), AutoSize = true, Checked = model.UseHtmlStyling };
        tab2.Controls.Add(chkHtmlStyling);
        y += 30;

        chkFrontMatter = new CheckBox { Text = "添加 YAML Front Matter 元数据头", Location = new Point(140, y), AutoSize = true, Checked = model.AddFrontMatterHeader };
        tab2.Controls.Add(chkFrontMatter);
        y += 30;

        chkRemoveHeader = new CheckBox { Text = "移除 OneNote 自动生成的页眉", Location = new Point(140, y), AutoSize = true, Checked = model.PostProcessingRemoveOneNoteHeader };
        tab2.Controls.Add(chkRemoveHeader);

        // Tab 3: Limits
        var tab3 = new TabPage("限制") { Padding = new Padding(12) };
        y = 16;

        AddLabel(tab3, "标题最大长度", 12, y);
        numTitleMaxLength = new NumericUpDown { Location = new Point(140, y), Size = new Size(100, 23), Minimum = 1, Maximum = 500, Value = Math.Clamp(model.PageTitleMaxLength, 1, 500) };
        tab3.Controls.Add(numTitleMaxLength);
        AddDescription(tab3, "字符数。超过此长度的页面标题将被截断。", 250, y + 2);
        y += 36;

        AddLabel(tab3, "文件名最大长度", 12, y);
        numFileMaxLength = new NumericUpDown { Location = new Point(140, y), Size = new Size(100, 23), Minimum = 1, Maximum = 500, Value = Math.Clamp(model.MdMaxFileLength, 1, 500) };
        tab3.Controls.Add(numFileMaxLength);
        AddDescription(tab3, "字符数。生成的 Markdown 文件名最大长度。", 250, y + 2);

        tabs.TabPages.AddRange([tab1, tab2, tab3]);

        var btnOk = new Button { Text = "保存", Size = new Size(90, 32), Anchor = AnchorStyles.Bottom | AnchorStyles.Right, DialogResult = DialogResult.OK };
        var btnCancel = new Button { Text = "取消", Size = new Size(90, 32), Anchor = AnchorStyles.Bottom | AnchorStyles.Right, DialogResult = DialogResult.Cancel };
        btnOk.Location = new Point(panelBottom.Width - 200, 10);
        btnCancel.Location = new Point(panelBottom.Width - 100, 10);

        panelBottom.Controls.AddRange([btnOk, btnCancel]);
        Controls.Add(tabs);
        Controls.Add(panelBottom);
        AcceptButton = btnOk;
        CancelButton = btnCancel;

        panelBottom.Resize += (_, _) =>
        {
            btnOk.Location = new Point(panelBottom.Width - 200, 10);
            btnCancel.Location = new Point(panelBottom.Width - 100, 10);
        };
    }

    private AppSettingsModel BuildResult() => new()
    {
        ProcessingOfPageHierarchy = GetTag(cmbPageHierarchy),
        IndentingStyle = GetTag(cmbIndentingStyle),
        ResourceFolderLocation = GetTag(cmbResourceLocation),
        ResourceFolderName = txtResourceFolderName.Text.Trim(),
        OneNoteLinksHandling = GetTag(cmbLinksHandling),
        PanDocMarkdownFormat = txtPandocFormat.Text.Trim(),
        UseHtmlStyling = chkHtmlStyling.Checked,
        AddFrontMatterHeader = chkFrontMatter.Checked,
        PostProcessingRemoveOneNoteHeader = chkRemoveHeader.Checked,
        PageTitleMaxLength = (int)numTitleMaxLength.Value,
        MdMaxFileLength = (int)numFileMaxLength.Value
    };

    private static void AddLabel(Control parent, string text, int x, int y)
    {
        parent.Controls.Add(new Label { Text = text, Location = new Point(x, y + 3), AutoSize = true });
    }

    private static void AddDescription(Control parent, string text, int x, int y)
    {
        parent.Controls.Add(new Label { Text = text, Location = new Point(x, y), AutoSize = true, ForeColor = Color.DimGray });
    }

    private static ComboBox AddCombo(Control parent, int x, int y, int width, params (string display, string tag)[] items)
    {
        var combo = new ComboBox { Location = new Point(x, y), Size = new Size(width, 25), DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var (display, tag) in items)
            combo.Items.Add(new TagItem(display, tag));
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
        parent.Controls.Add(combo);
        return combo;
    }

    private static void SelectByTag(ComboBox combo, string tag)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i] is TagItem item && string.Equals(item.Tag, tag, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedIndex = i;
                return;
            }
        }
    }

    private static string GetTag(ComboBox combo) =>
        combo.SelectedItem is TagItem item ? item.Tag : combo.Text;

    private sealed record TagItem(string Display, string Tag)
    {
        public override string ToString() => Display;
    }
}
