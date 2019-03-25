using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using Semantics; using Errors;

namespace AST
{
    public abstract class VariableDeclarationNode : DeclarationNode
    {
        public IdNode IdNode => Children[0] as IdNode;
        public ExpressionNode ExpressionNode => Children[1] as ExpressionNode;

        public VariableInfo VariableInfo;

        protected VariableDeclarationNode(IToken payload = null) : base(payload) { }
    }

    public class ImplicitVariableDeclarationNode : VariableDeclarationNode
    {
        public ImplicitVariableDeclarationNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            IdNode.CheckSemantics(scope, report);
            ExpressionNode.CheckSemantics(scope, report);

            if (!IdNode.IsOK || !ExpressionNode.IsOK)
                return;

            //Check use of variable name
            if (!scope.VariableNameAvailable(IdNode.Name))
            {
                report.AddError(SemanticErrors.VariableNameAlreadyInUse(IdNode, IdNode.Name));
                return;
            }

            //Check children types
            if (TigerType.AreOfSameType(ExpressionNode.TigerType, TigerType.Nil) || TigerType.AreOfSameType(ExpressionNode.TigerType, TigerType.Void))

            {
                report.AddError(SemanticErrors.InvalidImplicitVariableDeclaration(ExpressionNode, ExpressionNode.TigerType));
                return;
            }
            //Add variable to scope
            VariableInfo = scope.DefineVariable(IdNode.Name, ExpressionNode.TigerType, scope);

            IsOK = true;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            ExpressionNode.GenerateCode(generator);
            generator.Emit(OpCodes.Stsfld, VariableInfo.FieldBuilder);
        }
    }

    public class ExplicitVariableDeclarationNode : VariableDeclarationNode
    {
        public TypeAccessNode TypeNode => Children[2] as TypeAccessNode;

        public ExplicitVariableDeclarationNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            IdNode.CheckSemantics(scope, report);
            TypeNode.CheckSemantics(scope, report);
            ExpressionNode.CheckSemantics(scope, report);

            if (!IdNode.IsOK || !TypeNode.IsOK || !ExpressionNode.IsOK)
                return;

            //Check use of variable name
            if (!scope.VariableNameAvailable(IdNode.Name))
            {
                report.AddError(SemanticErrors.VariableNameAlreadyInUse(IdNode, IdNode.Name));
                return;
            }

            //Check children types
            if (!TypeNode.TigerType.Assignable(ExpressionNode.TigerType))
                report.AddError(SemanticErrors.InvalidAssignType(ExpressionNode, TypeNode.TigerType, ExpressionNode.TigerType));

            //Add variable to scope
            VariableInfo = scope.DefineVariable(IdNode.Name, TypeNode.TigerType, ContainingScope);

            IsOK = true;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            ExpressionNode.GenerateCode(generator);
            generator.Emit(OpCodes.Stsfld, VariableInfo.FieldBuilder);
        }
    }
}

