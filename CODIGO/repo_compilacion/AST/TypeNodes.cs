using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using Semantics;
using Errors;

namespace AST
{
    public abstract class TypeDeclarationNode : DeclarationNode
    {
        public IdNode IdNode => Children[0] as IdNode;

        protected TypeDeclarationNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            IdNode.CheckSemantics(scope, report);

            if (!IdNode.IsOK)
                return;

            //Check type name existence
            if (!scope.TypeNameAvailable(IdNode.Name))
                report.AddError(SemanticErrors.TypeNameAlreadyInUse(IdNode, IdNode.Name));
            else
                scope.DefineIncompleteType(IdNode.Name);

            IsOK = true;
        }

        public abstract void CheckBodySemantics(TigerScope scope, Report report);

        public override void GenerateCode(ILGenerator generator) { }
    }

    public class SimpleTypeDeclarationNode : TypeDeclarationNode
    {
        public TypeAccessNode TypeAccessNode => Children[1] as TypeAccessNode;

        public SimpleTypeDeclarationNode(IToken payload = null) : base(payload) { }
        
        public override void CheckBodySemantics(TigerScope scope, Report report)
        {
            TypeAccessNode.CheckSemantics(scope, report);

            //If type was not added to scope (if CheckSemantics failed) return
            if (!TypeAccessNode.IsOK || !IsOK)
            {
                IsOK = false;
                return;
            }

            IsOK = true;
        }
    }

    public class ArrayTypeDeclarationNode : TypeDeclarationNode
    {
        public TypeAccessNode TypeAccessNode => Children[1] as TypeAccessNode;

        public ArrayTypeDeclarationNode(IToken payload = null) : base(payload) { }

        public override void CheckBodySemantics(TigerScope scope, Report report)
        {
            TypeAccessNode.CheckSemantics(scope, report);

            //If type was not added to scope (if CheckSemantics failed) return
            if (!TypeAccessNode.IsOK || !IsOK)
            {
                IsOK = false;
                return;
            }

            scope.CompleteType(IdNode.Name, new ArrayType(IdNode.Name, TypeAccessNode.TigerType));

            IsOK = true;
        }
    }

    public class RecordTypeDeclarationNode : TypeDeclarationNode
    {
        public FieldDeclarationNode[] FieldDeclarationNodes => Children.Skip(1).Cast<FieldDeclarationNode>().ToArray();

        public RecordTypeDeclarationNode(IToken payload = null) : base(payload) { }

        public RecordType RecordTigerType;

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            IdNode.CheckSemantics(scope, report);

            if (!IdNode.IsOK)
                return;

            //Check type name existence
            if (!scope.TypeNameAvailable(IdNode.Name))
                report.AddError(SemanticErrors.TypeNameAlreadyInUse(IdNode, IdNode.Name));
            else
            {
                RecordTigerType = new RecordType(IdNode.Name, ContainingScope.Name, null);
                scope.DefineIncompleteType(IdNode.Name, RecordTigerType);
            }

            IsOK = true;
        }

        public override void CheckBodySemantics(TigerScope scope, Report report)
        {
            //Check children
            IdNode.CheckSemantics(scope, report);
            FieldDeclarationNodes.ToList().ForEach(f => f.CheckSemantics(scope, report));

            var usedNames = new List<string>();

            foreach (var f in FieldDeclarationNodes)
            {
                if (usedNames.Contains(f.IdNode.Name))
                    report.AddError(SemanticErrors.DuplicateFieldDeclaration(f, IdNode.Name, f.IdNode.Name));
                usedNames.Add(f.IdNode.Name);
            }

            //If type was not added to scope (if CheckSemantics failed) return
            if (!IsOK || FieldDeclarationNodes.Any(f => !f.IsOK))
            {
                IsOK = false;
                return;
            }

            RecordTigerType = scope.FindType(IdNode.Name) as RecordType;

            IsOK = true;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            var classBuilder = (TypeBuilder)RecordTigerType.Type;
            foreach (var fieldDeclarationNode in FieldDeclarationNodes)
                classBuilder.DefineField(fieldDeclarationNode.IdNode.Name, fieldDeclarationNode.TigerType.Type,
                    FieldAttributes.Public);

            classBuilder.CreateType();
        }
    }

    public class FieldDeclarationNode : ExpressionNode
    {
        public IdNode IdNode => Children[0] as IdNode;
        public TypeAccessNode TypeNode => Children[1] as TypeAccessNode;

        public FieldDeclarationNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            IdNode.CheckSemantics(scope, report);
            TypeNode.CheckSemantics(scope, report);

            if (!IdNode.IsOK || !TypeNode.IsOK)
                return;

            TigerType = TypeNode.TigerType;
        }

        public override void GenerateCode(ILGenerator generator) { }
    }
}
