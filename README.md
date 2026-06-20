# OneNoteExporter

OneNoteExporter 是一个 Windows 图形界面工具，用来把桌面版 OneNote 笔记本导出为 Markdown 文件。它基于 [OneNoteMdExporter](https://github.com/alxnbl/onenote-md-exporter) v1.6.0 封装，自动完成控制台交互、配置写入、进度显示和导出结果整理。

## 功能特点

- 图形界面选择导出器目录、笔记本、导出格式和输出目录。
- 自动扫描 OneNote 中已加载的笔记本。
- 支持 Markdown 和 Joplin Raw Folder 两种导出格式。
- 支持页面层级、缩进、资源目录、链接转换、Front Matter 等高级设置。
- 支持自定义导出目录，导出完成后自动移动结果。
- 自动备份 `appSettings.json`，降低误改原配置的风险。

## 环境要求

| 依赖 | 说明 |
|------|------|
| Windows 10/11 | 当前项目是 WinForms 桌面应用 |
| 桌面版 OneNote | Microsoft Store 版不支持导出工具调用 |
| Microsoft Word | OneNoteMdExporter 通过 Word + Pandoc 转换内容 |
| .NET 9 SDK | 从源码运行 `run.bat` 或 `dotnet publish` 需要 |
| OneNoteMdExporter v1.6.0 | 实际执行 OneNote 导出的控制台工具 |

> 不要以管理员权限运行。OneNote、Word、OneNoteMdExporter 和本工具都应以普通用户权限启动，否则可能互相访问不到 COM 实例或用户配置。

## 准备工作

1. 安装并打开桌面版 OneNote。
2. 在 OneNote 中确认要导出的笔记本已经同步并加载完成。
3. 下载或解压 OneNoteMdExporter v1.6.0。
4. 推荐保持以下目录结构，`run.bat` 会按这个相对路径自动查找导出工具：

```text
note/
├── OneNoteExporter/
│   ├── run.bat
│   ├── README.md
│   └── src/
└── OneNoteMdExporter.v1.6.0/
    ├── OneNoteMdExporter.exe
    ├── appSettings.json
    ├── pandoc/
    └── Resources/
```

如果导出工具不在上述位置，也可以启动程序后在界面中点击「浏览」手动选择 `OneNoteMdExporter.exe` 所在目录。

## 快速开始

1. 双击 `run.bat`。
2. 首次运行会自动执行 `dotnet publish`，并把 GUI、OneNoteMdExporter、Pandoc 和资源文件合并到 `dist/`。
3. 程序启动后检查「导出器目录」是否检测通过。
4. 点击「扫描笔记本」。
5. 在「笔记本」下拉框中选择要导出的笔记本。
6. 选择导出格式和导出目录。
7. 点击「开始导出」，等待日志显示导出完成。

`run.bat` 生成的发布目录如下：

```text
dist/
├── OneNoteExporter.exe
├── OneNoteMdExporter.exe
├── appSettings.json
├── pandoc/
├── Resources/
└── Exports/
```

后续如果 `dist/` 已存在，也可以直接运行 `dist/OneNoteExporter.exe`。

## 界面说明

### 导出器目录

OneNoteMdExporter.exe 所在文件夹。程序启动时会自动检测，通常不需要手动设置。

检测通过需要以下文件存在：

- `OneNoteMdExporter.exe` — 真正执行导出的控制台程序
- `appSettings.json` — 导出配置文件
- `pandoc\pandoc.exe` — 将 Word/HTML 转换成 Markdown 的工具
- `Resources\` — 语言资源文件

### 笔记本 / 格式

点击「扫描笔记本」后，程序会临时启动 OneNoteMdExporter 读取 OneNote 中的笔记本列表。

- `[0]` 表示导出全部笔记本
- `[1]`、`[2]`... 表示对应的单个笔记本

导出格式：

- **Markdown** — 导出为 `.md` 文件和资源文件夹，适合 Obsidian、Typora、VS Code
- **Joplin Raw Folder** — 导出为 Joplin 可导入的目录格式

### 导出目录

默认输出到导出工具目录下的 `Exports` 文件夹。

勾选「自定义」可指定其他目录。导出完成后程序会自动把结果移动过去，因为原始 OneNoteMdExporter 不支持直接指定输出路径。

### 高级设置

点击「高级设置...」打开配置对话框，分为三个页签。

**页面结构**

| 选项 | 说明 |
|------|------|
| 页面层级处理 | **文件夹树形结构**（推荐 Obsidian）：父页面/子页面.md；**页面标题前缀**：父页面_子页面.md；**忽略层级**：扁平导出 |
| 缩进处理 | **保持原样** / **转换为全角空格**（视觉更接近 OneNote）/ **转换为列表** |
| 资源文件夹位置 | **根目录**：所有图片集中存放；**页面旁边**：资源与对应页面放在一起 |
| 资源文件夹名称 | 图片和附件存放的子文件夹名，默认 `resources` |

**转换选项**

| 选项 | 说明 |
|------|------|
| 链接处理 | **保持原链接**（onenote://）/ **转 Markdown 链接**（适合 Joplin）/ **转 Wikilink**（推荐 Obsidian）/ **移除** |
| Pandoc 格式 | 默认 `gfm`（GitHub Flavored Markdown），也可选 `markdown`、`commonmark` 等 |
| 保留 HTML 样式 | 开启后颜色、复杂表格等用 HTML 标签保留，但 Markdown 会混入 HTML |
| 添加 YAML Front Matter | 在每个 `.md` 文件顶部添加 `title`、`created` 等元数据 |
| 移除 OneNote 页眉 | 去掉 OneNote 导出 API 自动生成的页眉内容，建议开启 |

**限制**

| 选项 | 说明 |
|------|------|
| 标题最大长度 | 超过此字符数的页面标题会被截断，避免 Windows 路径过长 |
| 文件名最大长度 | 生成的 Markdown 文件名最大字符数，建议保持 50 左右 |

## 导出流程

程序会自动完成 OneNoteMdExporter 控制台的交互流程：按回车 → 输入笔记本编号 → 选择格式 → 跳过原始高级设置 → 等待导出完成。

导出时可以通过日志和进度条判断状态：

- **总进度**：扫描章节约占 20%，导出页面约占 80%。
- **WRN**：警告，通常表示部分资源提取不完整，一般不影响主体 Markdown 导出。
- **ERR**：错误，需要检查 OneNote、Word、权限、导出器目录或配置文件。

## 常见问题

### 启动后提示找不到 OneNoteMdExporter.exe

确认 `OneNoteMdExporter.v1.6.0` 与 `OneNoteExporter` 在同一个上级目录下，或在界面中点击「浏览」手动选择导出器目录。

### 扫描不到笔记本

确认使用的是桌面版 OneNote，并且笔记本已经在 OneNote 中打开、同步完成。不要使用管理员权限启动本工具或 OneNote。

### 导出过程卡住或很慢

大笔记本、图片较多的页面、嵌入附件和复杂表格都会显著增加导出时间。建议先用小笔记本测试导出流程。

### Word 或 Pandoc 相关错误

确认 Microsoft Word 可以正常打开，并检查导出器目录下是否存在 `pandoc\pandoc.exe`。如果目录不完整，请重新解压 OneNoteMdExporter v1.6.0。

### 自定义导出目录为空

原始导出工具会先输出到导出器目录下的 `Exports`，本工具在导出结束后再移动结果。若导出失败或被取消，自定义目录可能不会生成完整结果。

## 注意事项

- 请始终保留 OneNote 原始备份，导出是单向操作。
- 密码保护的分区、手写内容、部分嵌入图片可能无法完整导出。
- 每次导出前程序会自动备份 `appSettings.json`，备份文件带时间戳后缀。
- `dist/` 是本地发布输出目录，不建议提交到版本库。
- `.claude/` 是本地 AI 助手使用记录目录，不应提交到版本库。

## 构建与运行

开发调试：

```bash
dotnet build src/OneNoteExporter.csproj
dotnet run --project src/OneNoteExporter.csproj
```

发布并启动：

```bash
run.bat
```

`run.bat` 会执行以下操作：

1. 发布 WinForms GUI 到 `dist/`。
2. 复制 `OneNoteMdExporter.exe`、`appSettings.json`、`pandoc/` 和 `Resources/`。
3. 创建 `dist/Exports/`。
4. 启动 `dist/OneNoteExporter.exe`。

## 项目结构

```text
OneNoteExporter/
├── OneNoteExporter.sln
├── README.md
├── run.bat
├── src/
│   ├── OneNoteExporter.csproj
│   ├── Program.cs
│   ├── Models.cs
│   ├── ExportEngine.cs
│   ├── AppServices.cs
│   ├── MainForm.cs
│   ├── MainForm.Designer.cs
│   └── SettingsForm.cs
└── dist/
    ├── OneNoteExporter.exe
    ├── OneNoteMdExporter.exe
    ├── appSettings.json
    ├── pandoc/
    ├── Resources/
    └── Exports/
```
