using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;

namespace MultithreadingAsyncPart2;

public sealed class MainForm : Form
{
    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    // Task 1 — dancing progress bars
    private readonly NumericUpDown _barCount = new() { Minimum = 1, Maximum = 30, Value = 8, Width = 80 };
    private readonly FlowLayoutPanel _barsPanel = new() { Dock = DockStyle.Fill, AutoScroll = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly Button _startBarsButton = new() { Text = "Старт", AutoSize = true };
    private readonly Button _stopBarsButton = new() { Text = "Остановить", AutoSize = true, Enabled = false };
    private CancellationTokenSource? _barsCts;

    // Task 2 — horse race
    private readonly ProgressBar[] _horseBars = new ProgressBar[5];
    private readonly Label[] _horseLabels = new Label[5];
    private readonly DataGridView _raceResults = new()
    {
        Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false,
        AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        RowHeadersVisible = false
    };
    private readonly Button _startRaceButton = new() { Text = "Старт скачки", AutoSize = true };
    private CancellationTokenSource? _raceCts;

    // Task 3 — Fibonacci
    private readonly TextBox _fibLimit = new() { Width = 180, Text = "100000" };
    private readonly TextBox _fibOutput = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly Button _fibButton = new() { Text = "Посчитать числа Фибоначчи", AutoSize = true };

    // Tasks 4 and 5 — file search
    private readonly TextBox _filePath = new() { Width = 390, PlaceholderText = "Путь к файлу" };
    private readonly TextBox _directoryPath = new() { Width = 390, PlaceholderText = "Путь к директории" };
    private readonly TextBox _searchWord = new() { Width = 180, PlaceholderText = "Слово для поиска" };
    private readonly TextBox _fileResult = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox _directoryResult = new() { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false };
    private readonly Button _searchFileButton = new() { Text = "Найти в файле", AutoSize = true };
    private readonly Button _searchDirectoryButton = new() { Text = "Найти во всей директории", AutoSize = true };
    private CancellationTokenSource? _searchCts;

    public MainForm()
    {
        Text = "Модуль 3 — Многопоточность и асинхронность (часть 2)";
        Width = 1100;
        Height = 760;
        MinimumSize = new Size(850, 600);
        StartPosition = FormStartPosition.CenterScreen;

        BuildDancingBarsTab();
        BuildRaceTab();
        BuildFibonacciTab();
        BuildFileSearchTab();
        BuildDirectorySearchTab();

        Controls.Add(_tabs);
        FormClosing += (_, _) =>
        {
            _barsCts?.Cancel();
            _raceCts?.Cancel();
            _searchCts?.Cancel();
        };
    }

    private void BuildDancingBarsTab()
    {
        var page = new TabPage("1. Танцующие прогресс-бары");
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 52, Padding = new Padding(8), WrapContents = true };
        top.Controls.Add(new Label { Text = "Количество полос:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        top.Controls.Add(_barCount);
        top.Controls.Add(_startBarsButton);
        top.Controls.Add(_stopBarsButton);
        _startBarsButton.Click += async (_, _) => await StartDancingBarsAsync();
        _stopBarsButton.Click += (_, _) => _barsCts?.Cancel();
        page.Controls.Add(_barsPanel);
        page.Controls.Add(top);
        _tabs.TabPages.Add(page);
    }

    private async Task StartDancingBarsAsync()
    {
        _barsCts?.Cancel();
        _barsCts?.Dispose();
        _barsCts = new CancellationTokenSource();
        var token = _barsCts.Token;
        _barsPanel.Controls.Clear();
        _startBarsButton.Enabled = false;
        _stopBarsButton.Enabled = true;

        int count = (int)_barCount.Value;
        var random = new Random();
        var jobs = new List<Task>();

        for (int i = 0; i < count; i++)
        {
            int index = i + 1;
            Color color = Color.FromArgb(random.Next(50, 230), random.Next(50, 230), random.Next(50, 230));
            var row = new FlowLayoutPanel { Width = 850, Height = 48, WrapContents = false, Padding = new Padding(4) };
            var label = new Label { Text = $"Поток {index}", Width = 90, TextAlign = ContentAlignment.MiddleLeft };
            var bar = new ProgressBar { Width = 650, Height = 25, Minimum = 0, Maximum = 100, Value = 0, Style = ProgressBarStyle.Continuous };
            row.Controls.Add(label);
            row.Controls.Add(bar);
            _barsPanel.Controls.Add(row);

            jobs.Add(Task.Run(async () =>
            {
                var rng = new Random(Guid.NewGuid().GetHashCode());
                int value = 0;
                while (value < 100 && !token.IsCancellationRequested)
                {
                    await Task.Delay(rng.Next(40, 220), token);
                    value = Math.Min(100, value + rng.Next(1, 13));
                    Ui(() =>
                    {
                        if (!bar.IsDisposed)
                        {
                            bar.Value = value;
                            bar.ForeColor = color;
                        }
                    });
                }
            }, token));
        }

        try { await Task.WhenAll(jobs); }
        catch (OperationCanceledException) { }
        finally
        {
            _startBarsButton.Enabled = true;
            _stopBarsButton.Enabled = false;
        }
    }

    private void BuildRaceTab()
    {
        var page = new TabPage("2. Конные скачки");
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 48, Padding = new Padding(8) };
        top.Controls.Add(_startRaceButton);
        _startRaceButton.Click += async (_, _) => await StartRaceAsync();

        var racePanel = new TableLayoutPanel { Dock = DockStyle.Top, Height = 260, ColumnCount = 2, Padding = new Padding(12) };
        racePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        racePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 5; i++)
        {
            racePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 20));
            _horseLabels[i] = new Label { Text = $"Лошадь {i + 1}", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            _horseBars[i] = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 0 };
            racePanel.Controls.Add(_horseLabels[i], 0, i);
            racePanel.Controls.Add(_horseBars[i], 1, i);
        }

        _raceResults.Columns.Add("Place", "Место");
        _raceResults.Columns.Add("Horse", "Лошадь");
        _raceResults.Columns.Add("Time", "Время, мс");

        var resultsGroup = new GroupBox { Text = "Результаты скачки", Dock = DockStyle.Fill, Padding = new Padding(8) };
        resultsGroup.Controls.Add(_raceResults);
        page.Controls.Add(resultsGroup);
        page.Controls.Add(racePanel);
        page.Controls.Add(top);
        _tabs.TabPages.Add(page);
    }

    private async Task StartRaceAsync()
    {
        _raceCts?.Cancel();
        _raceCts?.Dispose();
        _raceCts = new CancellationTokenSource();
        var token = _raceCts.Token;
        _startRaceButton.Enabled = false;
        _raceResults.Rows.Clear();

        foreach (var bar in _horseBars) bar.Value = 0;
        var finishOrder = new ConcurrentQueue<(int Horse, long Milliseconds)>();
        var stopwatch = Stopwatch.StartNew();

        Task[] horses = Enumerable.Range(0, 5).Select(horse => Task.Run(async () =>
        {
            var rng = new Random(Guid.NewGuid().GetHashCode());
            int progress = 0;
            while (progress < 100)
            {
                await Task.Delay(rng.Next(35, 180), token);
                progress = Math.Min(100, progress + rng.Next(1, 9));
                int current = progress;
                Ui(() => { if (!_horseBars[horse].IsDisposed) _horseBars[horse].Value = current; });
            }
            finishOrder.Enqueue((horse + 1, stopwatch.ElapsedMilliseconds));
        }, token)).ToArray();

        try
        {
            await Task.WhenAll(horses);
            int place = 1;
            foreach (var result in finishOrder.OrderBy(x => x.Milliseconds))
                _raceResults.Rows.Add(place++, $"Лошадь {result.Horse}", result.Milliseconds);
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show(this, "Гонка была остановлена.", "Скачки", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        finally { _startRaceButton.Enabled = true; }
    }

    private void BuildFibonacciTab()
    {
        var page = new TabPage("3. Числа Фибоначчи");
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 55, Padding = new Padding(8) };
        top.Controls.Add(new Label { Text = "Верхняя граница:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        top.Controls.Add(_fibLimit);
        top.Controls.Add(_fibButton);
        _fibButton.Click += async (_, _) => await CalculateFibonacciAsync();
        page.Controls.Add(_fibOutput);
        page.Controls.Add(top);
        _tabs.TabPages.Add(page);
    }

    private async Task CalculateFibonacciAsync()
    {
        if (!long.TryParse(_fibLimit.Text, out long limit) || limit < 0)
        {
            MessageBox.Show(this, "Введите неотрицательное целое число в диапазоне Int64.", "Ошибка ввода");
            return;
        }

        _fibButton.Enabled = false;
        _fibOutput.Clear();
        try
        {
            var result = await Task.Run(() =>
            {
                var values = new List<BigInteger>();
                BigInteger a = 0, b = 1;
                while (a <= limit)
                {
                    values.Add(a);
                    (a, b) = (b, a + b);
                }
                return values;
            });

            _fibOutput.Text = "Числа Фибоначчи от 0 до " + limit + ":" + Environment.NewLine +
                              string.Join(", ", result) + Environment.NewLine + Environment.NewLine +
                              "Количество чисел: " + result.Count;
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ошибка"); }
        finally { _fibButton.Enabled = true; }
    }

    private void BuildFileSearchTab()
    {
        var page = new TabPage("4. Поиск в файле");
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(8), WrapContents = true };
        top.Controls.Add(new Label { Text = "Файл:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        top.Controls.Add(_filePath);
        top.Controls.Add(MakeButton("Обзор...", (_, _) => ChooseFile()));
        top.Controls.Add(new Label { Text = "Слово:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        top.Controls.Add(_searchWord);
        top.Controls.Add(_searchFileButton);
        _searchFileButton.Click += async (_, _) => await SearchSingleFileAsync();

        page.Controls.Add(_fileResult);
        page.Controls.Add(top);
        _tabs.TabPages.Add(page);
    }

    private void BuildDirectorySearchTab()
    {
        var page = new TabPage("5. Поиск в директории");
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(8), WrapContents = true };
        top.Controls.Add(new Label { Text = "Директория:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        top.Controls.Add(_directoryPath);
        top.Controls.Add(MakeButton("Обзор...", (_, _) => ChooseDirectory()));
        top.Controls.Add(new Label { Text = "Слово:", AutoSize = true, Padding = new Padding(0, 8, 0, 0) });
        top.Controls.Add(_searchWord);
        top.Controls.Add(_searchDirectoryButton);
        top.Controls.Add(MakeButton("Отмена", (_, _) => _searchCts?.Cancel()));
        _searchDirectoryButton.Click += async (_, _) => await SearchDirectoryAsync();

        page.Controls.Add(_directoryResult);
        page.Controls.Add(top);
        _tabs.TabPages.Add(page);
    }

    private async Task SearchSingleFileAsync()
    {
        string path = _filePath.Text.Trim();
        string word = _searchWord.Text;
        if (!File.Exists(path)) { MessageBox.Show(this, "Выберите существующий файл."); return; }
        if (string.IsNullOrEmpty(word)) { MessageBox.Show(this, "Введите слово для поиска."); return; }

        _searchFileButton.Enabled = false;
        _fileResult.Clear();
        try
        {
            int count = await Task.Run(async () =>
            {
                string content = await File.ReadAllTextAsync(path);
                return CountOccurrences(content, word);
            });
            _fileResult.Text = $"Файл: {Path.GetFileName(path)}{Environment.NewLine}" +
                               $"Путь: {path}{Environment.NewLine}" +
                               $"Искомое слово: {word}{Environment.NewLine}" +
                               $"Количество вхождений: {count}";
        }
        catch (Exception ex) { _fileResult.Text = "Ошибка поиска: " + ex.Message; }
        finally { _searchFileButton.Enabled = true; }
    }

    private async Task SearchDirectoryAsync()
    {
        string directory = _directoryPath.Text.Trim();
        string word = _searchWord.Text;
        if (!Directory.Exists(directory)) { MessageBox.Show(this, "Выберите существующую директорию."); return; }
        if (string.IsNullOrEmpty(word)) { MessageBox.Show(this, "Введите слово для поиска."); return; }

        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        _searchDirectoryButton.Enabled = false;
        _directoryResult.Clear();

        try
        {
            var report = await Task.Run(async () =>
            {
                var lines = new List<string>();
                int filesScanned = 0;
                int filesWithMatches = 0;
                int errors = 0;

                // EnumerationOptions skips inaccessible folders and follows subdirectories.
                var options = new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true,
                    ReturnSpecialDirectories = false
                };

                foreach (string file in Directory.EnumerateFiles(directory, "*", options))
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        string text = await File.ReadAllTextAsync(file, token);
                        int count = CountOccurrences(text, word);
                        filesScanned++;
                        if (count > 0)
                        {
                            filesWithMatches++;
                            lines.Add($"Название файла: {Path.GetFileName(file)}");
                            lines.Add($"Путь к файлу: {file}");
                            lines.Add($"Количество вхождений слова: {count}");
                            lines.Add(new string('-', 70));
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch { errors++; }
                }

                string summary = $"Отчёт поиска: {word}{Environment.NewLine}" +
                                 $"Проверено файлов: {filesScanned}{Environment.NewLine}" +
                                 $"Файлов с совпадениями: {filesWithMatches}{Environment.NewLine}" +
                                 $"Файлов, которые не удалось прочитать: {errors}{Environment.NewLine}" +
                                 new string('=', 70) + Environment.NewLine;
                return summary + (lines.Count > 0 ? string.Join(Environment.NewLine, lines) : "Совпадений не найдено.");
            }, token);

            _directoryResult.Text = report;
        }
        catch (OperationCanceledException) { _directoryResult.Text = "Поиск отменён пользователем."; }
        catch (Exception ex) { _directoryResult.Text = "Ошибка поиска: " + ex.Message; }
        finally { _searchDirectoryButton.Enabled = true; }
    }

    private static int CountOccurrences(string text, string word)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(word, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            index += word.Length;
        }
        return count;
    }

    private void ChooseFile()
    {
        using var dialog = new OpenFileDialog { Title = "Выберите файл", Filter = "Все файлы (*.*)|*.*" };
        if (dialog.ShowDialog(this) == DialogResult.OK) _filePath.Text = dialog.FileName;
    }

    private void ChooseDirectory()
    {
        using var dialog = new FolderBrowserDialog { Description = "Выберите директорию для рекурсивного поиска" };
        if (dialog.ShowDialog(this) == DialogResult.OK) _directoryPath.Text = dialog.SelectedPath;
    }

    private static Button MakeButton(string text, EventHandler handler)
    {
        var button = new Button { Text = text, AutoSize = true, Height = 30, Margin = new Padding(4) };
        button.Click += handler;
        return button;
    }

    private void Ui(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try
        {
            if (InvokeRequired) BeginInvoke(action);
            else action();
        }
        catch (InvalidOperationException) { }
    }
}
