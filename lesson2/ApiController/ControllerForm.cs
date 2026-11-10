using System.Runtime.InteropServices;

namespace ApiController;

public sealed class ControllerForm : Form
{
    private const string DefaultTargetCaption = "Наследованный код — целевое окно";
    private const int WM_SETTEXT = 0x000C;
    private const int WM_CLOSE = 0x0010;
    private const int WM_SYSCOMMAND = 0x0112;
    private const int SC_MINIMIZE = 0xF020;
    private const int GWL_STYLE = -16;
    private const long WS_DISABLED = 0x08000000L;

    private readonly TextBox _windowTitle = new() { Width = 310, Text = DefaultTargetCaption };
    private readonly TextBox _newTitle = new() { Width = 310, Text = "Новое название окна" };
    private readonly ComboBox _windowAction = new() { Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _beepCount = new() { Minimum = 1, Maximum = 10, Value = 5, Width = 70 };
    private readonly NumericUpDown _interval = new() { Minimum = 200, Maximum = 5000, Value = 1000, Increment = 200, Width = 90 };
    private readonly Button _styleButton = new() { Text = "Переключить стиль кнопок (вкл./выкл.)", AutoSize = true };
    private readonly TextBox _log = new()
    {
        Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical,
        Dock = DockStyle.Fill, Font = new Font("Consolas", 9)
    };
    private CancellationTokenSource? _beepCancellation;
    private bool _buttonsDisabled;

    public ControllerForm()
    {
        Text = "ApiController — управление через Windows API";
        Width = 850;
        Height = 680;
        MinimumSize = new Size(700, 550);
        StartPosition = FormStartPosition.CenterScreen;

        _windowAction.Items.AddRange(new object[] { "Изменить заголовок", "Закрыть окно", "Свернуть окно" });
        _windowAction.SelectedIndex = 0;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(12), ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var findPanel = MakePanel("Задание 2 — FindWindow и SendMessage");
        findPanel.Controls.Add(new Label { Text = "Заголовок целевого окна:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        findPanel.Controls.Add(_windowTitle);
        findPanel.Controls.Add(new Label { Text = "Действие:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        findPanel.Controls.Add(_windowAction);
        findPanel.Controls.Add(new Label { Text = "Новый заголовок:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        findPanel.Controls.Add(_newTitle);
        findPanel.Controls.Add(MakeButton("Найти окно и выполнить", (_, _) => FindAndControlWindow()));

        var beepPanel = MakePanel("Задание 3 — Beep и MessageBeep");
        beepPanel.Controls.Add(new Label { Text = "Количество сигналов:", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        beepPanel.Controls.Add(_beepCount);
        beepPanel.Controls.Add(new Label { Text = "Интервал (мс):", AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        beepPanel.Controls.Add(_interval);
        beepPanel.Controls.Add(MakeButton("Запустить сигналы", async (_, _) => await PlayBeepsAsync()));
        beepPanel.Controls.Add(MakeButton("Остановить сигналы", (_, _) => _beepCancellation?.Cancel()));

        var stylePanel = MakePanel("Задание 4 — изменение стилей элементов управления");
        stylePanel.Controls.Add(new Label
        {
            Text = "Найдите целевое окно в задании 2, затем переключите стиль его кнопок.",
            AutoSize = true,
            Padding = new Padding(0, 7, 8, 0)
        });
        _styleButton.Click += (_, _) => ToggleTargetButtons();
        stylePanel.Controls.Add(_styleButton);

        root.Controls.Add(findPanel, 0, 0);
        root.Controls.Add(beepPanel, 0, 1);
        root.Controls.Add(stylePanel, 0, 2);
        root.Controls.Add(new Label { Text = "Журнал работы:", AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold), Margin = new Padding(3, 10, 3, 5) }, 0, 3);
        root.Controls.Add(_log, 0, 4);
        Controls.Add(root);

        FormClosing += (_, _) => _beepCancellation?.Cancel();
        Log("Запустите приложение TargetApp отдельно, затем используйте эту панель.");
    }

    private static FlowLayoutPanel MakePanel(string title)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top, AutoSize = true, WrapContents = true,
            Padding = new Padding(8), Margin = new Padding(3, 5, 3, 5),
            BorderStyle = BorderStyle.FixedSingle
        };
        panel.Controls.Add(new Label
        {
            Text = title, AutoSize = true, Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Margin = new Padding(3, 5, 12, 8)
        });
        return panel;
    }

    private static Button MakeButton(string text, EventHandler handler)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 30, Margin = new Padding(4) };
        button.Click += handler;
        return button;
    }

    private void Log(string text)
    {
        if (InvokeRequired) { BeginInvoke(() => Log(text)); return; }
        if (!IsDisposed) _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}{Environment.NewLine}");
    }

    private IntPtr FindTarget()
    {
        IntPtr handle = NativeMethods.FindWindowW(null, _windowTitle.Text);
        if (handle == IntPtr.Zero)
            Log("Окно не найдено. Проверьте заголовок и убедитесь, что TargetApp запущено.");
        else
            Log($"Окно найдено. HWND = 0x{handle.ToInt64():X}");
        return handle;
    }

    private void FindAndControlWindow()
    {
        IntPtr handle = FindTarget();
        if (handle == IntPtr.Zero) return;

        switch (_windowAction.SelectedIndex)
        {
            case 0:
                NativeMethods.SendMessageW(handle, WM_SETTEXT, IntPtr.Zero, _newTitle.Text);
                Log("Отправлено сообщение WM_SETTEXT. Заголовок окна изменён.");
                // После переименования обновляем поле для последующих операций поиска.
                _windowTitle.Text = _newTitle.Text;
                break;
            case 1:
                NativeMethods.SendMessageW(handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                Log("Отправлено сообщение WM_CLOSE. Окно получило команду закрытия.");
                break;
            case 2:
                NativeMethods.SendMessageW(handle, WM_SYSCOMMAND, (IntPtr)SC_MINIMIZE, IntPtr.Zero);
                Log("Отправлено сообщение WM_SYSCOMMAND / SC_MINIMIZE.");
                break;
        }
    }

    private async Task PlayBeepsAsync()
    {
        _beepCancellation?.Cancel();
        _beepCancellation?.Dispose();
        _beepCancellation = new CancellationTokenSource();
        var token = _beepCancellation.Token;
        int count = (int)_beepCount.Value;
        int delay = (int)_interval.Value;

        try
        {
            Log($"Начата последовательность из {count} звуковых сигналов.");
            for (int i = 0; i < count; i++)
            {
                token.ThrowIfCancellationRequested();
                bool beepOk = NativeMethods.Beep((uint)(600 + (i % 4) * 250), 250);
                bool messageOk = NativeMethods.MessageBeep(0x00000040);
                Log($"Сигнал {i + 1}: Beep={beepOk}, MessageBeep={messageOk}");
                await Task.Delay(delay, token);
            }
            Log("Последовательность звуковых сигналов завершена.");
        }
        catch (OperationCanceledException) { Log("Последовательность сигналов остановлена пользователем."); }
    }

    private void ToggleTargetButtons()
    {
        IntPtr target = FindTarget();
        if (target == IntPtr.Zero) return;

        int changed = 0;
        NativeMethods.EnumChildWindows(target, (child, _) =>
        {
            var className = new System.Text.StringBuilder(128);
            NativeMethods.GetClassNameW(child, className, className.Capacity);

            if (className.ToString().Equals("Button", StringComparison.OrdinalIgnoreCase))
            {
                long style = NativeMethods.GetWindowLongW(child, GWL_STYLE).ToInt64();
                long newStyle = _buttonsDisabled ? (style & ~WS_DISABLED) : (style | WS_DISABLED);
                NativeMethods.SetWindowLongW(child, GWL_STYLE, new IntPtr(newStyle));
                NativeMethods.SetWindowPos(child, IntPtr.Zero, 0, 0, 0, 0,
                    0x0001 | 0x0002 | 0x0004 | 0x0020); // NOSIZE | NOMOVE | NOZORDER | FRAMECHANGED
                changed++;
            }
            return true;
        }, IntPtr.Zero);

        _buttonsDisabled = !_buttonsDisabled;
        Log(changed == 0
            ? "В целевом окне не найдены элементы Button."
            : $"Изменён стиль WS_DISABLED у кнопок: {changed}. Теперь отключены: {_buttonsDisabled}.");
    }
}

internal static class NativeMethods
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "FindWindowW", SetLastError = true)]
    internal static extern IntPtr FindWindowW(string? className, string? windowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW", SetLastError = true)]
    internal static extern IntPtr SendMessageW(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

    [DllImport("user32.dll", EntryPoint = "SendMessageW", SetLastError = true)]
    internal static extern IntPtr SendMessageW(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool EnumChildWindows(IntPtr parent, EnumChildProc callback, IntPtr lParam);

    internal delegate bool EnumChildProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetClassNameW", SetLastError = true)]
    internal static extern int GetClassNameW(IntPtr hWnd, System.Text.StringBuilder className, int maxCount);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW", SetLastError = true)]
    internal static extern IntPtr GetWindowLongW(IntPtr hWnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    internal static extern IntPtr SetWindowLongW(IntPtr hWnd, int index, IntPtr newValue);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool Beep(uint frequency, uint duration);

    [DllImport("user32.dll", SetLastError = true)]
    internal static extern bool MessageBeep(uint type);
}
