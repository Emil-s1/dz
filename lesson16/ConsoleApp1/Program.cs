using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AllTasksInOneFile
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== ЗАДАНИЕ 1: Геометрические фигуры ===");
            Console.WriteLine($"Площадь квадрата (сторона 5): {Task1_Geometry.SquareArea(5)}");
            Console.WriteLine($"Площадь прямоугольника (4x6): {Task1_Geometry.RectangleArea(4, 6)}");
            Console.WriteLine($"Площадь треугольника (3, 4, 5): {Task1_Geometry.TriangleArea(3, 4, 5)}");
            Console.WriteLine();

            Console.WriteLine("=== ЗАДАНИЕ 2: Работа с текстом ===");
            string testString = "А роза упала на лапу Азора. Привет! Как дела?";
            Console.WriteLine($"Исходная строка: {testString}");
            Console.WriteLine($"Является ли 'А роза упала на лапу Азора' палиндромом: {Task2_TextProcessor.IsPalindrome("А роза упала на лапу Азора")}");
            Console.WriteLine($"Количество предложений: {Task2_TextProcessor.CountSentences(testString)}");
            Console.WriteLine($"Переворот строки 'Привет': {Task2_TextProcessor.ReverseString("Привет")}");
            Console.WriteLine();

            Console.WriteLine("=== ЗАДАНИЕ 3: Проверка контактных данных ===");
            Console.WriteLine($"ФИО 'Иванов Иван' корректно: {Task3_UserValidator.ValidateFullName("Иванов Иван")}");
            Console.WriteLine($"ФИО 'Ivan123' корректно: {Task3_UserValidator.ValidateFullName("Ivan123")}");
            Console.WriteLine($"Возраст '25' корректно: {Task3_UserValidator.ValidateAge("25")}");
            Console.WriteLine($"Возраст '25 лет' корректно: {Task3_UserValidator.ValidateAge("25 лет")}");
            Console.WriteLine($"Телефон '+7 (999) 123-45-67' корректно: {Task3_UserValidator.ValidatePhone("+7 (999) 123-45-67")}");
            Console.WriteLine($"Email 'test@mail.ru' корректно: {Task3_UserValidator.ValidateEmail("test@mail.ru")}");
            Console.WriteLine();

            Console.WriteLine("=== ЗАДАНИЯ 4 и 5: Файловая система ===");
            RunFileSystemTasks();

            Console.ReadLine();
        }

        static void RunFileSystemTasks()
        {
            string testDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "TestFolder_HomeWork");

            if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
            Directory.CreateDirectory(testDir);

            string file1 = Path.Combine(testDir, "file1.txt");
            string file2 = Path.Combine(testDir, "file2.txt");
            string file3 = Path.Combine(testDir, "image.jpg");

            File.WriteAllText(file1, "Это файл 1. Здесь есть слово Привет.");
            File.WriteAllText(file2, "Это файл 2. Здесь нет нужных слов.");
            File.WriteAllText(file3, "Это картинка. Слово Привет тоже есть.");

            Console.WriteLine($"Тестовая папка создана по пути: {testDir}");

            Console.WriteLine("\n-- Задание 4 --");

            string copiedFile = Path.Combine(testDir, "file1_copy.txt");
            Task4_FileSystem.CopyFile(file1, copiedFile);
            Console.WriteLine($"Файл скопирован: {Path.GetFileName(copiedFile)}");

            string destDir = Path.Combine(testDir, "CopiedDir");
            Task4_FileSystem.CopyDirectory(testDir, destDir);
            Console.WriteLine($"Директория скопирована в: {Path.GetFileName(destDir)}");

            Console.WriteLine("Удаление файлов по маске '*.txt' в скопированной папке...");
            Task4_FileSystem.DeleteFilesByMask(destDir, "*.txt");

            string movedFile = Path.Combine(testDir, "file2_moved.txt");
            Task4_FileSystem.MoveFile(file2, movedFile);
            Console.WriteLine($"Файл перенесен: {Path.GetFileName(movedFile)}");

            Task4_FileSystem.DeleteFile(movedFile);
            Console.WriteLine("Файл удален по имени.");

            Console.WriteLine("\n-- Задание 5 --");

            Console.WriteLine("Поиск слова 'Привет' в file1.txt:");
            Task5_Search.FindWordInFile(file1, "Привет");

            Console.WriteLine("\nПоиск слова 'Привет' во всей папке (создание отчета):");
            Task5_Search.FindWordInDirectory(testDir, "Привет");

            Console.WriteLine($"\n[Все файлы и отчеты сохранены в папке: {testDir}]");
        }
        public static class Task1_Geometry
        {
            public static double SquareArea(double side) => side < 0 ? throw new ArgumentException("Сторона не может быть отрицательной") : side * side;
            public static double RectangleArea(double w, double h) => (w < 0 || h < 0) ? throw new ArgumentException("Стороны не могут быть отрицательными") : w * h;
            public static double TriangleArea(double a, double b, double c)
            {
                if (a <= 0 || b <= 0 || c <= 0) return 0;
                double p = (a + b + c) / 2;
                return Math.Sqrt(p * (p - a) * (p - b) * (p - c));
            }
        }
        public static class Task2_TextProcessor
        {
            public static bool IsPalindrome(string text)
            {
                if (string.IsNullOrEmpty(text)) return false;
                string cleanText = new string(text.Where(char.IsLetterOrDigit).ToArray()).ToLower();
                return cleanText == new string(cleanText.Reverse().ToArray());
            }
            public static int CountSentences(string text) => string.IsNullOrEmpty(text) ? 0 : text.Count(c => c == '.' || c == '!' || c == '?');
            public static string ReverseString(string text) => string.IsNullOrEmpty(text) ? text : new string(text.Reverse().ToArray());
        }
        public static class Task3_UserValidator
        {
            public static bool ValidateFullName(string fullName) => !string.IsNullOrWhiteSpace(fullName) && Regex.IsMatch(fullName, @"^[a-zA-Zа-яА-Я\s\-]+$");
            public static bool ValidateAge(string age) => !string.IsNullOrWhiteSpace(age) && age.All(char.IsDigit);
            public static bool ValidatePhone(string phone) => Regex.IsMatch(phone, @"^[\d\+\-\(\)\s]{10,20}$");
            public static bool ValidateEmail(string email) => Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        public static class Task4_FileSystem
        {
            public static void CopyFile(string source, string dest) => File.Copy(source, dest, true);

            public static void CopyDirectory(string sourceDir, string destDir)
            {
                string fullSource = Path.GetFullPath(sourceDir).TrimEnd('\\') + "\\";
                string fullDest = Path.GetFullPath(destDir).TrimEnd('\\') + "\\";

                if (fullDest.StartsWith(fullSource))
                {
                    Console.WriteLine($"[Предупреждение] Пропущено копирование папки в саму себя.");
                    return;
                }

                Directory.CreateDirectory(destDir);

                foreach (var file in Directory.GetFiles(sourceDir))
                    File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), true);

                foreach (var dir in Directory.GetDirectories(sourceDir))
                {
                    string dirName = Path.GetFileName(dir);
                    if (dirName == "bin" || dirName == "obj") continue;
                    CopyDirectory(dir, Path.Combine(destDir, dirName));
                }
            }

            public static void DeleteFile(string path)
            {
                if (File.Exists(path)) File.Delete(path);
            }

            public static void DeleteFiles(string[] filePaths)
            {
                foreach (var path in filePaths)
                    if (File.Exists(path)) File.Delete(path);
            }

            public static void DeleteFilesByMask(string directory, string mask)
            {
                if (!Directory.Exists(directory)) return;
                foreach (var file in Directory.GetFiles(directory, mask))
                    File.Delete(file);
            }

            public static void MoveFile(string source, string dest) => File.Move(source, dest);
        }
        public static class Task5_Search
        {
            public static void FindWordInFile(string filePath, string word)
            {
                if (!File.Exists(filePath)) return;

                string[] lines = File.ReadAllLines(filePath);
                StringBuilder report = new StringBuilder();
                report.AppendLine($"--- Отчет по поиску слова '{word}' в файле {Path.GetFileName(filePath)} ---");

                bool found = false;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        report.AppendLine($"Строка {i + 1}: {lines[i]}");
                        found = true;
                    }
                }

                if (!found) report.AppendLine("Слово не найдено.");

                string reportPath = Path.Combine(Path.GetDirectoryName(filePath), "Report_File.txt");
                File.WriteAllText(reportPath, report.ToString());
                Console.WriteLine($"Отчет создан: {reportPath}");
            }

            public static void FindWordInDirectory(string dirPath, string word)
            {
                if (!Directory.Exists(dirPath)) return;

                StringBuilder report = new StringBuilder();
                report.AppendLine($"--- Отчет по поиску слова '{word}' в папке {Path.GetFileName(dirPath)} ---");
                bool found = false;

                foreach (var file in Directory.GetFiles(dirPath, "*.txt"))
                {
                    string[] lines = File.ReadAllLines(file);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            report.AppendLine($"[{Path.GetFileName(file)}] Строка {i + 1}: {lines[i]}");
                            found = true;
                        }
                    }
                }

                if (!found) report.AppendLine("Слово не найдено ни в одном файле.");

                string reportPath = Path.Combine(dirPath, "Report_Folder.txt");
                File.WriteAllText(reportPath, report.ToString());
                Console.WriteLine($"Отчет создан: {reportPath}");
            }
        }
    }
}