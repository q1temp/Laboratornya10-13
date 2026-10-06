using System;
using System.Collections.Generic;
using System.IO;

namespace ConsoleApp1
{
    class InputOutput
    {
        public struct TextPosition
        {
            public uint lineNumber;
            public byte charNumber;
        }

        private static char _ch;
        private static TextPosition _currentPosition;
        private static string _line;
        private static byte _lastInLine;
        private static StreamReader _file;
        private static uint _currentLineNumber;
        private static List<string> _allLines;
        private static Dictionary<int, List<ErrorInfo>> _errors;
        private static List<int> _codes;
        private static List<Token> _tokens;       

        private class ErrorInfo
        {
            public int CharPos;
            public int Code;
            public string Message;
        }

        public static char Ch
        {
            get { return _ch; }
            private set { _ch = value; }
        }

        public static TextPosition PositionNow
        {
            get { return _currentPosition; }
            private set { _currentPosition = value; }
        }

        public static void Init(string fileName)
        {
            _file = new StreamReader(fileName);
            _allLines = new List<string>();
            _errors = new Dictionary<int, List<ErrorInfo>>();
            _codes = new List<int>();
            _tokens = new List<Token>();          
            _currentLineNumber = 0;
            _currentPosition = new TextPosition();
            ReadNextLine();
        }

        public static char PeekNext()
        {
            if (_currentPosition.charNumber + 1 <= _lastInLine)
                return _line[_currentPosition.charNumber + 1];
            return '\0';
        }

        public static void NextCh()
        {
            if (Ch == '\0') return;
            if (_currentPosition.charNumber >= _lastInLine)
            {
                ReadNextLine();
                if (Ch == '\0') return;
                _currentPosition.charNumber = 0;
            }
            else
            {
                _currentPosition.charNumber++;
                Ch = _line[_currentPosition.charNumber];
            }
        }

        private static void ReadNextLine()
        {
            if (!_file.EndOfStream)
            {
                _line = _file.ReadLine();
                if (_line == null) { Ch = '\0'; return; }
                _allLines.Add(_line);
                _currentLineNumber++;
                _currentPosition.lineNumber = _currentLineNumber;
                if (_line.Length == 0) _line = " ";
                _lastInLine = (byte)(_line.Length - 1);
                Ch = _line[0];
                _currentPosition.charNumber = 0;
            }
            else Ch = '\0';
        }

        public static void ResetErrors()
        {
            _errors.Clear();
        }

        public static void AddError(int lineNumber, int charPos, int code, string message)
        {
            if (!_errors.ContainsKey(lineNumber))
                _errors[lineNumber] = new List<ErrorInfo>();
            _errors[lineNumber].Add(new ErrorInfo { CharPos = charPos, Code = code, Message = message });
        }

        public static void AddCode(int code)
        {
            _codes.Add(code);
        }

        public static void AddToken(Token t)
        {
            _tokens.Add(t);
        }

        public static List<Token> GetTokens()
        {
            return _tokens;
        }

        public static void Finish()
        {
            for (int i = 0; i < _allLines.Count; i++)
            {
                int lineNum = i + 1;
                string prefix = $"{lineNum,4} ";
                Console.WriteLine($"{prefix}{_allLines[i]}");

                if (_errors.ContainsKey(lineNum))
                {
                    foreach (var err in _errors[lineNum])
                    {
                        int visualPos = 0;
                        for (int j = 0; j < err.CharPos && j < _allLines[i].Length; j++)
                        {
                            if (_allLines[i][j] == '\t')
                                visualPos += 4 - (visualPos % 4);
                            else visualPos++;
                        }
                        string arrow = new string(' ', visualPos) + "^";
                        Console.WriteLine($"{new string(' ', prefix.Length)}{arrow} ошибка {err.Code}: {err.Message}");
                    }
                }
            }

            int totalErrors = 0;
            foreach (var list in _errors.Values) totalErrors += list.Count;

            Console.WriteLine();
            Console.WriteLine($"Компиляция завершена: ошибок — {totalErrors}");

            string codesFilePath = "codes.txt";
            File.WriteAllText(codesFilePath, string.Join(" ", _codes));
            Console.WriteLine($"Коды символов записаны в файл: {codesFilePath}");

            _file.Close();
        }
    }
}