using System.Collections.Concurrent;
using System.Text;

namespace Module4Synchronization;

public sealed class MainForm : Form
{
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill };
    private readonly Random random = new();

    public MainForm()
    {
        Text = "Модуль 4 — Синхронизация: события и критические секции";
        Width = 1100;
        Height = 760;
        MinimumSize = new Size(900, 620);
        StartPosition = FormStartPosition.CenterScreen;
        Controls.Add(tabs);
        BuildTask1();
        BuildBusTab(2);
        BuildBusTab(3);
        BuildBusTab(4);
    }

    private static Label MakeLabel(string text) => new() { Text = text, AutoSize = true, Margin = new Padding(6) };
    private static NumericUpDown MakeNumber(int value, int min, int max, int width = 90) =>
        new() { Value = value, Minimum = min, Maximum = max, Width = width, Margin = new Padding(5) };

    private void BuildTask1()
    {
        var page = new TabPage("1. События и файлы");
        var panel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 54, Padding = new Padding(8) };
        var count = MakeNumber(20, 1, 10000);
        var start = new Button { Text = "Запустить потоки", AutoSize = true, Margin = new Padding(8) };
        panel.Controls.Add(MakeLabel("Количество пар:"));
        panel.Controls.Add(count);
        panel.Controls.Add(start);
        var output = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 10) };
        page.Controls.Add(output);
        page.Controls.Add(panel);
        start.Click += async (_, _) =>
        {
            start.Enabled = false;
            output.Clear();
            try
            {
                int n = (int)count.Value;
                string dir = Path.Combine(AppContext.BaseDirectory, "Task1Results");
                Directory.CreateDirectory(dir);
                string pairsPath = Path.Combine(dir, "pairs.txt");
                string sumsPath = Path.Combine(dir, "sums.txt");
                string productsPath = Path.Combine(dir, "products.txt");
                using var generated = new ManualResetEventSlim(false);
                var pairs = new List<(int A, int B)>();
                var generator = Task.Run(() =>
                {
                    var rng = new Random();
                    for (int i = 0; i < n; i++)
                        pairs.Add((rng.Next(1, 101), rng.Next(1, 101)));
                    File.WriteAllLines(pairsPath, pairs.Select((p, i) => $"{i + 1}: {p.A}; {p.B}"));
                    generated.Set();
                });
                var sumWorker = Task.Run(() =>
                {
                    generated.Wait();
                    File.WriteAllLines(sumsPath, pairs.Select((p, i) => $"{i + 1}: {p.A} + {p.B} = {p.A + p.B}"));
                });
                var productWorker = Task.Run(() =>
                {
                    generated.Wait();
                    File.WriteAllLines(productsPath, pairs.Select((p, i) => $"{i + 1}: {p.A} * {p.B} = {p.A * p.B}"));
                });
                await Task.WhenAll(generator, sumWorker, productWorker);
                output.AppendText($"Генерация завершена. Создано пар: {pairs.Count}{Environment.NewLine}");
                output.AppendText($"Файл пар: {pairsPath}{Environment.NewLine}");
                output.AppendText($"Суммы: {sumsPath}{Environment.NewLine}");
                output.AppendText($"Произведения: {productsPath}{Environment.NewLine}{Environment.NewLine}");
                output.AppendText("Первые результаты:" + Environment.NewLine);
                output.AppendText(string.Join(Environment.NewLine, pairs.Take(10).Select((p, i) => $"{i + 1}. {p.A}, {p.B} → сумма {p.A + p.B}, произведение {p.A * p.B}")));
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка"); }
            finally { start.Enabled = true; }
        };
        tabs.TabPages.Add(page);
    }

    private void BuildBusTab(int task)
    {
        string caption = task switch
        {
            2 => "2. Один маршрут",
            3 => "3. Разные маршруты",
            _ => "4. Обычная остановка"
        };
        var page = new TabPage(caption);
        var top = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 92, Padding = new Padding(8), WrapContents = true };
        var busCount = MakeNumber(8, 1, 50);
        var capacity = MakeNumber(30, 1, 200);
        var interval = MakeNumber(500, 100, 3000);
        var routesText = new TextBox { Text = "175, 12, 42", Width = 150, Margin = new Padding(5) };
        var start = new Button { Text = "Начать моделирование", AutoSize = true, Margin = new Padding(8) };
        var stop = new Button { Text = "Остановить", AutoSize = true, Enabled = false, Margin = new Padding(8) };
        top.Controls.Add(MakeLabel("Автобусов:"));
        top.Controls.Add(busCount);
        top.Controls.Add(MakeLabel("Вместимость:"));
        top.Controls.Add(capacity);
        top.Controls.Add(MakeLabel("Интервал (мс):"));
        top.Controls.Add(interval);
        if (task >= 3)
        {
            top.Controls.Add(MakeLabel("Номера маршрутов:"));
            top.Controls.Add(routesText);
        }
        top.Controls.Add(start);
        top.Controls.Add(stop);

        var status = new Label { Dock = DockStyle.Top, Height = 34, Padding = new Padding(10), Font = new Font("Segoe UI", 10, FontStyle.Bold), Text = "Ожидание запуска" };
        var log = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9) };
        page.Controls.Add(log);
        page.Controls.Add(status);
        page.Controls.Add(top);

        CancellationTokenSource? cts = null;
        start.Click += async (_, _) =>
        {
            string[] routes = task == 2 ? new[] { "175" } : routesText.Text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToArray();
            if (routes.Length == 0) { MessageBox.Show("Введите хотя бы один номер маршрута."); return; }
            start.Enabled = false;
            stop.Enabled = true;
            log.Clear();
            cts = new CancellationTokenSource();
            int buses = (int)busCount.Value;
            int cap = (int)capacity.Value;
            int delay = (int)interval.Value;
            int passengerCount = 0;
            var waiting = routes.ToDictionary(r => r, _ => 0);
            var onboard = routes.ToDictionary(r => r, _ => 0);
            var sync = new object();
            var rnd = new Random();
            int arrivals = 0, departed = 0;

            void Write(string line)
            {
                if (log.IsDisposed) return;
                if (log.InvokeRequired) log.BeginInvoke(new Action(() => { log.AppendText(line + Environment.NewLine); status.Text = $"Ожидают: {waiting.Values.Sum()} | В автобусах на борту: {onboard.Values.Sum()} | Пришло людей: {arrivals}"; }));
                else { log.AppendText(line + Environment.NewLine); status.Text = $"Ожидают: {waiting.Values.Sum()} | В автобусах на борту: {onboard.Values.Sum()} | Пришло людей: {arrivals}"; }
            }

            // Генератор пассажиров и автобусы работают параллельно; общий доступ к очередям защищён критической секцией lock.
            var arrivalTask = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(Math.Max(150, delay / 2), cts.Token);
                    lock (sync)
                    {
                        int amount = rnd.Next(1, 13);
                        string route = routes[rnd.Next(routes.Length)];
                        waiting[route] += amount;
                        arrivals += amount;
                        Write($"Пришло {amount} пассажиров на маршрут {route}. Всего ждут на этом маршруте: {waiting[route]}.");
                    }
                }
            }, cts.Token);

            var busTasks = Enumerable.Range(0, buses).Select(index => Task.Run(async () =>
            {
                string route = routes[index % routes.Length];
                for (int trip = 0; trip < Math.Max(1, 1000); trip++)
                {
                    await Task.Delay(delay, cts.Token);
                    lock (sync)
                    {
                        if (task == 4)
                        {
                            int leaving = onboard[route] > 0 ? rnd.Next(0, onboard[route] + 1) : 0;
                            onboard[route] -= leaving;
                            if (leaving > 0) Write($"Автобус {route} приехал с {leaving} пассажирами, они вышли.");
                        }
                        int free = cap - onboard[route];
                        int boarded = Math.Min(free, waiting[route]);
                        waiting[route] -= boarded;
                        onboard[route] += boarded;
                        departed++;
                        Write($"Автобус №{route} ({index + 1}) прибыл и взял {boarded} пассажиров. В салоне: {onboard[route]}/{cap}. Осталось ждать: {waiting[route]}.");
                    }
                }
            }, cts.Token)).ToArray();

            try
            {
                await Task.WhenAll(busTasks.Append(arrivalTask));
            }
            catch (OperationCanceledException) { }
            finally
            {
                lock (sync)
                {
                    Write($"Моделирование остановлено. Автобусов прибыло: {departed}. Всего пришло пассажиров: {arrivals}. Осталось ждать: {waiting.Values.Sum()}.");
                }
                start.Enabled = true;
                stop.Enabled = false;
                cts.Dispose();
                cts = null;
            }
        };
        stop.Click += (_, _) => cts?.Cancel();
        tabs.TabPages.Add(page);
    }
}
