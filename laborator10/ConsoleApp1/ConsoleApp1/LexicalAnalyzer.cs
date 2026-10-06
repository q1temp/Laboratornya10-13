using System;

namespace ConsoleApp1
{
    class LexicalAnalyzer
    {
        public const byte dosy = 1, ifsy = 2, insy = 3, ofsy = 4, orsy = 5, tosy = 6,
                          endsy = 7, varsy = 8, divsy = 9, andsy = 10, notsy = 11, forsym = 12,
                          modsy = 13, nilsy = 14, setsy = 16, thensy = 17, elsesy = 18,
                          casesy = 19, filesy = 20, gotosy = 21, typesy = 22, withsy = 23,
                          beginsy = 24, whilesy = 25, arraysy = 26, constsy = 27, labelsy = 28,
                          untilsy = 29, downtosy = 30, packedsy = 31, recordsy = 32,
                          repeatsy = 33, programsy = 34, functionsy = 35, procedurensy = 36;

        public const byte identsy = 50;
        public const byte intcon = 51;
        public const byte strcon = 52;

        public const byte assignOp = 100;   
        public const byte rangeOp = 101;   
        public const byte leOp = 102;   
        public const byte geOp = 103;   
        public const byte neOp = 104;   

        private bool _inComment;
        private bool _inBraceComment;
        private int _commentStartLine;
        private int _commentStartPos;
        private int _braceStartLine;
        private int _braceStartPos;

        private Keywords _keywords;
        private const string ValidSpecialChars = "+-*/=<>:;,.()[]{}' ^@#$\\";

        public void Analyze()
        {
            InputOutput.ResetErrors();
            _inComment = false;
            _inBraceComment = false;
            _keywords = new Keywords();

            while (InputOutput.Ch != '\0')
            {
                SkipWhitespace();
                if (InputOutput.Ch == '\0') break;

                int line = (int)InputOutput.PositionNow.lineNumber;
                int pos = InputOutput.PositionNow.charNumber;

                if (_inComment)
                {
                    if (InputOutput.Ch == '*' && InputOutput.PeekNext() == ')')
                    {
                        _inComment = false;
                        InputOutput.NextCh();
                        InputOutput.NextCh();
                    }
                    else
                    {
                        int currentLine = (int)InputOutput.PositionNow.lineNumber;
                        InputOutput.NextCh();
                        if ((int)InputOutput.PositionNow.lineNumber != currentLine)
                        {
                            InputOutput.AddError(_commentStartLine, _commentStartPos, 205,
                                "Незакрытый блочный комментарий (* ... *)");
                            _inComment = false;
                        }
                    }
                    continue;
                }

                if (_inBraceComment)
                {
                    if (InputOutput.Ch == '}')
                    {
                        _inBraceComment = false;
                        InputOutput.NextCh();
                    }
                    else
                    {
                        int currentLine = (int)InputOutput.PositionNow.lineNumber;
                        InputOutput.NextCh();
                        if ((int)InputOutput.PositionNow.lineNumber != currentLine)
                        {
                            InputOutput.AddError(_braceStartLine, _braceStartPos, 207,
                                "Незакрытый альтернативный блочный комментарий { ... }");
                            _inBraceComment = false;
                        }
                    }
                    continue;
                }

                if (InputOutput.Ch == '(' && InputOutput.PeekNext() == '*')
                {
                    _inComment = true; _commentStartLine = line; _commentStartPos = pos;
                    InputOutput.NextCh(); InputOutput.NextCh(); continue;
                }

                if (InputOutput.Ch == '{')
                {
                    _inBraceComment = true; _braceStartLine = line; _braceStartPos = pos;
                    InputOutput.NextCh(); continue;
                }

                if (InputOutput.Ch == '/' && InputOutput.PeekNext() == '/')
                {
                    SkipLineComment(); continue;
                }

                if (InputOutput.Ch == '\'') { ScanString(); continue; }
                if (Char.IsDigit(InputOutput.Ch)) { ScanNumber(line, pos); continue; }

                if (Char.IsLetter(InputOutput.Ch))
                {
                    string name = "";
                    while (Char.IsLetterOrDigit(InputOutput.Ch))
                    {
                        name += InputOutput.Ch;
                        InputOutput.NextCh();
                    }
                    byte code = _keywords.CheckKeyword(name);
                    if (code == 0) code = identsy;

                    InputOutput.AddCode(code);
                    InputOutput.AddToken(new Token(code, name, line, pos));   
                    continue;
                }

                if (InputOutput.Ch == ':' && InputOutput.PeekNext() == '=')
                {
                    InputOutput.AddCode(assignOp);
                    InputOutput.AddToken(new Token(assignOp, ":=", line, pos));
                    InputOutput.NextCh(); InputOutput.NextCh(); continue;
                }
                if (InputOutput.Ch == '.' && InputOutput.PeekNext() == '.')
                {
                    InputOutput.AddCode(rangeOp);
                    InputOutput.AddToken(new Token(rangeOp, "..", line, pos));
                    InputOutput.NextCh(); InputOutput.NextCh(); continue;
                }
                if (InputOutput.Ch == '<' && InputOutput.PeekNext() == '=')
                {
                    InputOutput.AddCode(leOp);
                    InputOutput.AddToken(new Token(leOp, "<=", line, pos));
                    InputOutput.NextCh(); InputOutput.NextCh(); continue;
                }
                if (InputOutput.Ch == '>' && InputOutput.PeekNext() == '=')
                {
                    InputOutput.AddCode(geOp);
                    InputOutput.AddToken(new Token(geOp, ">=", line, pos));
                    InputOutput.NextCh(); InputOutput.NextCh(); continue;
                }
                if (InputOutput.Ch == '<' && InputOutput.PeekNext() == '>')
                {
                    InputOutput.AddCode(neOp);
                    InputOutput.AddToken(new Token(neOp, "<>", line, pos));
                    InputOutput.NextCh(); InputOutput.NextCh(); continue;
                }

                if (InputOutput.Ch == ';')
                {
                    InputOutput.AddCode((int)';');
                    InputOutput.AddToken(new Token((byte)';', ";", line, pos));
                    CheckAfterSemicolon(line, pos);
                    continue;
                }

                if (IsValidPascalChar(InputOutput.Ch))
                {
                    char c = InputOutput.Ch;
                    InputOutput.AddCode((int)c);
                    InputOutput.AddToken(new Token((byte)c, c.ToString(), line, pos));
                    InputOutput.NextCh(); continue;
                }

                InputOutput.AddError(line, pos, 208, $"Недопустимый символ '{InputOutput.Ch}'");
                InputOutput.NextCh();
            }

            if (_inComment)
                InputOutput.AddError(_commentStartLine, _commentStartPos, 205,
                    "Незакрытый блочный комментарий (* ... *)");
            if (_inBraceComment)
                InputOutput.AddError(_braceStartLine, _braceStartPos, 207,
                    "Незакрытый альтернативный блочный комментарий { ... }");
        }

