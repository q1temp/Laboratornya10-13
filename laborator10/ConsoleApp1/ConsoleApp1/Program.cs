using System;
using System.IO;

namespace ConsoleApp1
{
    class Program
    {
        static void Main()
        {
            string filePath = "C:\\Users\\boxer\\Desktop\\YP\\laba10\\paskal2.txt";

            if (!File.Exists(filePath))
            {
                Console.WriteLine("Файл не найден!");
                return;
            }

            InputOutput.Init(filePath);
            LexicalAnalyzer la = new LexicalAnalyzer();
            la.Analyze();

            SyntaxAnalyzer sa = new SyntaxAnalyzer();
            sa.Analyze();

            InputOutput.Finish();

            Console.ReadKey();
        }
    }
}