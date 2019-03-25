using System.Reflection.Emit;
using Antlr.Runtime;

namespace AST
{
    public class PlusNode : ArithmeticNode
    {
        public PlusNode(IToken payload = null) : base(payload) { }
        public override void GenerateCode(ILGenerator generator)
        {
            LeftOperandNode.GenerateCode(generator);
            RightOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Add);
        }
    }

    public class MinusNode : ArithmeticNode
    {
        public MinusNode(IToken payload = null) : base(payload) { }
        public override void GenerateCode(ILGenerator generator)
        {
            LeftOperandNode.GenerateCode(generator);
            RightOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Sub);
        }
    }

    public class MultNode : ArithmeticNode
    {
        public MultNode(IToken payload = null) : base(payload) { }
        public override void GenerateCode(ILGenerator generator)
        {
            LeftOperandNode.GenerateCode(generator);
            RightOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Mul);
        }
    }

    public class DivNode : ArithmeticNode
    {
        public DivNode(IToken payload = null) : base(payload) { }
        public override void GenerateCode(ILGenerator generator)
        {
            LeftOperandNode.GenerateCode(generator);
            RightOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Div);
        }
    }
}