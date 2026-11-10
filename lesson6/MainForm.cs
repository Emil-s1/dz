using System.Numerics;

namespace MultithreadingAsyncApp;

public sealed class MainForm : Form
{
    private readonly TextBox _fromBox = new() { Width = 100, PlaceholderText = "2" };
    private readonly TextBox _toBox = new() { Width = 100, PlaceholderText = "Без границы" };
    private readonly TextBox _primesOutput = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };
    private readonly TextBox _fibonacciOutput = new() { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill };

    private CancellationTokenSource? _primeCts;
    private CancellationTokenSource? _fibCts;
    private ManualResetEventSlim? _primePause;
    private ManualResetEventSlim? _fibPause;
    private Task? _primeTask;
    private Task? _fibTask;

    public MainForm()
    {
        Text = "Многопоточность и асинхронность — простые числа и Фибоначчи";
        Width = 1000;
        Height = 650;
        MinimumSize = new Size(800, 500);
        StartPosition = FormStartPosition.CenterScreen;

        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 85, Padding = new Padding(10), WrapContents = true };
        top.Controls.Add(new Label { Text = "От:", AutoSize = true, Margin = new Padding(3, 9, 3, 3) });
        top.Controls.Add(_fromBox);
        top.Controls.Add(new Label { Text = "До:", AutoSize = true, Margin = new Padding(12, 9, 3, 3) });
        top.Controls.Add(_toBox);

        var start = MakeButton("Запустить", (_, _) => StartBoth());
        var stopPrime = MakeButton("Остановить простые", (_, _) => StopPrime());
        var pausePrime = MakeButton("Пауза простых", (_, _) => _primePause?.Reset());
        var resumePrime = MakeButton("Продолжить простые", (_, _) => _primePause?.Set());
        var stopFib = MakeButton("Остановить Фибоначчи", (_, _) => StopFib());
        var pauseFib = MakeButton("Пауза Фибоначчи", (_, _) => _fibPause?.Reset());
        var resumeFib = MakeButton("Продолжить Фибоначчи", (_, _) => _fibPause?.Set());
        var restart = MakeButton("Полный рестарт", (_, _) => RestartBoth());

        top.Controls.AddRange(new Control[] { start, stopPrime, pausePrime, resumePrime, stopFib, pauseFib, resumeFib, restart });

        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 480 };
        split.Panel1.Controls.Add(_primesOutput);
        split.Panel2.Controls.Add(_fibonacciOutput);
        split.Panel1.Controls.Add(new Label { Text = "Простые числа", Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter });
        split.Panel2.Controls.Add(new Label { Text = "Числа Фибоначчи", Dock = DockStyle.Top, Height = 28, TextAlign = ContentAlignment.MiddleCenter });

        Controls.Add(split);
        Controls.Add(top);
        FormClosing += (_, _) => { StopPrime(); StopFib(); };
    }

    private static Button MakeButton(string text, EventHandler click)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 30, Margin = new Padding(4) };
        button.Click += click;
        return button;
    }

    private (long min, long? max) ReadBounds()
    {
        long min = 2;
        long? max = null;

        if (!string.IsNullOrWhiteSpace(_fromBox.Text) &&
            (!long.TryParse(_fromBox.Text, out min)))
            throw new ArgumentException("Нижняя граница должна быть целым числом типа long.");

        if (!string.IsNullOrWhiteSpace(_toBox.Text))
        {
            if (!long.TryParse(_toBox.Text, out long parsedMax))
                throw new ArgumentException("Верхняя граница должна быть целым числом типа long.");
            max = parsedMax;
        }

        if (max.HasValue && min > max.Value)
            throw new ArgumentException("Нижняя граница не может быть больше верхней.");

        return (min, max);
    }

    private void StartBoth()
    {
        try
        {
            var bounds = ReadBounds();
            StartPrime(bounds.min, bounds.max);
            StartFib(bounds.min, bounds.max);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void RestartBoth()
    {
        try
        {
            var bounds = ReadBounds();
            StopPrime();
            StopFib();
            _primesOutput.Clear();
            _fibonacciOutput.Clear();
            StartPrime(bounds.min, bounds.max);
            StartFib(bounds.min, bounds.max);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void StartPrime(long min, long? max)
    {
        StopPrime();
        _primeCts = new CancellationTokenSource();
        _primePause = new ManualResetEventSlim(true);
        var token = _primeCts.Token;
        var pause = _primePause;
        _primeTask = Task.Run(() =>
        {
            long candidate = Math.Max(2, min);
            while (!token.IsCancellationRequested && (!max.HasValue || candidate <= max.Value))
            {
                pause.Wait(token);
                if (IsPrime(candidate))
                    Append(_primesOutput, candidate.ToString());
                if (candidate == long.MaxValue) break;
                candidate++;
            }
        }, token);
    }

    private void StartFib(long min, long? max)
    {
        StopFib();
        _fibCts = new CancellationTokenSource();
        _fibPause = new ManualResetEventSlim(true);
        var token = _fibCts.Token;
        var pause = _fibPause;
        _fibTask = Task.Run(() =>
        {
            BigInteger a = 0, b = 1;
            while (!token.IsCancellationRequested)
            {
                pause.Wait(token);
                if (a >= min && (!max.HasValue || a <= max.Value))
                    Append(_fibonacciOutput, a.ToString());

                if (max.HasValue && a > max.Value) break;
                BigInteger next = a + b;
                a = b;
                b = next;
                // При заданной верхней границе последовательность завершится автоматически.
                // Без верхней границы BigInteger позволяет продолжать вычисления без переполнения.
            }
        }, token);
    }

    private static bool IsPrime(long number)
    {
        if (number < 2) return false;
        if (number == 2) return true;
        if (number % 2 == 0) return false;
        for (long divisor = 3; divisor <= number / divisor; divisor += 2)
            if (number % divisor == 0) return false;
        return true;
    }

    private void Append(TextBox box, string value)
    {
        if (box.IsDisposed || !box.IsHandleCreated) return;
        try
        {
            box.BeginInvoke(() =>
            {
                if (!box.IsDisposed)
                {
                    // Не даём окну бесконечно разрастаться при генерации без верхней границы.
                    if (box.TextLength > 200_000) box.Clear();
                    box.AppendText(value + Environment.NewLine);
                }
            });
        }
        catch (InvalidOperationException) { }
    }

    private void StopPrime()
    {
        var cts = _primeCts;
        var pause = _primePause;
        var task = _primeTask;
        _primeCts = null;
        _primePause = null;
        _primeTask = null;
        if (cts is null) return;

        cts.Cancel();
        pause?.Set(); // Разбудить поток, если он находился на паузе.
        _ = (task ?? Task.CompletedTask).ContinueWith(_ =>
        {
            pause?.Dispose();
            cts.Dispose();
        }, TaskScheduler.Default);
    }

    private void StopFib()
    {
        var cts = _fibCts;
        var pause = _fibPause;
        var task = _fibTask;
        _fibCts = null;
        _fibPause = null;
        _fibTask = null;
        if (cts is null) return;

        cts.Cancel();
        pause?.Set();
        _ = (task ?? Task.CompletedTask).ContinueWith(_ =>
        {
            pause?.Dispose();
            cts.Dispose();
        }, TaskScheduler.Default);
    }
}