        private void CheckAfterSemicolon(int semicolonLine, int semicolonPos)
        {
            int currentLine = (int)InputOutput.PositionNow.lineNumber;
            InputOutput.NextCh();
            while (InputOutput.Ch != '\0')
            {
                if ((int)InputOutput.PositionNow.lineNumber != currentLine) return;
                if (Char.IsWhiteSpace(InputOutput.Ch)) { InputOutput.NextCh(); continue; }
                if (InputOutput.Ch == '/' && InputOutput.PeekNext() == '/') return;
                if (InputOutput.Ch == '(' && InputOutput.PeekNext() == '*') return;
                if (InputOutput.Ch == '{') return;

                int errPos = (int)InputOutput.PositionNow.charNumber;
                InputOutput.AddError(semicolonLine, errPos, 209,
                    "Недопустимые символы после ';' в одной строке");

                while (InputOutput.Ch != '\0')
                {
                    if ((int)InputOutput.PositionNow.lineNumber != currentLine) break;
                    InputOutput.NextCh();
                }
                return;
            }
        }

        private bool IsValidPascalChar(char c) => ValidSpecialChars.IndexOf(c) >= 0;

        private void SkipWhitespace()
        {
            while (InputOutput.Ch != '\0' && Char.IsWhiteSpace(InputOutput.Ch))
                InputOutput.NextCh();
        }

        private void SkipLineComment()
        {
            InputOutput.NextCh(); InputOutput.NextCh();
            int currentLine = (int)InputOutput.PositionNow.lineNumber;
            while (InputOutput.Ch != '\0')
            {
                if ((int)InputOutput.PositionNow.lineNumber != currentLine) break;
                InputOutput.NextCh();
            }
        }

        private void ScanString()
        {
            int startLine = (int)InputOutput.PositionNow.lineNumber;
            int startPos = InputOutput.PositionNow.charNumber;
            InputOutput.AddCode(strcon);
            InputOutput.AddToken(new Token(strcon, "'...'", startLine, startPos));  

            InputOutput.NextCh();
            bool closed = false;
            int currentLine = startLine;

            while (InputOutput.Ch != '\0')
            {
                if ((int)InputOutput.PositionNow.lineNumber != currentLine) break;
                if (InputOutput.Ch == '\'')
                {
                    if (InputOutput.PeekNext() == '\'') InputOutput.NextCh();
                    else { closed = true; InputOutput.NextCh(); break; }
                }
                InputOutput.NextCh();
            }
            if (!closed)
                InputOutput.AddError(startLine, startPos, 206, "Незакрытая строковая константа");
        }

        private void ScanNumber(int line, int pos)
        {
            string num = "";
            long value = 0;
            bool overflow = false;

            while (InputOutput.Ch != '\0' && Char.IsDigit(InputOutput.Ch))
            {
                num += InputOutput.Ch;
                int digit = InputOutput.Ch - '0';
                if (value > (32767 - digit) / 10) overflow = true;
                value = value * 10 + digit;
                InputOutput.NextCh();
            }

            InputOutput.AddCode(intcon);
            InputOutput.AddToken(new Token(intcon, num, line, pos)); 

            if (overflow || value > 32767)
                InputOutput.AddError(line, pos, 203, "Число слишком большое для integer (максимум 32767)");
        }
    }
}