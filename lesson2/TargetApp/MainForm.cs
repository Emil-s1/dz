using System.Runtime.InteropServices;

namespace TargetApp;

public sealed class MainForm : Form
{
    private const string DefaultCaption = "Наследованный код — целевое окно";

    private readonly TextBox _fullName = new() { Width = 260, Text = "Введите ФИО" };
    private readonly TextBox _group = new() { Width = 260, Text = "Введите группу" };
    private readonly TextBox _course = new() { Width = 260, Text = "Системное программирование" };

    public MainForm()
    {
        Text = DefaultCaption;
        Name = "TargetWindow";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 650;
        Height = 430;
        MinimumSize = new Size(550, 350);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 2,
            RowCount = 6
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var heading = new Label
        {
            Text = "Приложение для демонстрации Windows API",
            AutoSize = true,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Margin = new Padding(3, 3, 3, 18)
        };
        root.Controls.Add(heading);
        root.SetColumnSpan(heading, 2);

        AddRow(root, 1, "ФИО:", _fullName);
        AddRow(root, 2, "Группа:", _group);
        AddRow(root, 3, "Курс:", _course);

        var messageButton = new Button
        {
            Text = "Задание 1 — показать информацию через MessageBox",
            AutoSize = true,
            Height = 38,
            Name = "btnMessageBoxes"
        };
        messageButton.Click += (_, _) => ShowInformation();
        root.Controls.Add(messageButton, 0, 4);
        root.SetColumnSpan(messageButton, 2);

        var info = new Label
        {
            Text = "Это целевое окно. Второе приложение ApiController может найти его,\nизменить заголовок, закрыть окно и изменить стиль кнопок.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Margin = new Padding(3, 18, 3, 3)
        };
        root.Controls.Add(info, 0, 5);
        root.SetColumnSpan(info, 2);

        Controls.Add(root);
    }

    private static void AddRow(TableLayoutPanel panel, int row, string caption, Control control)
    {
        panel.Controls.Add(new Label
        {
            Text = caption,
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 8, 12, 8)
        }, 0, row);
        control.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        control.Margin = new Padding(3, 5, 3, 5);
        panel.Controls.Add(control, 1, row);
    }

    private void ShowInformation()
    {
        // Задание 1: вызовы MessageBox из Windows API.
        NativeMethods.MessageBoxW(Handle, $"ФИО: {_fullName.Text}", "Информация — 1", 0x00000040);
        NativeMethods.MessageBoxW(Handle, $"Группа: {_group.Text}", "Информация — 2", 0x00000040);
        NativeMethods.MessageBoxW(Handle, $"Курс: {_course.Text}", "Информация — 3", 0x00000040);
    }
}

internal static class NativeMethods
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "MessageBoxW")]
    internal static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);
}
