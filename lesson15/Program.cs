using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace ConsoleApp5
{
    class Program
    {
        static void Main(string[] args)
        {
            string filePath = "numbers.txt";

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Файл {filePath} не найден.");
                return;
            }

            List<int> numbers = File.ReadAllText(filePath)
                                     .Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                     .Select(int.Parse)
                                     .ToList();

            Console.WriteLine($"Исходный список: {string.Join(", ", numbers)}\n");

            var uniqueCount = numbers.AsParallel().Distinct().Count();
            Console.WriteLine($"Задание 1. Количество уникальных значений: {uniqueCount}");

            int[] arr = numbers.ToArray();

            var maxIncreasingLength = Enumerable.Range(0, arr.Length)
                .AsParallel()
                .Select(startIndex =>
                {
                    int currentLength = 1;
                    int lastValue = arr[startIndex];

                    for (int i = startIndex + 1; i < arr.Length; i++)
                    {
                        if (arr[i] > lastValue)
                        {
                            currentLength++;
                            lastValue = arr[i];
                        }
                        else
                        {
                            break;
                        }
                    }
                    return currentLength;
                })
                .Max();

            Console.WriteLine($"Задание 2. Максимальная длина возрастающей последовательности: {maxIncreasingLength}");

            int maxPositiveSequence = 0;
            int currentPositiveSequence = 0;

            var isPositiveArray = arr.AsParallel().Select(x => x > 0).ToArray();

            foreach (bool isPositive in isPositiveArray)
            {
                if (isPositive)
                {
                    currentPositiveSequence++;
                    if (currentPositiveSequence > maxPositiveSequence)
                    {
                        maxPositiveSequence = currentPositiveSequence;
                    }
                }
                else
                {
                    currentPositiveSequence = 0; 
                }
            }

            Console.WriteLine($"Задание 3. Максимальная длина последовательности положительных чисел: {maxPositiveSequence}");

            Console.ReadLine();
        }
    }
}