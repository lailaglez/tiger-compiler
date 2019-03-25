using System.Linq;
using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using Semantics;
using Errors;

namespace AST
{
    class VariableAccessNode : LValueNode
    {
        private IdNode IdNode => Children[0] as IdNode;

        public VariableAccessNode(IToken payload = null) : base(payload) { }

        public VariableInfo VariableInfo;

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            IdNode.CheckSemantics(scope, report);

            if (!IdNode.IsOK)
                return;

            //Check variable existence
            VariableInfo = scope.FindVariable(IdNode.Name);

            if (VariableInfo == null)
                report.AddError(SemanticErrors.NonExistentVariableReference(IdNode, IdNode.Name));
            else TigerType = VariableInfo.TigerType;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            generator.Emit(OpCodes.Ldsfld, VariableInfo.FieldBuilder);
        }

        public override void GenerateAssignCode(ILGenerator generator)
        {
            generator.Emit(OpCodes.Stsfld, VariableInfo.FieldBuilder);
        }
    }

    class ArrayAccessNode : LValueNode
    {
        private LValueNode LValueNode => Children[0] as LValueNode;
        private ExpressionNode IndexExpressionNode => Children[1] as ExpressionNode;

        public ArrayAccessNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            LValueNode.CheckSemantics(scope, report);
            IndexExpressionNode.CheckSemantics(scope, report);

            if (!LValueNode.IsOK || !IndexExpressionNode.IsOK)
                return;

            //Check children types
            if (!(LValueNode.TigerType is ArrayType))
            {
                report.AddError(SemanticErrors.NonArrayType(LValueNode, LValueNode.TigerType));
                return;
            }
            if (!TigerType.Int.Assignable(IndexExpressionNode.TigerType))
            {
                report.AddError(SemanticErrors.ArrayIndexerInvalidType(IndexExpressionNode, IndexExpressionNode.TigerType));
                return;
            }

            TigerType = ((ArrayType)LValueNode.TigerType).ContentType;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            LValueNode.GenerateCode(generator);
            IndexExpressionNode.GenerateCode(generator);
            generator.Emit(OpCodes.Ldelem, ((ArrayType)LValueNode.TigerType).ContentType.Type);
        }

        public override void GenerateAssignCode(ILGenerator generator)
        {
            var val = generator.DeclareLocal(((ArrayType)LValueNode.TigerType).ContentType.Type);
            generator.Emit(OpCodes.Stloc, val);             //guarda el valor que esta en el tope de la pila para guardarlo en el array despues
            LValueNode.GenerateCode(generator);
            IndexExpressionNode.GenerateCode(generator);    //pon el indice
            generator.Emit(OpCodes.Ldloc, val);             //pon el valor que habiamos guardado para asignar
            generator.Emit(OpCodes.Stelem, val.LocalType);  //asignalo
        }
    }

    public class RecordAccessNode : LValueNode
    {
        private LValueNode LValueNode => Children[0] as LValueNode;
        private IdNode AttributeNode => Children[1] as IdNode;

        public RecordAccessNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            LValueNode.CheckSemantics(scope, report);
            AttributeNode.CheckSemantics(scope, report);

            if (!LValueNode.IsOK || !AttributeNode.IsOK)
                return;

            //Check children types
            if (!(LValueNode.TigerType is RecordType))
                report.AddError(SemanticErrors.NonRecordType(LValueNode, LValueNode.TigerType));
            else
            {
                var field = ((RecordType)LValueNode.TigerType).RecordFields.FirstOrDefault(f => f.Name == AttributeNode.Name);
                if (field == null)
                    report.AddError(SemanticErrors.InvalidField(AttributeNode, AttributeNode.Name, LValueNode.TigerType.Name));
                else
                    TigerType = field.TigerType;
            }
        }

        public override void GenerateCode(ILGenerator generator)
        {
            var recordClass = ProgramNode.Module.GetType(ProgramNode.Module.ScopeName + "." + ((RecordType)(LValueNode.TigerType)).StaticName);
            var field = recordClass.GetField(AttributeNode.Name);
            LValueNode.GenerateCode(generator);     //pone en el tope de la pila el record al cual se quiere acceder
            generator.Emit(OpCodes.Ldfld, field);   //pon en el tope de la pila el valor del campo deseado
        }

        public override void GenerateAssignCode(ILGenerator generator)
        {
            var recordClass = ProgramNode.Module.GetType(ProgramNode.Module.ScopeName + "." + ((RecordType)(LValueNode.TigerType)).StaticName);
            var field = recordClass.GetField(AttributeNode.Name);
            var val = generator.DeclareLocal(field.FieldType);
            generator.Emit(OpCodes.Stloc, val);     //guarda el valor que se quiere asignar
            LValueNode.GenerateCode(generator);     //guarda en el tope de la pila la instancia
            generator.Emit(OpCodes.Ldloc, val);     //ahora guarda el valor que se quiere asignar en el tope de la pila
            generator.Emit(OpCodes.Stfld, field);   //asigna
        }
    }

    public class TypeAccessNode : LValueNode
    {
        public IdNode IdNode => Children[0] as IdNode;

        public TypeAccessNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check type existence
            var type = scope.FindType(IdNode.Name);

            //Checking existence of array
            if (type != null)
                TigerType = type is SimpleType ? ((SimpleType)type).ActualType : type;
            else
                report.AddError(SemanticErrors.NonExistentTypeReference(IdNode, IdNode.Name));
        }

        public override void GenerateCode(ILGenerator generator) { }

        public override void GenerateAssignCode(ILGenerator generator) { }
    }
}
