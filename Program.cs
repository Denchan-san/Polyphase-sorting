using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Hosting;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Polyphase_sort
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Options options = new Options { InputFileName = "input", OutputFileName = "output.txt", TapesCount = 5, PreSort = false, GenerateFiles = false };

            SetAllOptions(options);

            if (options.GenerateFiles == true)
            {
                Console.WriteLine("Enter size of generated file in megabytes:");
                int mb = Int32.Parse(Console.ReadLine()),
                buff = mb * 1024 * 1024;
                GenerateRandomNumberFile($"{mb}mb_file.txt", buff);
                options.InputFileName = $"{mb}mb_file.txt";
            }
            else
            {
                Console.WriteLine("Enter filename witout .txt");
                options.InputFileName = Console.ReadLine() + ".txt";
            }

            Stopwatch timer = Stopwatch.StartNew();
            if (options.PreSort == true)
            {
                PreSort(options.InputFileName);
                Console.WriteLine("PreSort ended;");
            }

            Sort(options);

            timer.Stop();

            Console.WriteLine(timer.ElapsedMilliseconds + "ms");

            for (int i = 0; i < options.TapesCount; i++)
            {
                File.Delete($"temp_{i}");

            }
        }
        public static void SetAllOptions(Options options)
        {
            Console.WriteLine("Enter count of tapes:");
            options.TapesCount = Int32.Parse(Console.ReadLine());

            Console.WriteLine("Generate files : true/false");
            options.GenerateFiles = bool.Parse(Console.ReadLine());

            Console.WriteLine("Use presort : true/false");
            options.PreSort = bool.Parse(Console.ReadLine());
        }

        public static void GenerateRandomNumberFile(string filePath, long fileSizeInMegaBytes)
        {
            Random random = new Random();
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            using (StreamWriter writer = new StreamWriter(fileStream))
            {
                long currentSize = 0;
                while (currentSize < fileSizeInMegaBytes)
                {
                    int randomNumber = random.Next(1, 1000001); 
                    string numberString = randomNumber.ToString();
                    writer.WriteLine(numberString);
                    currentSize += numberString.Length + Environment.NewLine.Length;
                }
            }
        }
        static void Sort(Options options) //основна ф-ція
        {
            Tape[] tapes = new Tape[options.TapesCount];

            DistributeTapes(options, tapes);

            CountSequences(tapes);

            int[] fibonacci = GenerateFibonacciSequence(options.TapesCount - 1);

            GetValidFibonacci(tapes, fibonacci);

            foreach (var item in tapes) Console.WriteLine($"Tape[{item.Index}]  BatchTotal = {item.BatchTotal}, BatchReal = {item.BatchReal}, BatchEmpty = {item.BatchEmpty}");
            Console.WriteLine("===");

            MergeFiles(tapes, fibonacci);
        }

        static void MergeFiles(Tape[] tapes, int[] fibonacci)
        {
            int tapesCount = tapes.Count();
            int indexOfEmptyTape = tapesCount - 1;
            while (SumOfSeries(tapes) != 1)
            {
                int minSeries = minOfTotal(tapes);
                int[] actualNumbers = new int[tapesCount - 1];

                //read first numbers

                for (int i = 0, index = 0; i < tapesCount; i++)
                {
                    if (i == indexOfEmptyTape) continue;
                    tapes[index].Reader = new StreamReader($"temp_{i}");
                    if (tapes[i].BatchReal > 0)
                    {
                        string line = tapes[index].Reader.ReadLine();
                        actualNumbers[index] = line != null ? int.Parse(line) : int.MaxValue;
                    }
                    else
                    {
                        actualNumbers[index] = int.MaxValue;
                    }
                    index++;
                }

                tapes[indexOfEmptyTape].Writer = new StreamWriter($"temp_{indexOfEmptyTape}");

                //pick the lowest, bring it to empty file and add new number from file with index of number that were added to previousNumbers,
                //than read another number from file[indexOfLower]
                int[] last = new int[tapesCount - 1];
                for (int i = 0; i < minSeries; i++)
                {
                    while (!actualNumbers.All(x => x == int.MaxValue))
                    {
                        int minIndex = getIndexOfLowest(actualNumbers);
                        tapes[indexOfEmptyTape].Writer.WriteLine(actualNumbers[minIndex]);

                        string line = tapes[minIndex].Reader.ReadLine();
                        int next = line != null ? int.Parse(line) : int.MinValue;
                        if (next < actualNumbers[minIndex])
                        {
                            actualNumbers[minIndex] = int.MaxValue;
                            last[minIndex] = line != null ? next : int.MaxValue;
                        }
                        else
                        {
                            actualNumbers[minIndex] = next;
                        }
                    }
                    for (int j = 0; j < tapesCount - 1; j++)
                    {
                        actualNumbers[j] = last[j] == 0 ? int.MaxValue : last[j];
                    }
                }

                tapes[indexOfEmptyTape].Writer.Close();

                for (int x = 0, index = 0; x < tapesCount; x++)
                {
                    if (x == indexOfEmptyTape) continue;
                    StreamWriter temp = new StreamWriter($"copy_temp_{x}");

                    if (actualNumbers[index] == int.MaxValue)
                    {
                        temp.Close();
                        index++;
                        continue;
                    }
                    temp.WriteLine(actualNumbers[index]);

                    while (!tapes[index].Reader.EndOfStream)
                    {
                        temp.WriteLine(tapes[index].Reader.ReadLine());
                    }
                    temp.Close();
                    index++;
                }

                for (int i = 0; i < tapesCount - 1; i++)
                {
                    tapes[i].Reader.Close();
                }

                for (int i = 0; i < tapesCount; i++)
                {
                    if (i == indexOfEmptyTape) continue;
                    File.Delete($"temp_{i}");
                    File.Move($"copy_temp_{i}", $"temp_{i}");
                }



                bool emptySeriesMerge = true;
                for (int i = 0; i < tapes.Count(); i++)
                {
                    if (tapes[i].BatchTotal == 0) continue;
                    if (tapes[i].BatchReal >= minSeries)
                    {
                        emptySeriesMerge = false;
                        break;
                    }
                }


                for (int i = 0; i < tapesCount; i++)
                {
                    if (tapes[i].BatchTotal == 0)
                    {
                        tapes[i].BatchTotal += minSeries;
                        if (!emptySeriesMerge)
                        {
                            tapes[i].BatchReal += minSeries;
                        }
                        else
                        {
                            tapes[i].BatchReal += EmptyMergeCount(tapes, minSeries);
                            tapes[i].BatchReal = tapes[i].BatchTotal - tapes[i].BatchEmpty;
                        }
                        break;
                    }
                }

                for (int i = 0; i < tapesCount; i++)
                {
                    if (indexOfEmptyTape != i)
                    {
                        tapes[i].BatchTotal -= minSeries;
                        if (tapes[i].BatchReal < minSeries)
                        {
                            tapes[i].BatchEmpty -= minSeries - tapes[i].BatchReal;
                            tapes[i].BatchReal = 0;
                        }
                        else
                        {
                            tapes[i].BatchReal -= minSeries;
                        }
                    }
                }


                for (int i = 0; i < tapesCount; i++)
                {
                    if (tapes[i].BatchTotal == 0)
                    {
                        indexOfEmptyTape = i;
                        break;
                    }
                }

                for (int i = 0; i < tapesCount; i++)
                {
                    Console.WriteLine($"Tape[{i}]  BatchTotal = {tapes[i].BatchTotal}, BatchReal = {tapes[i].BatchReal}, BatchEmpty = {tapes[i].BatchEmpty}");
                }
                Console.WriteLine("===");
            }
            int outputIndex = -1;
            for (int i = 0; i < tapesCount; i++)
            {
                if (tapes[i].BatchTotal == 1)
                {
                    outputIndex = i;
                    break;
                }
            }

            Console.WriteLine("+++");
            string outputFile = "output.txt";
            if (File.Exists(outputFile))
                File.Delete(outputFile);
            File.Move($"temp_{outputIndex}", outputFile);
            for (int i = 0; i < tapesCount; i++)
            {
                File.Delete($"temp_{i}");
            }
        }

        static int EmptyMergeCount(Tape[] tapes, int minSeries)
        {
            int count = 0;

            foreach (Tape tape in tapes)
            {
                if (tape.BatchReal > count && !tape.IsEmpty) count = tape.BatchReal;
            }

            return minSeries - count;
        }

        private static int minOfTotal(Tape[] tapes)
        {
            int lowestNonZero = int.MaxValue;

            foreach (Tape tape in tapes)
            {
                if (tape.BatchTotal > 0 && tape.BatchTotal < lowestNonZero)
                {
                    lowestNonZero = tape.BatchTotal;
                }
            }

            return lowestNonZero;
        }

        static int getIndexOfLowest(int[] actualNumbers)
        {
            if (actualNumbers == null || actualNumbers.Length == 0)
            {
                throw new ArgumentException("Масив пустий або не існує елементів.");
            }

            int lowestValue = actualNumbers[0];
            int lowestIndex = 0;

            for (int i = 1; i < actualNumbers.Length; i++)
            {
                if (actualNumbers[i] < lowestValue)
                {
                    lowestValue = actualNumbers[i];
                    lowestIndex = i;
                }
            }

            return lowestIndex;
        }

        private static void GetValidFibonacci(Tape[] tapes, int[] fibonacci)
        {
            int[] numbers = new int[fibonacci.Length];

            for (int i = 0; i < numbers.Count(); i++)
            {
                numbers[i] = tapes[i].BatchReal; 
            }

            while (ContinueFibonacci(numbers, fibonacci))
            {
                UpdateFibonacci(fibonacci);
            }

            for (int i = 0; i < numbers.Length; i++)
            {
                for (int j = 0; j < numbers.Length; j++)
                {
                    if (tapes[j].BatchReal == numbers[i] && tapes[j].BatchTotal == 0)
                    {
                        tapes[j].BatchTotal = fibonacci[i];
                        tapes[j].BatchEmpty = tapes[j].BatchTotal - tapes[j].BatchReal;
                        break;
                    }
                }
            }
        }

        private static bool ContinueFibonacci(int[] realSeries, int[] fibonacci)
        {
            Array.Sort(realSeries);

            for (int i = 0; i < realSeries.Length; i++)
            {
                if (realSeries[i] > fibonacci[i])
                {
                    return true;
                }
            }

            return false;

        }

        public static void UpdateFibonacci(int[] fibonacci)
        {
            int[] nextFibonacci = new int[fibonacci.Length];
            nextFibonacci[0] = fibonacci[fibonacci.Length - 1];

            for (int i = 1; i < fibonacci.Length; i++)
            {
                nextFibonacci[i] = fibonacci[fibonacci.Length - 1] + fibonacci[i - 1];
            }
            for (int i = 0; i < fibonacci.Length; i++)
            {
                fibonacci[i] = nextFibonacci[i];
            }
        }

        static void DistributeTapes(Options options, Tape[] tapes)
        {
            StreamReader streamReader = new StreamReader(options.InputFileName);
            for (int i = 0; i < options.TapesCount; i++)
            {
                tapes[i] = new Tape(i);
            }

            int fileLength = File.ReadLines(options.InputFileName).Count();
            int chunkLength = Convert.ToInt32(Math.Ceiling(fileLength / Convert.ToDouble(options.TapesCount - 1)));

            for (int i = 0; i < options.TapesCount - 1; i++)
            {
                for (int j = 0; j < chunkLength && !streamReader.EndOfStream; j++)
                {
                    tapes[i].Writer.WriteLine(streamReader.ReadLine());
                }
            }

            for (int i = 0; i < options.TapesCount; i++)
            {
                tapes[i].Writer.Close();
            }

            streamReader.Close();
        }

        static int[] GenerateFibonacciSequence(int count)
        {
            int[] fibonacciNums = new int[count];

            fibonacciNums[fibonacciNums.Length - 1] = 1;

            return fibonacciNums;
        }

        static void CountSequences(Tape[] tapes)
        {
            for (int i = 0; i < tapes.Length; i++)
            {
                tapes[i].BatchReal = CountBatchesInTape(tapes[i]);

            }
        }

        static int CountBatchesInTape(Tape tape)
        {
            StreamReader reader = new StreamReader($"temp_{tape.Index}");
            int batchCount = 0;
            int previousNumber = int.MinValue;
            string line;
            bool isEmpty = true;
            while ((line = reader.ReadLine()) != null)
            {
                isEmpty = false;
                if (int.TryParse(line, out int number))
                {
                    if (number < previousNumber)
                    {
                        batchCount++;
                    }
                    previousNumber = number;
                }
            }
            reader.Close();

            batchCount++;
            if (isEmpty == true) return 0;
            return batchCount;
        }

        static int SumOfSeries(Tape[] tapes)
        {
            int sum = 0;

            foreach (Tape tape in tapes)
            {
                sum += tape.BatchTotal;
            }

            return sum;
        }

        static void PreSort(string inputFilePath)
        {
            int recordsInBuffer = 1024 * 1024 * 64;

            StreamReader inputReader = new StreamReader(inputFilePath);
            StreamWriter streamWriter = new StreamWriter($"copy_{inputFilePath}");

            while (!inputReader.EndOfStream)
            {
                int[] buffer = new int[recordsInBuffer];

                for (int i = 0; i < recordsInBuffer && !inputReader.EndOfStream; i++)
                {
                    string line = inputReader.ReadLine();
                    if (line != null) buffer[i] = int.Parse(line);
                }

                Array.Sort(buffer);

                foreach (var item in buffer)
                {
                    if (item == 0) continue;
                    streamWriter.WriteLine(item);
                }
            }

            inputReader.Close();
            streamWriter.Close();

            File.Delete(inputFilePath);
            File.Move($"copy_{inputFilePath}", $"{inputFilePath}");
        }
    }
}
