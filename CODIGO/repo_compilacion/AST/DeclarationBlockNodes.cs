using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using COMPLEMENTS;
using Errors;

namespace AST
{
    public abstract class DeclarationBlockNode : InstructionNode
    {
        protected DeclarationBlockNode(IToken payload = null) : base(payload) { }
    }

    public class VariableDeclarationBlockNode : DeclarationBlockNode
    {
        private VariableDeclarationNode[] VariableDeclarationNodes => Children.Cast<VariableDeclarationNode>().ToArray();

        public VariableDeclarationBlockNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Checking children
            VariableDeclarationNodes.ToList().ForEach(v => v.CheckSemantics(scope, report));

            IsOK = true;

            foreach (var v in VariableDeclarationNodes.Where(v => !v.IsOK && !(v is ExplicitVariableDeclarationNode)))
            {
                scope.RemoveVariable(v.IdNode.Name);
                IsOK = false;
            }
        }

        public override void GenerateCode(ILGenerator generator)
        {
            foreach (var variableDeclarationNode in VariableDeclarationNodes)
            {
                FieldBuilder newField;
                var fieldName = variableDeclarationNode.VariableInfo.StaticName;
                if (variableDeclarationNode is ImplicitVariableDeclarationNode)
                    newField = ProgramNode.Program.DefineField(fieldName,
                        variableDeclarationNode.ExpressionNode.TigerType.Type,
                        FieldAttributes.Public | FieldAttributes.Static);
                else
                    newField = ProgramNode.Program.DefineField(fieldName,
                        ((ExplicitVariableDeclarationNode)variableDeclarationNode).TypeNode.TigerType.Type,
                        FieldAttributes.Public | FieldAttributes.Static);
                variableDeclarationNode.VariableInfo.FieldBuilder = newField;
            }

            foreach (var variableDeclarationNode in VariableDeclarationNodes)
                variableDeclarationNode.GenerateCode(generator);
        }
    }

    public class TypeDeclarationBlockNode : DeclarationBlockNode
    {
        private TypeDeclarationNode[] TypeDeclarationNodes => Children.Cast<TypeDeclarationNode>().ToArray();

        public TypeDeclarationBlockNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            TypeDeclarationNodes.ToList().ForEach(t => t.CheckSemantics(scope, report));

            //Check for circularity in non-record types
            var typeResolution = new TypeResolution(TypeDeclarationNodes, scope, report);

            for (int i = 0; i < TypeDeclarationNodes.Length; i++)
            {
                if (!(TypeDeclarationNodes[i] is RecordTypeDeclarationNode))
                    TypeDeclarationNodes[i].CheckBodySemantics(scope, report);
                if (TypeDeclarationNodes[i].IsOK)
                    typeResolution.CreateSets(i);
            }

            typeResolution.JoinSets();

            //Solve non-record types
            typeResolution.SolveTypes();

            //Solve record types
            foreach (var t in TypeDeclarationNodes.Where(x => x is RecordTypeDeclarationNode))
                t.CheckBodySemantics(scope, report);

            typeResolution.SolveRecords();

            IsOK = true;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            var recordDeclarations = TypeDeclarationNodes.Where(x => x is RecordTypeDeclarationNode).Cast<RecordTypeDeclarationNode>();
            foreach (var recordDeclaration in recordDeclarations)
                recordDeclaration.RecordTigerType.Type =
                    ProgramNode.Module.DefineType(ProgramNode.Module.ScopeName + "." + recordDeclaration.RecordTigerType.StaticName);
            foreach (var typeDeclarationNode in TypeDeclarationNodes)
                typeDeclarationNode.GenerateCode(generator);
        }
    }

    public class FunctionDeclarationBlockNode : DeclarationBlockNode
    {
        private MethodDeclarationNode[] FunctionDeclarationNodes => Children.Cast<MethodDeclarationNode>().ToArray();

        public FunctionDeclarationBlockNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Cheking children (function body semantics not checked)
            FunctionDeclarationNodes.ToList().ForEach(f => f.CheckSemantics(scope, report));

            //Cheking children function body (notice all functions in block are already declared)
            FunctionDeclarationNodes.ToList().ForEach(f => f.CheckBodySemantics(scope, report));

            foreach (var f in FunctionDeclarationNodes.Where(f => !f.IsOK && f.ReturnTypeNode == null))
            {
                scope.RemoveVariable(f.IdNode.Name);
                IsOK = false;
            }

            if (FunctionDeclarationNodes.All(f => f.IsOK))
                IsOK = true;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            foreach (var t in FunctionDeclarationNodes)
            {
                var parametersTypes = new Type[t.ParameterDeclarationNodes.Length]; //extrae los tipos de los parametros
                for (var i = 0; i < t.ParameterDeclarationNodes.Length; i++)
                    parametersTypes[i] = t.ParameterDeclarationNodes[i].TigerType.Type;
                var newMethod = ProgramNode.Program.DefineMethod(t.IdNode.Name,
                    MethodAttributes.Static | MethodAttributes.Public, t.FunctionBodyNode.TigerType.Type, parametersTypes);
                t.FunctionInfo.MethodBuilder = newMethod; //setea FunctionInfo
            }

            foreach (var t in FunctionDeclarationNodes)
            {
                var newGenerator = t.FunctionInfo.MethodBuilder.GetILGenerator();
                t.GenerateCode(newGenerator);
            }
        }
    }    
}
