using System.Globalization;
using System.Text;

namespace ChildProcess;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Не указан режим работы дочернего процесса.");
                return 1;
            }

            return args[0] switch
            {
                "--demo" => RunDemo(),
                "--calc" => RunCalculation(args),
                "--count" => RunWordCount(args),
                _ => UnknownMode(args[0])
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    // Демонстрационный процесс для заданий 1 и 2.
    // Возвращает код 42 после небольшой задержки, чтобы родитель успел его остановить.
    private static int RunDemo()
    {
        Console.WriteLine("Дочерний процесс выполняет демонстрационную работу...");
        for (int i = 1; i <= 10; i++)
        {
            Thread.Sleep(1000);
            Console.WriteLine($"Выполнен шаг {i} из 10.");
        }
        Console.WriteLine("Демонстрационная работа завершена.");
        return 42;
    }

    private static int RunCalculation(string[] args)
    {
        if (args.Length != 4)
        {
            Console.Error.WriteLine("Формат: --calc <число1> <операция> <число2>");
            return 2;
        }

        if (!double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double a) ||
            !double.TryParse(args[3], NumberStyles.Float, CultureInfo.InvariantCulture, out double b))
        {
            Console.Error.WriteLine("Аргументы должны быть числами.");
            return 2;
        }

        string op = args[2];
        double result = op switch
        {
            "+" => a + b,
            "-" => a - b,
            "*" => a * b,
            "/" when b != 0 => a / b,
            "/" => throw new DivideByZeroException("Деление на ноль невозможно."),
            _ => throw new ArgumentException("Неизвестная операция. Используйте +, -, * или /.")
        };

        Console.WriteLine($"Получены аргументы: {a} {op} {b}");
        Console.WriteLine($"Результат: {result}");
        return 0;
    }

    private static int RunWordCount(string[] args)
    {
        if (args.Length != 3)
        {
            Console.Error.WriteLine("Формат: --count <путь_к_файлу> <слово>");
            return 2;
        }

        string path = args[1];
        string word = args[2];
        if (!File.Exists(path))
            throw new FileNotFoundException("Файл не найден.", path);

        string content = File.ReadAllText(path, Encoding.UTF8);
        int count = 0;
        int position = 0;

        // Считаются вхождения подстроки, в том числе внутри других слов.
        while ((position = content.IndexOf(word, position, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            position += word.Length;
        }

        Console.WriteLine($"Файл: {path}");
        Console.WriteLine($"Искомое слово: {word}");
        Console.WriteLine($"Количество вхождений: {count}");
        return 0;
    }

    private static int UnknownMode(string mode)
    {
        Console.Error.WriteLine($"Неизвестный режим: {mode}");
        return 2;
    }
}
