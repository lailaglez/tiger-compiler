using System;
using System.Linq;
using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using Semantics; using Errors;

namespace AST
{
    public class RecordInitNode : ExpressionNode
    {
        private TypeAccessNode TypeNode => Children[0] as TypeAccessNode;
        private RecordFieldInitNode[] RecordFieldInitNodes => Children.Skip(1).Cast<RecordFieldInitNode>().ToArray();

        public RecordInitNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            TypeNode.CheckSemantics(scope, report);
            RecordFieldInitNodes.ToList().ForEach(f => f.CheckSemantics(scope, report));

            if (!TypeNode.IsOK || RecordFieldInitNodes.Any(f => !f.IsOK))
                return;
            
            //Check children types
            if (!(TypeNode.TigerType is RecordType))
                report.AddError(SemanticErrors.NonRecordType(TypeNode, TypeNode.TigerType));
            else
            {
                TigerType = TypeNode.TigerType;

                var fields = ((RecordType) TypeNode.TigerType).RecordFields;
                //Check fields length
                if (fields.Length != RecordFieldInitNodes.Length)
                    report.AddError(SemanticErrors.WrongNumberOfFields(TypeNode, TypeNode.TigerType.Name, fields.Length, RecordFieldInitNodes.Length));
                //Check field names and types
                for (int i = 0; i < Math.Min(fields.Length, RecordFieldInitNodes.Length); i++)
                {
                    if (fields[i].Name != RecordFieldInitNodes[i].IdNode.Name)
                        report.AddError(SemanticErrors.WrongFieldPosition(RecordFieldInitNodes[i].IdNode, fields[i].Name, RecordFieldInitNodes[i].IdNode.Name));
                    if (!fields[i].TigerType.Assignable(RecordFieldInitNodes[i].TigerType))
                        report.AddError(SemanticErrors.InvalidFieldType(RecordFieldInitNodes[i].IdNode, fields[i].TigerType, RecordFieldInitNodes[i].TigerType));
                }
            }
        }

        public override void GenerateCode(ILGenerator generator)
        {
            //var recordClass = ProgramNode.Module.GetType(ProgramNode.Module.ScopeName + "." + TypeNode.IdNode.Name);  //busca con reflection la clase deseada
            var recordClass = TypeNode.TigerType.Type;
            var constructor = recordClass.GetConstructor(System.Type.EmptyTypes);  //obten el constructor por defecto de dicha clase
            var instance = generator.DeclareLocal(recordClass);  //crea una variable para guardar la instancia que sera creada
            generator.Emit(OpCodes.Newobj, constructor);  //crea una nueva instancia de la clase
            generator.Emit(OpCodes.Stloc, instance);  //guardala en la variable que se creo
            foreach (var recordFieldInitNode in RecordFieldInitNodes)
            {
                var field = recordClass.GetField(recordFieldInitNode.IdNode.Name);  //obten de la clase el campo que queremos setear
                generator.Emit(OpCodes.Ldloc, instance);  //pon la instancia en la pila
                recordFieldInitNode.ExpressionNode.GenerateCode(generator);  //pon el nuevo valor en la pila
                generator.Emit(OpCodes.Stfld, field);  //asignale el valor deseado al campo field
            }
            generator.Emit(OpCodes.Ldloc, instance);
        }
    }

    public class RecordFieldInitNode : ExpressionNode
    {
        public IdNode IdNode => Children[0] as IdNode;
        public ExpressionNode ExpressionNode => Children[1] as ExpressionNode;

        public RecordFieldInitNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            IdNode.CheckSemantics(scope, report);
            ExpressionNode.CheckSemantics(scope, report);

            if (!IdNode.IsOK || !ExpressionNode.IsOK)
                return;

            TigerType = ExpressionNode.TigerType;
        }

        public override void GenerateCode(ILGenerator generator) { }
    }
}