using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using Errors;
using Semantics;

namespace AST
{
    public class ArrayInitNode : ExpressionNode
    {
        private TypeAccessNode TypeNode => Children[0] as TypeAccessNode;
        private ExpressionNode LengthExpressionNode => Children[1] as ExpressionNode;
        private ExpressionNode InitialValueExpressionNode => Children[2] as ExpressionNode;

        public ArrayInitNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Checking Children
            TypeNode.CheckSemantics(scope, report);
            LengthExpressionNode.CheckSemantics(scope, report);
            InitialValueExpressionNode.CheckSemantics(scope, report);

            if (!TypeNode.IsOK || !LengthExpressionNode.IsOK || !InitialValueExpressionNode.IsOK)
                return;

            var type = TypeNode.TigerType as ArrayType;

            if (type == null)
            {
                report.AddError(SemanticErrors.NonArrayType(TypeNode, TypeNode.TigerType));
                return;
            }

            TigerType = TypeNode.TigerType;

            //Checking LengthExpression type
            if (!TigerType.AreCompatible(LengthExpressionNode.TigerType, TigerType.Int))
                report.AddError(SemanticErrors.NonIntegerArrayLength(LengthExpressionNode,
                    LengthExpressionNode.TigerType));

            //Checking InitialValueExpression
            if (!type.ContentType.Assignable(InitialValueExpressionNode.TigerType))
                report.AddError(SemanticErrors.ArrayInitialValueInvalidType(InitialValueExpressionNode,
                    InitialValueExpressionNode.TigerType, TypeNode.TigerType));
        }

        public override void GenerateCode(ILGenerator generator)
        {
            var newArray = generator.DeclareLocal(TigerType.Type);
            var length = generator.DeclareLocal(typeof (int));
            var index = generator.DeclareLocal(typeof (int));
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Stloc, index);               //inicializa en 0 la variable para ir llenando el arreglo
            LengthExpressionNode.GenerateCode(generator);
            generator.Emit(OpCodes.Stloc, length);              //guarda el tamaño del arreglo en una var  para iterar despues
            generator.Emit(OpCodes.Ldloc, length); 
            generator.Emit(OpCodes.Newarr, ((ArrayType)TigerType).ContentType.Type); //crea el arreglo nuevo
            generator.Emit(OpCodes.Stloc, newArray);            //guardalo en una variable
            var loopLabel = generator.DefineLabel();
            var endLabel = generator.DefineLabel();
            generator.MarkLabel(loopLabel);
            generator.Emit(OpCodes.Ldloc, length);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Beq, endLabel);              //si length es igual a 0 sal del ciclo
            generator.Emit(OpCodes.Ldloc, newArray);            //pon el array en la pila
            generator.Emit(OpCodes.Ldloc, index);               //pon el indice
            InitialValueExpressionNode.GenerateCode(generator); //pon el resultado de  evaluar la expresion inicial
            generator.Emit(OpCodes.Stelem, ((ArrayType)TigerType).ContentType.Type); //array[index] = initialExpression
            generator.Emit(OpCodes.Ldloc, length);              //length--; index++; 
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Sub);
            generator.Emit(OpCodes.Stloc, length);
            generator.Emit(OpCodes.Ldloc, index);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Add);
            generator.Emit(OpCodes.Stloc, index);
            generator.Emit(OpCodes.Br, loopLabel);              //salta hasta loopLabel
            generator.MarkLabel(endLabel);
            generator.Emit(OpCodes.Ldloc, newArray);
        }
    }
}
