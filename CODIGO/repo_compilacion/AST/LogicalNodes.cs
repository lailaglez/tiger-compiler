using System.Reflection.Emit;
using Antlr.Runtime;

namespace AST
{
    public class AndNode : LogicalNode
    {
        public AndNode(IToken payload = null) : base(payload) { }

        public override void GenerateCode(ILGenerator generator)
        {
            var endLabel = generator.DefineLabel();
            var zeroLabel = generator.DefineLabel();
            LeftOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Beq, zeroLabel);
            RightOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Br, endLabel);
            generator.MarkLabel(zeroLabel);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.MarkLabel(endLabel);
        }
    }

    public class OrNode : LogicalNode
    {
        public OrNode(IToken payload = null) : base(payload) { }

        public override void GenerateCode(ILGenerator generator)
        {
            var endLabel = generator.DefineLabel();
            var oneLabel = generator.DefineLabel();
            LeftOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Beq, oneLabel);
            RightOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Br, endLabel);
            generator.MarkLabel(oneLabel);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.MarkLabel(endLabel);
        }
    }
}
