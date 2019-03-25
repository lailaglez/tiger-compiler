using System.Reflection.Emit;
using System.Text;
using Antlr.Runtime;
using Complements;
using Semantics;
using Errors;

namespace AST
{
    public abstract class AtomicNode : ExpressionNode
    {
        protected AtomicNode(IToken payload = null) : base(payload) { }
    }

    public class IntNode : AtomicNode
    {
        private int Value { get; set; }

        public IntNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            int val;
            if (!int.TryParse(Text, out val))
                report.AddError(SemanticErrors.InvalidIntegerConstant(this, Text));
            else
            {
                TigerType = TigerType.Int;
                Value = val;
            }
        }

        public override void GenerateCode(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldc_I4, Value);
        }
    }

    public class StringNode : AtomicNode
    {
        private string Value;

        public StringNode(IToken payload = null) : base(payload) { }

        private string Parse()
        {
            var sb = new StringBuilder();
            for (int i = 1; i < Text.Length - 1; i++)
                if (Text[i] != '\\')
                    sb.Append(Text[i]);
                else
                {
                    char c = Text[++i];
                    if (char.IsDigit(c))
                        sb.Append((char)int.Parse(c + Text[++i].ToString() + Text[++i]));
                    else if (char.IsWhiteSpace(c))
                        while (Text[++i] != '\\') { }
                    else
                        switch (c)
                        {
                            case 'n':
                                sb.Append('\n');
                                break;
                            case 't':
                                sb.Append('\t');
                                break;
                            case 'r':
                                sb.Append('\r');
                                break;
                            case '\\':
                                sb.Append('\\');
                                break;
                        }
                }
            return sb.ToString();
        }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            Value = Parse();

            TigerType = TigerType.String;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldstr, Value);
        }
    }

    public class NilNode : AtomicNode
    {
        public NilNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            TigerType = TigerType.Nil;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldnull);
        }
    }
}
