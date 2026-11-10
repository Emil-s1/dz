using System.Diagnostics;
using System.Globalization;

namespace ProcessManager;

public sealed class MainForm : Form
{
    private readonly TextBox _log = new()
    {
        Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill, Font = new Font("Consolas", 10)
    };
    private readonly TextBox _number1 = new() { Width = 90, Text = "7" };
    private readonly TextBox _number2 = new() { Width = 90, Text = "3" };
    private readonly ComboBox _operation = new() { Width = 65, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _filePath = new() { Width = 300, PlaceholderText = "Полный путь к текстовому файлу" };
    private readonly TextBox _word = new() { Width = 130, Text = "bicycle" };
    private Process? _activeProcess;
    private readonly string _childExe;

    public MainForm()
    {
        Text = "Модуль 2 — Работа с процессами";
        Width = 950;
        Height = 700;
        MinimumSize = new Size(780, 550);
        StartPosition = FormStartPosition.CenterScreen;

        _childExe = Path.Combine(AppContext.BaseDirectory, "ChildProcess", "ChildProcess.exe");

        _operation.Items.AddRange(new object[] { "+", "-", "*", "/" });
        _operation.SelectedIndex = 0;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(10)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var task12 = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true };
        task12.Controls.Add(Header("Задания 1–2: запуск и управление процессом"));
        task12.SetFlowBreak(task12.Controls[^1], true);
        task12.Controls.Add(MakeButton("Задание 1 — запустить и ждать", async (_, _) => await RunAndWaitAsync()));
        task12.Controls.Add(MakeButton("Задание 2 — запустить и ждать", async (_, _) => await StartManagedProcessAsync()));
        task12.Controls.Add(MakeButton("Принудительно завершить", (_, _) => KillActiveProcess()));

        var task34 = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, WrapContents = true, Padding = new Padding(0, 10, 0, 5) };
        task34.Controls.Add(Header("Задание 3: аргументы командной строки"));
        task34.SetFlowBreak(task34.Controls[^1], true);
        task34.Controls.Add(new Label { Text = "Число 1:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        task34.Controls.Add(_number1);
        task34.Controls.Add(new Label { Text = "Операция:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        task34.Controls.Add(_operation);
        task34.Controls.Add(new Label { Text = "Число 2:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        task34.Controls.Add(_number2);
        task34.Controls.Add(MakeButton("Выполнить вычисление", async (_, _) => await RunCalculationAsync()));

        task34.Controls.Add(new Label { Text = "Задание 4 — файл:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        task34.Controls.Add(_filePath);
        task34.Controls.Add(MakeButton("Обзор...", (_, _) => ChooseFile()));
        task34.Controls.Add(new Label { Text = "Слово:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        task34.Controls.Add(_word);
        task34.Controls.Add(MakeButton("Найти слово", async (_, _) => await RunSearchAsync()));

        var outputPanel = new Panel { Dock = DockStyle.Fill };
        outputPanel.Controls.Add(_log);
        layout.Controls.Add(task12, 0, 0);
        layout.Controls.Add(task34, 0, 1);
        layout.Controls.Add(outputPanel, 0, 2);
        Controls.Add(layout);

        FormClosing += (_, _) =>
        {
            try { if (_activeProcess is { HasExited: false }) _activeProcess.Kill(true); }
            catch { /* Процесс мог завершиться одновременно с закрытием окна. */ }
        };

        Log("Готово. Выберите одно из заданий.");
        Log("Для задания 4 используйте текстовый файл в кодировке UTF-8 или обычный текстовый файл.");
    }

    private static Label Header(string text) => new()
    {
        Text = text, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Margin = new Padding(3, 8, 3, 8)
    };

    private static Button MakeButton(string text, EventHandler handler)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 32, Margin = new Padding(4) };
        button.Click += handler;
        return button;
    }

    private void Log(string message)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(() => Log(message)); return; }
        _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    private void CheckChild()
    {
        if (!File.Exists(_childExe))
            throw new FileNotFoundException("Не найден дочерний файл ChildProcess.exe. Пересоберите решение целиком.", _childExe);
    }

    private ProcessStartInfo CreateStartInfo(params string[] args)
    {
        CheckChild();
        var info = new ProcessStartInfo
        {
            FileName = _childExe,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (string arg in args) info.ArgumentList.Add(arg);
        return info;
    }

    private async Task RunAndWaitAsync()
    {
        try
        {
            using var process = new Process { StartInfo = CreateStartInfo("--demo") };
            process.Start();
            Log($"Задание 1: дочерний процесс запущен. PID = {process.Id}");
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Log((await stdout).Trim());
            string error = (await stderr).Trim();
            if (error.Length > 0) Log("Ошибка дочернего процесса: " + error);
            Log($"Дочерний процесс завершён. Код завершения: {process.ExitCode}");
        }
        catch (Exception ex) { Log("Ошибка: " + ex.Message); }
    }

    private async Task StartManagedProcessAsync()
    {
        try
        {
            if (_activeProcess is { HasExited: false })
            {
                Log("Сначала завершите уже запущенный управляемый процесс.");
                return;
            }

            var process = new Process { StartInfo = CreateStartInfo("--demo") };
            process.Start();
            _activeProcess = process;
            Log($"Задание 2: дочерний процесс запущен. PID = {process.Id}");
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Log((await stdout).Trim());
            string error = (await stderr).Trim();
            if (error.Length > 0) Log("Ошибка дочернего процесса: " + error);
            Log($"Процесс завершён. Код завершения: {process.ExitCode}");
            if (ReferenceEquals(_activeProcess, process)) _activeProcess = null;
            process.Dispose();
        }
        catch (Exception ex) { Log("Ошибка: " + ex.Message); }
    }

    private void KillActiveProcess()
    {
        try
        {
            if (_activeProcess is null || _activeProcess.HasExited)
            {
                Log("Активного дочернего процесса нет.");
                return;
            }
            int pid = _activeProcess.Id;
            _activeProcess.Kill(true);
            Log($"Отправлена команда принудительного завершения процесса PID = {pid}.");
        }
        catch (Exception ex) { Log("Не удалось завершить процесс: " + ex.Message); }
    }

    private async Task RunCalculationAsync()
    {
        if (!double.TryParse(_number1.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double a) ||
            !double.TryParse(_number2.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double b))
        {
            Log("Введите корректные числа. Для дробных чисел используйте точку, например 2.5.");
            return;
        }

        try
        {
            string op = _operation.SelectedItem?.ToString() ?? "+";
            using var process = new Process { StartInfo = CreateStartInfo("--calc", a.ToString(CultureInfo.InvariantCulture), op, b.ToString(CultureInfo.InvariantCulture)) };
            process.Start();
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Log("Задание 3:");
            Log((await stdout).Trim());
            string error = (await stderr).Trim();
            if (error.Length > 0) Log("Ошибка: " + error);
            Log($"Код завершения: {process.ExitCode}");
        }
        catch (Exception ex) { Log("Ошибка: " + ex.Message); }
    }

    private void ChooseFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Выберите текстовый файл",
            Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) _filePath.Text = dialog.FileName;
    }

    private async Task RunSearchAsync()
    {
        string path = _filePath.Text.Trim();
        string word = _word.Text;
        if (path.Length == 0 || !File.Exists(path))
        {
            Log("Укажите существующий файл.");
            return;
        }
        if (word.Length == 0)
        {
            Log("Введите слово для поиска.");
            return;
        }

        try
        {
            using var process = new Process { StartInfo = CreateStartInfo("--count", path, word) };
            process.Start();
            Task<string> stdout = process.StandardOutput.ReadToEndAsync();
            Task<string> stderr = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            Log("Задание 4:");
            Log((await stdout).Trim());
            string error = (await stderr).Trim();
            if (error.Length > 0) Log("Ошибка: " + error);
            Log($"Код завершения: {process.ExitCode}");
        }
        catch (Exception ex) { Log("Ошибка: " + ex.Message); }
    }
}
