namespace ConsoleApp1
{
    public class Token
    {
        public byte Code;       
        public string Value;  
        public int Line;       
        public int Pos;         

        public Token(byte code, string value, int line, int pos)
        {
            Code = code;
            Value = value;
            Line = line;
            Pos = pos;
        }

        public override string ToString()
        {
            return $"({Code}, '{Value}', {Line}:{Pos})";
        }
    }
}