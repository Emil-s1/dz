using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ParallelProgramming_Module5
{
    public class MainForm : Form
    {
        private TextBox txtSourceText;
        private TextBox txtTextReport;
        private CheckBox chkSentences, chkChars, chkWords, chkQuestions, chkExclamations;
        private RadioButton rbScreen, rbFile;
        private Button btnAnalyze, btnStop;
        private CancellationTokenSource textCts;
        private TextBox txtSourceDir, txtDestinationDir, txtFileReport;
        private Button btnBrowseSource, btnBrowseDestination, btnMove, btnStopFiles;
        private CheckBox chkIncludeSubfolders;
        private CancellationTokenSource fileCts;

        public MainForm()
        {
            Text = "Модуль 5 — Параллельное программирование";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(900, 650);
            Size = new Size(1100, 780);
            Font = new Font("Segoe UI", 9F);
            BuildInterface();
        }

        private void BuildInterface()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            var tabText = new TabPage("Задания 1–3. Анализ текста");
            var tabFiles = new TabPage("Задания 4–5. Файлы-дубликаты");
            tabs.TabPages.Add(tabText);
            tabs.TabPages.Add(tabFiles);
            Controls.Add(tabs);

            BuildTextTab(tabText);
            BuildFilesTab(tabFiles);
        }

        private void BuildTextTab(TabPage page)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 95));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            page.Controls.Add(layout);

            layout.Controls.Add(new Label { Text = "Введите текст для анализа:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            txtSourceText = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, AcceptsReturn = true };
            layout.Controls.Add(txtSourceText, 0, 1);

            var options = new GroupBox { Text = "Какие данные включить в отчёт (задание 3)", Dock = DockStyle.Fill };
            layout.Controls.Add(options, 0, 2);
            var checks = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(8), WrapContents = true };
            options.Controls.Add(checks);
            chkSentences = AddCheck(checks, "Количество предложений", true);
            chkChars = AddCheck(checks, "Количество символов", true);
            chkWords = AddCheck(checks, "Количество слов", true);
            chkQuestions = AddCheck(checks, "Вопросительные предложения", true);
            chkExclamations = AddCheck(checks, "Восклицательные предложения", true);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            layout.Controls.Add(actions, 0, 3);
            btnAnalyze = new Button { Text = "Начать / повторить анализ", AutoSize = true, Height = 30 };
            btnAnalyze.Click += async (s, e) => await AnalyzeTextAsync();
            btnStop = new Button { Text = "Остановить", AutoSize = true, Height = 30, Enabled = false };
            btnStop.Click += (s, e) => { if (textCts != null) textCts.Cancel(); };
            actions.Controls.Add(btnAnalyze);
            actions.Controls.Add(btnStop);
            actions.Controls.Add(new Label { Text = "Результат:", AutoSize = true, Padding = new Padding(8, 7, 0, 0) });
            rbScreen = new RadioButton { Text = "Показать на экране", Checked = true, AutoSize = true, Padding = new Padding(8, 5, 0, 0) };
            rbFile = new RadioButton { Text = "Сохранить в файл", AutoSize = true, Padding = new Padding(8, 5, 0, 0) };
            actions.Controls.Add(rbScreen);
            actions.Controls.Add(rbFile);

            txtTextReport = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical };
            layout.Controls.Add(txtTextReport, 0, 4);
        }

        private CheckBox AddCheck(Control parent, string caption, bool isChecked)
        {
            var cb = new CheckBox { Text = caption, Checked = isChecked, AutoSize = true, Margin = new Padding(8, 6, 14, 4) };
            parent.Controls.Add(cb);
            return cb;
        }

        private async Task AnalyzeTextAsync()
        {
            if (textCts != null) textCts.Cancel();
            textCts = new CancellationTokenSource();
            var token = textCts.Token;
            string input = txtSourceText.Text;
            bool s = chkSentences.Checked, c = chkChars.Checked, w = chkWords.Checked;
            bool q = chkQuestions.Checked, ex = chkExclamations.Checked;
            btnAnalyze.Enabled = false;
            btnStop.Enabled = true;
            txtTextReport.Clear();
            try
            {
                string report = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    var sentences = Regex.Split(input.Trim(), @"(?<=[.!?])\s+")
                        .Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
                    int sentenceCount = sentences.Count;
                    int questionCount = sentences.Count(x => x.TrimEnd().EndsWith("?"));
                    int exclamationCount = sentences.Count(x => x.TrimEnd().EndsWith("!"));
                    int wordCount = Regex.Matches(input, @"[\p{L}\p{N}]+(?:[-'’][\p{L}\p{N}]+)*").Count;
                    token.ThrowIfCancellationRequested();

                    var lines = new List<string> { "ОТЧЁТ ПО АНАЛИЗУ ТЕКСТА", "Дата: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"), "" };
                    if (s) lines.Add("Количество предложений: " + sentenceCount);
                    if (c) lines.Add("Количество символов: " + input.Length);
                    if (w) lines.Add("Количество слов: " + wordCount);
                    if (q) lines.Add("Вопросительных предложений: " + questionCount);
                    if (ex) lines.Add("Восклицательных предложений: " + exclamationCount);
                    if (!s && !c && !w && !q && !ex) lines.Add("Поля отчёта не выбраны.");
                    return string.Join(Environment.NewLine, lines);
                }, token);

                token.ThrowIfCancellationRequested();
                if (rbFile.Checked)
                {
                    using (var dialog = new SaveFileDialog { Filter = "Текстовый файл (*.txt)|*.txt", FileName = "Отчёт_анализа.txt" })
                    {
                        if (dialog.ShowDialog(this) == DialogResult.OK)
                            await Task.Run(() => File.WriteAllText(dialog.FileName, report, Encoding.UTF8), token);
                        else
                            txtTextReport.Text = "Сохранение отменено пользователем.";
                    }
                }
                else txtTextReport.Text = report;
            }
            catch (OperationCanceledException) { txtTextReport.Text = "Анализ остановлен. Нажмите «Начать / повторить анализ», чтобы запустить его снова."; }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { btnAnalyze.Enabled = true; btnStop.Enabled = false; textCts.Dispose(); textCts = null; }
        }

        private void BuildFilesTab(TabPage page)
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(10) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            page.Controls.Add(layout);

            var sourceRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            sourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
            sourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            sourceRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            sourceRow.Controls.Add(new Label { Text = "Папка-источник:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            txtSourceDir = new TextBox { Dock = DockStyle.Fill };
            sourceRow.Controls.Add(txtSourceDir, 1, 0);
            btnBrowseSource = new Button { Text = "Обзор...", Dock = DockStyle.Fill };
            btnBrowseSource.Click += (s, e) => SelectFolder(txtSourceDir);
            sourceRow.Controls.Add(btnBrowseSource, 2, 0);
            layout.Controls.Add(sourceRow, 0, 0);

            var destinationRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            destinationRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
            destinationRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            destinationRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            destinationRow.Controls.Add(new Label { Text = "Папка-приёмник:", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
            txtDestinationDir = new TextBox { Dock = DockStyle.Fill };
            destinationRow.Controls.Add(txtDestinationDir, 1, 0);
            btnBrowseDestination = new Button { Text = "Обзор...", Dock = DockStyle.Fill };
            btnBrowseDestination.Click += (s, e) => SelectFolder(txtDestinationDir);
            destinationRow.Controls.Add(btnBrowseDestination, 2, 0);
            layout.Controls.Add(destinationRow, 0, 1);

            var note = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(4) };
            chkIncludeSubfolders = new CheckBox { Text = "Проверять также вложенные папки", Checked = true, AutoSize = true };
            note.Controls.Add(chkIncludeSubfolders);
            note.Controls.Add(new Label { Text = "Дубликаты определяются по SHA-256; перемещаются только уникальные файлы.", AutoSize = true, Padding = new Padding(12, 3, 0, 0) });
            layout.Controls.Add(note, 0, 2);

            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill };
            btnMove = new Button { Text = "Найти дубликаты и переместить оригиналы", AutoSize = true, Height = 30 };
            btnMove.Click += async (s, e) => await ProcessFilesAsync();
            btnStopFiles = new Button { Text = "Остановить", AutoSize = true, Height = 30, Enabled = false };
            btnStopFiles.Click += (s, e) => { if (fileCts != null) fileCts.Cancel(); };
            actions.Controls.Add(btnMove);
            actions.Controls.Add(btnStopFiles);
            layout.Controls.Add(actions, 0, 3);

            txtFileReport = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false };
            layout.Controls.Add(txtFileReport, 0, 4);
        }

        private void SelectFolder(TextBox target)
        {
            using (var dialog = new FolderBrowserDialog())
                if (dialog.ShowDialog(this) == DialogResult.OK) target.Text = dialog.SelectedPath;
        }

        private async Task ProcessFilesAsync()
        {
            string source = txtSourceDir.Text.Trim();
            string destination = txtDestinationDir.Text.Trim();
            if (!Directory.Exists(source)) { MessageBox.Show(this, "Укажите существующую папку-источник."); return; }
            if (string.IsNullOrWhiteSpace(destination)) { MessageBox.Show(this, "Укажите папку-приёмник."); return; }
            string srcFull = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string dstFull = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (dstFull.StartsWith(srcFull, StringComparison.OrdinalIgnoreCase) || srcFull.StartsWith(dstFull, StringComparison.OrdinalIgnoreCase))
            { MessageBox.Show(this, "Папки-источник и приёмник не должны совпадать или находиться одна внутри другой."); return; }

            if (fileCts != null) fileCts.Cancel();
            fileCts = new CancellationTokenSource();
            var token = fileCts.Token;
            bool recursive = chkIncludeSubfolders.Checked;
            btnMove.Enabled = false; btnStopFiles.Enabled = true; txtFileReport.Clear();

            try
            {
                var report = await Task.Run(() =>
                {
                    Directory.CreateDirectory(destination);
                    var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                    var paths = Directory.GetFiles(source, "*", searchOption);
                    var knownHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var log = new List<string>();
                    int moved = 0, duplicates = 0, errors = 0;
                    log.Add("ОТЧЁТ О ПОИСКЕ ДУБЛИКАТОВ И ПЕРЕМЕЩЕНИИ ФАЙЛОВ");
                    log.Add("Начало: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                    log.Add("Источник: " + source);
                    log.Add("Приёмник: " + destination);
                    log.Add("");

                    foreach (string path in paths)
                    {
                        token.ThrowIfCancellationRequested();
                        try
                        {
                            string hash;
                            using (var sha = SHA256.Create())
                            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                                hash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");

                            if (!knownHashes.Add(hash))
                            {
                                duplicates++;
                                log.Add("[ДУБЛИКАТ — оставлен в источнике] " + path);
                                continue;
                            }

                            string relative = recursive ? path.Substring(Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar).Length).TrimStart(Path.DirectorySeparatorChar) : Path.GetFileName(path);
                            string target = Path.Combine(destination, relative);
                            string targetDir = Path.GetDirectoryName(target);
                            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                            if (File.Exists(target))
                            {
                                string name = Path.GetFileNameWithoutExtension(target);
                                string ext = Path.GetExtension(target);
                                string dir = Path.GetDirectoryName(target);
                                int n = 1;
                                do { target = Path.Combine(dir, name + "_" + n++ + ext); } while (File.Exists(target));
                            }
                            File.Move(path, target);
                            moved++;
                            log.Add("[ПЕРЕМЕЩЁН ОРИГИНАЛ] " + path + " -> " + target);
                        }
                        catch (OperationCanceledException) { throw; }
                        catch (Exception err) { errors++; log.Add("[ОШИБКА] " + path + " — " + err.Message); }
                    }
                    log.Add("");
                    log.Add("ИТОГО:");
                    log.Add("Перемещено оригинальных файлов: " + moved);
                    log.Add("Найдено дубликатов (оставлены в источнике): " + duplicates);
                    log.Add("Ошибок: " + errors);
                    log.Add("Завершение: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
                    return string.Join(Environment.NewLine, log);
                }, token);

                txtFileReport.Text = report;
                using (var dialog = new SaveFileDialog { Filter = "Текстовый файл (*.txt)|*.txt", FileName = "Отчёт_перемещения.txt" })
                {
                    if (dialog.ShowDialog(this) == DialogResult.OK)
                        await Task.Run(() => File.WriteAllText(dialog.FileName, report, Encoding.UTF8), token);
                }
            }
            catch (OperationCanceledException) { txtFileReport.AppendText(Environment.NewLine + "Операция остановлена пользователем. Частично выполненные перемещения отменить автоматически невозможно."); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { btnMove.Enabled = true; btnStopFiles.Enabled = false; fileCts.Dispose(); fileCts = null; }
        }
    }
}
