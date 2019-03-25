using Antlr.Runtime;
using Complements;
using Errors;
using Semantics;

namespace AST
{
    public abstract class BinaryNode : ExpressionNode
    {
        protected ExpressionNode LeftOperandNode => Children[0] as ExpressionNode;
        protected ExpressionNode RightOperandNode => Children[1] as ExpressionNode;

        protected BinaryNode(IToken payload = null) : base(payload) { }
    }

    public abstract class ArithmeticNode : BinaryNode
    {
        protected ArithmeticNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            LeftOperandNode.CheckSemantics(scope, report);
            RightOperandNode.CheckSemantics(scope, report);
            if (!LeftOperandNode.IsOK || !RightOperandNode.IsOK)
                return;

            TigerType = TigerType.Int;

            //Check children types
            if (!TigerType.AreCompatible(LeftOperandNode.TigerType, TigerType.Int))
                    report.AddError(SemanticErrors.ArithmeticOperandInvalidType(LeftOperandNode, LeftOperandNode.TigerType));
            else if (!TigerType.AreCompatible(RightOperandNode.TigerType, TigerType.Int))
                report.AddError(SemanticErrors.ArithmeticOperandInvalidType(RightOperandNode, RightOperandNode.TigerType));
        }
    }

    public abstract class RelationalNode : BinaryNode
    {
        protected RelationalNode(IToken payload = null) : base(payload) { }
    }

    public abstract class IdentityNode : RelationalNode
    {
        protected IdentityNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            LeftOperandNode.CheckSemantics(scope, report);
            RightOperandNode.CheckSemantics(scope, report);
            if (!LeftOperandNode.IsOK || !RightOperandNode.IsOK)
                return;

            TigerType = TigerType.Int;

            //Check children types
            if (!TigerType.AreCompatible(LeftOperandNode.TigerType, RightOperandNode.TigerType) ||
                (TigerType.AreOfSameType(LeftOperandNode.TigerType, TigerType.Nil) && TigerType.AreOfSameType(RightOperandNode.TigerType, TigerType.Nil)))
                report.AddError(SemanticErrors.InvalidIdentityComparison(this, LeftOperandNode.TigerType,
                    RightOperandNode.TigerType));
        }
    }

    public abstract class OrderNode : RelationalNode
    {
        protected OrderNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            LeftOperandNode.CheckSemantics(scope, report);
            RightOperandNode.CheckSemantics(scope, report);
            if (!LeftOperandNode.IsOK || !RightOperandNode.IsOK)
                return;

            //Check children types
            if ((TigerType.AreCompatible(LeftOperandNode.TigerType, TigerType.Int) && TigerType.AreCompatible(RightOperandNode.TigerType, TigerType.Int)) ||
                (TigerType.AreCompatible(LeftOperandNode.TigerType, TigerType.String) && TigerType.AreCompatible(RightOperandNode.TigerType, TigerType.String)))
                TigerType = TigerType.Int;
            else
                report.AddError(SemanticErrors.InvalidOrderComparison(this, LeftOperandNode.TigerType,
                    RightOperandNode.TigerType));
        }
    }

    public abstract class LogicalNode : BinaryNode
    {
        protected LogicalNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            LeftOperandNode.CheckSemantics(scope, report);
            RightOperandNode.CheckSemantics(scope, report);
            if (!LeftOperandNode.IsOK || !RightOperandNode.IsOK)
                return;
            
            //Check children types
            if (TigerType.AreCompatible(LeftOperandNode.TigerType, TigerType.Int) && TigerType.AreCompatible(RightOperandNode.TigerType, TigerType.Int))
                TigerType = TigerType.Int;
            else if (!TigerType.AreCompatible(LeftOperandNode.TigerType, TigerType.Int))
                report.AddError(SemanticErrors.LogicalOperandInvalidType(LeftOperandNode, LeftOperandNode.TigerType));
            else
                report.AddError(SemanticErrors.LogicalOperandInvalidType(RightOperandNode, RightOperandNode.TigerType));
        }
    }
}
