using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using Semantics; using Errors;

namespace AST
{
    public abstract class MethodDeclarationNode : DeclarationNode
    {
        public IdNode IdNode => Children[0] as IdNode;
        public virtual ParameterDeclarationNode[] ParameterDeclarationNodes { get; }
        public virtual InstructionNode FunctionBodyNode { get; }
        public virtual TypeAccessNode ReturnTypeNode { get; }

        public FunctionInfo FunctionInfo;
        private TigerScope FunctionScope;

        protected MethodDeclarationNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            IdNode.CheckSemantics(scope, report);

            FunctionScope = new TigerScope(scope, IdNode.Name);

            ParameterDeclarationNodes.ToList().ForEach(p => p.CheckSemantics(FunctionScope, report));

            if (!IdNode.IsOK || ParameterDeclarationNodes.Any(p => !p.IsOK))
                return;

            //Check existence of function name
            if (!scope.FunctionNameAvailable(IdNode.Name))
            {
                report.AddError(SemanticErrors.FunctionNameAlreadyInUse(IdNode, IdNode.Name));
                return;
            }

            if (ReturnTypeNode != null)
            {
                ReturnTypeNode.CheckSemantics(scope, report);
                if (!ReturnTypeNode.IsOK)
                    return;
                FunctionInfo = scope.DefineFunction(IdNode.Name, ReturnTypeNode.TigerType,
                    ParameterDeclarationNodes.Select(p => p.TypeNode.TigerType).ToArray(), FunctionScope);
            }
            else
                FunctionInfo = scope.DefineFunction(IdNode.Name, TigerType.Void,
                    ParameterDeclarationNodes.Select(p => p.TypeNode.TigerType).ToArray(), FunctionScope);
            IsOK = true;
        }

        public void CheckBodySemantics(TigerScope scope, Report report)
        {
            //If CheckSemantics failed (FunctionInfo was not created) return
            if (!IsOK)
                return;

            IsOK = false;
                
            //Create function scope
            FunctionBodyNode.CheckSemantics(FunctionScope, report);

            if (!FunctionBodyNode.IsOK)
                return;

            IsOK = true;

            if (ReturnTypeNode != null && !ReturnTypeNode.TigerType.Assignable(FunctionBodyNode.TigerType))
                report.AddError(SemanticErrors.IncompatibleFunctionReturnTypeBody(FunctionBodyNode, ReturnTypeNode.TigerType, FunctionBodyNode.TigerType));
            else if (ReturnTypeNode == null && !TigerType.AreOfSameType(TigerType.Void, FunctionBodyNode.TigerType))
                report.AddError(SemanticErrors.IncompatibleFunctionReturnTypeBody(FunctionBodyNode, TigerType.Void, FunctionBodyNode.TigerType));
        }

        public override void GenerateCode(ILGenerator generator)
        {
            for (var i = 0; i < ParameterDeclarationNodes.Length; i++)
            {
                var parameter = ParameterDeclarationNodes[i];
                var field = ProgramNode.Program.DefineField(parameter.VariableInfo.StaticName,
                    parameter.TypeNode.TigerType.Type, FieldAttributes.Public | FieldAttributes.Static);
                generator.Emit(OpCodes.Ldarg, i);
                generator.Emit(OpCodes.Stsfld, field);
                parameter.VariableInfo.FieldBuilder = field;
            }
            FunctionBodyNode.GenerateCode(generator);  //genera el codigo del cuerpo del metodo con el generator obtenido para el nuevo metodo
            generator.Emit(OpCodes.Ret);
        }
    }

    public class FunctionDeclarationNode : MethodDeclarationNode
    {
        public override TypeAccessNode ReturnTypeNode => Children[1] as TypeAccessNode;

        public override ParameterDeclarationNode[] ParameterDeclarationNodes => Children.Skip(2).Take(Children.Count - 3).Cast<ParameterDeclarationNode>().ToArray();
        
        public override InstructionNode FunctionBodyNode => Children.Last() as InstructionNode;

        public FunctionDeclarationNode(IToken payload = null) : base(payload) { }
    }

    public class ProcedureDeclarationNode : MethodDeclarationNode
    {
        public override ParameterDeclarationNode[] ParameterDeclarationNodes => Children.Skip(1).Take(Children.Count - 2).Cast<ParameterDeclarationNode>().ToArray();

        public override InstructionNode FunctionBodyNode => Children.Last() as InstructionNode;

        public ProcedureDeclarationNode(IToken payload = null) : base(payload) { }
    }

    public class ParameterDeclarationNode : ExpressionNode
    {
        private IdNode IdNode => Children[0] as IdNode;

        public TypeAccessNode TypeNode => Children[1] as TypeAccessNode;

        public VariableInfo VariableInfo;

        public ParameterDeclarationNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            IdNode.CheckSemantics(scope, report);
            TypeNode.CheckSemantics(scope, report);

            if (!IdNode.IsOK || !TypeNode.IsOK)
                return;

            //Check use of variable name
            if (!scope.VariableNameAvailable(IdNode.Name))
            {
                report.AddError(SemanticErrors.RepeatedParameterName(IdNode, IdNode.Name));
                return;
            }

            //Add variable to scope
            VariableInfo = scope.DefineVariable(IdNode.Name, TypeNode.TigerType, scope);
            TigerType = TypeNode.TigerType;
        }

        public override void GenerateCode(ILGenerator generator) { }
    }
}
