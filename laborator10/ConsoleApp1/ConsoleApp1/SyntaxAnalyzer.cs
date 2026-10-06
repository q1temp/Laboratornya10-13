using System;
using System.Collections.Generic;

namespace ConsoleApp1
{
    class SyntaxAnalyzer
    {
        private List<Token> _tokens;
        private int _pos;

        private static readonly HashSet<string> SimpleTypes = new HashSet<string>
        {
            "integer", "real", "boolean", "char", "string",
            "byte", "word", "longint", "shortint", "single", "double"
        };

        private const int ErrSyntax = 300;

        public void Analyze()
        {
            _tokens = InputOutput.GetTokens();
            _pos = 0;
            if (_tokens.Count == 0) return;
            ParseProgram();
        }

        private Token Cur => _pos < _tokens.Count ? _tokens[_pos] : null;
        private Token Peek(int k = 1) => (_pos + k) < _tokens.Count ? _tokens[_pos + k] : null;
        private bool IsEof => _pos >= _tokens.Count;

        private void Advance() { if (!IsEof) _pos++; }

        private bool Check(byte code) => !IsEof && Cur.Code == code;

        private bool Accept(byte code)
        {
            if (Check(code)) { Advance(); return true; }
            return false;
        }

        private bool Expect(byte code, string msg)
        {
            if (Accept(code)) return true;
            Error(msg);
            return false;
        }

        private void Error(string msg)
        {
            Token t = Cur;
            int line = t != null ? t.Line : 0;
            int pos = t != null ? t.Pos : 0;
            InputOutput.AddError(line, pos, ErrSyntax, "Синтаксическая ошибка: " + msg);
        }

        private void SkipTo(params byte[] sync)
        {
            var set = new HashSet<byte>(sync);
            while (!IsEof && !set.Contains(Cur.Code)) Advance();
        }

        private void ParseProgram()
        {
            Expect(LexicalAnalyzer.programsy, "ожидается 'program'");
            Expect(LexicalAnalyzer.identsy, "ожидается имя программы");
            Expect((byte)';', "ожидается ';' после заголовка");
            ParseBlock();
            Expect((byte)'.', "ожидается '.' в конце программы");
        }

        private void ParseBlock()
        {
            if (Check(LexicalAnalyzer.varsy)) ParseVarSection();

            while (Check(LexicalAnalyzer.procedurensy))
                ParseProcedureDecl();

            ParseCompound();
        }

        private void ParseVarSection()
        {
            Expect(LexicalAnalyzer.varsy, "ожидается 'var'");
            ParseVarDecl();

            while (Accept((byte)';'))
            {
                if (IsEof) return;
                if (Check(LexicalAnalyzer.beginsy) ||
                    Check(LexicalAnalyzer.procedurensy)) return;
                ParseVarDecl();
            }
        }

        private void ParseVarDecl()
        {
            if (!Expect(LexicalAnalyzer.identsy, "ожидается идентификатор в описании переменных"))
            {
                SkipTo((byte)';', LexicalAnalyzer.beginsy, LexicalAnalyzer.procedurensy);
                return;
            }

            while (Accept((byte)','))
            {
                if (!Expect(LexicalAnalyzer.identsy, "ожидается идентификатор после ','"))
                {
                    SkipTo((byte)':', (byte)';', LexicalAnalyzer.beginsy);
                    return;
                }
            }

            if (!Expect((byte)':', "ожидается ':' после списка идентификаторов"))
            {
                SkipTo((byte)';', LexicalAnalyzer.beginsy);
                return;
            }

            if (!Check(LexicalAnalyzer.identsy) ||
                !SimpleTypes.Contains(Cur.Value.ToLower()))
            {
                Error("ожидается простой тип (integer, real, boolean, char, string)");
                SkipTo((byte)';', LexicalAnalyzer.beginsy, LexicalAnalyzer.procedurensy);
                return;
            }
            Advance(); 
        }

        private void ParseProcedureDecl()
        {
            Expect(LexicalAnalyzer.procedurensy, "ожидается 'procedure'");
            Expect(LexicalAnalyzer.identsy, "ожидается имя процедуры");

            if (Check((byte)'('))
            {
                int depth = 0;
                while (!IsEof)
                {
                    if (Cur.Code == (byte)'(') depth++;
                    else if (Cur.Code == (byte)')')
                    {
                        depth--;
                        Advance();
                        if (depth == 0) break;
                        continue;
                    }
                    Advance();
                }
            }

            Expect((byte)';', "ожидается ';' после заголовка процедуры");
            ParseBlock();
            Expect((byte)';', "ожидается ';' после тела процедуры");
        }

        private void ParseCompound()
        {
            if (!Expect(LexicalAnalyzer.beginsy, "ожидается 'begin'"))
            {
                SkipTo(LexicalAnalyzer.beginsy, LexicalAnalyzer.endsy,
                       LexicalAnalyzer.procedurensy, (byte)'.');
                if (!Accept(LexicalAnalyzer.beginsy)) return;
            }

            if (!Check(LexicalAnalyzer.endsy) && !IsEof)
            {
                ParseStatement();
                while (Accept((byte)';'))
                {
                    if (Check(LexicalAnalyzer.endsy) || IsEof) break;
                    ParseStatement();
                }
            }

            Expect(LexicalAnalyzer.endsy, "ожидается 'end'");
        }

        private void ParseStatement()
        {
            if (Check(LexicalAnalyzer.beginsy))
            {
                ParseCompound();
            }
            else if (Check(LexicalAnalyzer.identsy))
            {
                string name = Cur.Value;
                Advance();

                if (Check(LexicalAnalyzer.assignOp))
                {
                    Advance();          // :=
                    ParseExpression();
                }
                else if (Check((byte)'('))
                {
                    int depth = 0;
                    while (!IsEof)
                    {
                        if (Cur.Code == (byte)'(') depth++;
                        else if (Cur.Code == (byte)')')
                        {
                            depth--;
                            Advance();
                            if (depth == 0) break;
                            continue;
                        }
                        Advance();
                    }
                }
                else if (Check((byte)';') || Check(LexicalAnalyzer.endsy))
                {
                }
                else
                {
                    Error($"после '{name}' ожидается ':=' или '('");
                    SkipTo((byte)';', LexicalAnalyzer.endsy);
                }
            }
            else
            {
                Error("ожидается оператор (присваивание, вызов процедуры или составной)");
                SkipTo((byte)';', LexicalAnalyzer.endsy);
            }
        }

        private void ParseExpression()
        {
            int depth = 0;
            while (!IsEof)
            {
                var t = Cur;
                if (depth == 0 &&
                    (t.Code == (byte)';' || t.Code == LexicalAnalyzer.endsy))
                    break;

                if (t.Code == (byte)'(' || t.Code == (byte)'[') depth++;
                else if (t.Code == (byte)')' || t.Code == (byte)']') depth--;

                Advance();
            }
        }
    }
}