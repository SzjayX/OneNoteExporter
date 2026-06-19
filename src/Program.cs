namespace OneNoteExporter;

static class Program
{
    [STAThread]
    static void Main()
    {
        const string mutexName = "OneNoteExporter_SingleInstance";
        using var mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("OneNote Markdown 导出器 GUI 已在运行中。", "重复启动", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Application.ThreadException += (_, e) =>
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.Exception}\n\n");
            }
            catch { }

            MessageBox.Show($"发生未处理的异常：\n\n{e.Exception.Message}\n\n详细信息已写入 crash.log",
                "程序错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "crash.log");
                File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {e.ExceptionObject}\n\n");
            }
            catch { }

            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"发生严重错误：\n\n{ex.Message}\n\n详细信息已写入 crash.log",
                    "程序错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}
