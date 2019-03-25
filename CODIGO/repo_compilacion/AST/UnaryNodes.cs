using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using Semantics; using Errors;

namespace AST
{
    public abstract class UnaryNode : ExpressionNode
    {
        protected UnaryNode(IToken payload = null) : base(payload) { }
    }

    public class NegNode : UnaryNode
    {
        private ExpressionNode ExpressionNode => Children[0] as ExpressionNode;

        public NegNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check child
            ExpressionNode.CheckSemantics(scope, report);

            if (!ExpressionNode.IsOK)
                return;

            //Check child type
            if (TigerType.AreCompatible(TigerType.Int, ExpressionNode.TigerType))
                TigerType = TigerType.Int;
            else
                report.AddError(SemanticErrors.ArithmeticOperandInvalidType(ExpressionNode, ExpressionNode.TigerType));
        }

        public override void GenerateCode(ILGenerator generator)
        {
            ExpressionNode.GenerateCode(generator);
            generator.Emit(OpCodes.Neg);
        }
    }
}
