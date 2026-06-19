# OneNoteExporter

将 OneNote 笔记本导出为 Markdown 文件的图形界面工具。基于 [OneNoteMdExporter](https://github.com/alxnbl/onenote-md-exporter) v1.6.0 封装。

## 快速开始

1. 确保桌面版 OneNote 已打开，并且要导出的笔记本已加载。
2. 双击 `run.bat`，首次运行会自动编译并将 GUI 与导出工具合并到 `dist/` 文件夹。
3. 程序启动后自动检测同目录下的 OneNoteMdExporter.exe；如果没找到，手动点「检测」或「浏览」选择。
4. 点「扫描笔记本」获取可导出的笔记本列表。
5. 从下拉框选择笔记本，选择导出格式，点「开始导出」。

## 环境要求

| 依赖 | 说明 |
|------|------|
| 桌面版 OneNote | Microsoft Store 版不支持 |
| Microsoft Word | 导出工具通过 Word + Pandoc 转换内容 |
| .NET 9 运行时 | framework-dependent 发布需要 |

> 不要以管理员权限运行。OneNote 和本工具都应以普通用户权限启动。

## 界面说明

### 导出器目录

OneNoteMdExporter.exe 所在文件夹。程序启动时会自动检测，通常不需要手动设置。

检测通过需要以下文件存在：
- `OneNoteMdExporter.exe` — 真正执行导出的控制台程序
- `appSettings.json` — 导出配置文件
- `pandoc\pandoc.exe` — 将 Word/HTML 转换成 Markdown 的工具
- `Resources\` — 语言资源文件

### 笔记本 / 格式

点「扫描笔记本」后，程序会临时启动导出工具读取 OneNote 中的笔记本列表。

- `[0]` 表示导出全部笔记本
- `[1]`、`[2]`... 表示对应的笔记本

导出格式：

- **Markdown** — 导出为 `.md` 文件和资源文件夹，适合 Obsidian、Typora、VS Code
- **Joplin Raw Folder** — 导出为 Joplin 可导入的目录格式

### 导出目录

默认输出到导出工具目录下的 `Exports` 文件夹。

勾选「自定义」可指定其他目录——导出完成后程序会自动把结果移动过去（原工具本身不支持直接指定输出路径）。

### 高级设置

点「高级设置...」打开配置对话框，分三个页签：

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
| 添加 YAML Front Matter | 在每个 .md 文件顶部添加 title、created 等元数据 |
| 移除 OneNote 页眉 | 去掉 OneNote 导出 API 自动生成的页眉内容，建议开启 |

**限制**

| 选项 | 说明 |
|------|------|
| 标题最大长度 | 超过此字符数的页面标题会被截断，避免 Windows 路径过长 |
| 文件名最大长度 | 生成的 Markdown 文件名最大字符数，建议保持 50 左右 |

### 导出过程

程序自动完成与 OneNoteMdExporter 控制台的全部交互：按回车 → 输入笔记本编号 → 选择格式 → 跳过高级设置 → 等待导出完成。

- **总进度**：扫描章节占 20%，导出页面占 80%
- **日志中 WRN** 是警告（部分资源提取不完整，通常不影响导出）
- **日志中 ERR** 是错误（需检查 OneNote、Word、权限或配置）

## 注意事项

- 大笔记本导出耗时较长，建议先用小笔记本测试。
- 密码保护的分区、手写内容、部分嵌入图片可能无法完整导出。
- 请始终保留 OneNote 原始备份，导出是单向操作。
- 每次导出前程序会自动备份 appSettings.json（带时间戳后缀）。

## 项目结构

```
OneNoteExporter/
├── OneNoteExporter.sln
├── README.md
├── run.bat             # 一键编译+合并+启动
├── src/                # 源代码
│   ├── OneNoteExporter.csproj
│   ├── Program.cs
│   ├── Models.cs
│   ├── ExportEngine.cs
│   ├── AppServices.cs
│   ├── MainForm.cs
│   ├── MainForm.Designer.cs
│   └── SettingsForm.cs
└── dist/               # 发布输出（run.bat 生成）
    ├── OneNoteExporter.exe       (GUI)
    ├── OneNoteMdExporter.exe     (导出引擎)
    ├── appSettings.json
    ├── pandoc/
    ├── Resources/
    └── Exports/
```

## 构建

开发调试：

```bash
dotnet build src/OneNoteExporter.csproj
dotnet run --project src/OneNoteExporter.csproj
```

发布（自动合并导出工具）：

```bash
run.bat
```
