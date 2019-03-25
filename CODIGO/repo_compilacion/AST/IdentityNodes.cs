using System.Reflection.Emit;
using Antlr.Runtime;

namespace AST
{
    public class EqualNode : IdentityNode
    {
        public EqualNode(IToken payload = null) : base(payload) { }

        public override void GenerateCode(ILGenerator generator)
        {
            LeftOperandNode.GenerateCode(generator);
            RightOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Ceq);
        }
    }

    public class NotEqualNode : IdentityNode
    {
        public NotEqualNode(IToken payload = null) : base(payload) { }

        public override void GenerateCode(ILGenerator generator)
        {
            LeftOperandNode.GenerateCode(generator);
            RightOperandNode.GenerateCode(generator);
            generator.Emit(OpCodes.Ceq);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ceq);
        }
    }
}
