using System.Reflection.Emit;
using Antlr.Runtime;

namespace AST
{
    public class GreaterNode : OrderNode
    {
        public GreaterNode(IToken payload = null) : base(payload) { }

        public override void GenerateCode(ILGenerator generator)
        {
            LeftOperandNode.GenerateCode(generator);
            RightOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Cgt);
        }
    }

    public class GeqNode : OrderNode
    {
        public GeqNode(IToken payload = null) : base(payload) { }

        public override void GenerateCode(ILGenerator generator)
        {
            LeftOperandNode.GenerateCode(generator);
            RightOperandNode.GenerateCode(generator);
            var endLabel = generator.DefineLabel();
            var geLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Bge, geLabel);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Br, endLabel);
            generator.MarkLabel(geLabel);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.MarkLabel(endLabel);
        }
    }

    public class LessNode : OrderNode
    {
        public LessNode(IToken payload = null) : base(payload) { }

        public override void GenerateCode(ILGenerator generator)
        {
            LeftOperandNode.GenerateCode(generator);
            RightOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Clt);
        }
    }

    public class LeqNode : OrderNode
    {
        public LeqNode(IToken payload = null) : base(payload) { }

        public override void GenerateCode(ILGenerator generator)
        {
            LeftOperandNode.GenerateCode(generator);
            RightOperandNode.GenerateCode(generator);
            var endLabel = generator.DefineLabel();
            var leLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Ble, leLabel);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Br, endLabel);
            generator.MarkLabel(leLabel);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.MarkLabel(endLabel);
        }
    }
}
