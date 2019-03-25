using System.Linq;
using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using Semantics; using Errors;

namespace AST
{
    public class LetNode : ExpressionNode
    {
        private DeclarationListNode DeclarationListNode => Children[0] as DeclarationListNode;
        private InstructionSequenceNode InstructionSequenceNode => Children[1] as InstructionSequenceNode;

        public LetNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Create new scope
            var childScope = new TigerScope(scope, "Let");

            //Check children
            DeclarationListNode.CheckSemantics(childScope, report);
            InstructionSequenceNode.CheckSemantics(childScope, report);

            if (!DeclarationListNode.IsOK || !InstructionSequenceNode.IsOK)
                return;

            TigerType = InstructionSequenceNode.TigerType;

            //Check return type

            if (!childScope.ValidReturnType(TigerType))
                report.AddError(SemanticErrors.InvalidReturnType(this, TigerType));
        }

        public override void GenerateCode(ILGenerator generator)
        {
            DeclarationListNode.GenerateCode(generator);
            InstructionSequenceNode.GenerateCode(generator);
        }
    }

    public class ParenthesesNode : ExpressionNode
    {
        private InstructionSequenceNode InstructionSequenceNode => Children?[0] as InstructionSequenceNode;

        public ParenthesesNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            if (InstructionSequenceNode != null)
            {
                InstructionSequenceNode.CheckSemantics(scope, report);

                if (!InstructionSequenceNode.IsOK)
                {
                    TigerType = TigerType.Error;
                    return;
                }

                TigerType = InstructionSequenceNode.TigerType;
            }
            else
                TigerType = TigerType.Void;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            InstructionSequenceNode.GenerateCode(generator);
        }
    }

    public class IfElseNode : ExpressionNode
    {
        private ExpressionNode Condition => Children[0] as ExpressionNode;
        private InstructionNode IfBlock => Children[1] as InstructionNode;
        private InstructionNode ElseBlock => Children[2] as InstructionNode;

        public IfElseNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Checking children
            Condition.CheckSemantics(scope, report);
            IfBlock.CheckSemantics(scope, report);
            ElseBlock.CheckSemantics(scope, report);

            if (!Condition.IsOK || !IfBlock.IsOK || !ElseBlock.IsOK)
                return;

            TigerType = IfBlock.TigerType;

            //Checking children types
            if (!TigerType.AreCompatible(Condition.TigerType, TigerType.Int))
                report.AddError(SemanticErrors.InvalidConditionType(Condition, Condition.TigerType));
            if (!TigerType.AreCompatible(IfBlock.TigerType, ElseBlock.TigerType))
                report.AddError(SemanticErrors.IncompatibleIfElseReturnType(ElseBlock, IfBlock.TigerType, ElseBlock.TigerType));
        }

        public override void GenerateCode(ILGenerator generator)
        {
            var endLabel = generator.DefineLabel();
            var falseLabel = generator.DefineLabel();
            Condition.GenerateCode(generator);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Beq, falseLabel);
            IfBlock.GenerateCode(generator);
            generator.Emit(OpCodes.Br, endLabel);
            generator.MarkLabel(falseLabel);
            ElseBlock.GenerateCode(generator);
            generator.MarkLabel(endLabel);
        }
    }

    public class IfNode : ExpressionNode
    {
        private ExpressionNode Condition => Children[0] as ExpressionNode;
        private InstructionNode IfBlock => Children[1] as InstructionNode;

        public IfNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            Condition.CheckSemantics(scope, report);
            IfBlock.CheckSemantics(scope, report);

            if (!Condition.IsOK && !IfBlock.IsOK)
                return;

            TigerType = TigerType.Void;

            //Check condition type
            if (!TigerType.AreCompatible(TigerType.Int, Condition.TigerType))
            {
                report.AddError(SemanticErrors.InvalidConditionType(Condition, Condition.TigerType));
                TigerType = TigerType.Error;
            }
            if (!TigerType.AreCompatible(TigerType.Void, IfBlock.TigerType))
            {
                report.AddError(SemanticErrors.InvalidIfBodyType(IfBlock));
                TigerType = TigerType.Error;
            }
        }

        public override void GenerateCode(ILGenerator generator)
        {
            var endLabel = generator.DefineLabel();
            Condition.GenerateCode(generator);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Beq, endLabel);
            IfBlock.GenerateCode(generator);
            generator.MarkLabel(endLabel);
        }
    }

    public class CallNode : ExpressionNode
    {
        private IdNode IdNode => Children[0] as IdNode;
        private ExpressionNode[] Arguments => Children.Skip(1).Cast<ExpressionNode>().ToArray();

        private FunctionInfo FunctionInfo { get; set; }

        public CallNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Checking children
            IdNode.CheckSemantics(scope, report);
            Arguments.ToList().ForEach(e => e.CheckSemantics(scope, report));

            if (!IdNode.IsOK || Arguments.Any(e => !e.IsOK))
                return;

            //Checking existence of function
            FunctionInfo = scope.FindFunction(IdNode.Name);

            if (FunctionInfo == null)
                report.AddError(SemanticErrors.NonExistentFunctionReference(IdNode, IdNode.Name));
            else
            {
                TigerType = FunctionInfo.ReturnType;

                //Checking parameters
                if (Arguments.Length != FunctionInfo.ParameterNumber)
                    report.AddError(SemanticErrors.WrongNumberOfParameters(IdNode, FunctionInfo.Name,
                        FunctionInfo.ParameterNumber, Arguments.Length));
                else
                    for (int i = 0; i < Arguments.Length; i++)
                        if (!FunctionInfo.Parameters[i].Assignable(Arguments[i].TigerType))
                            report.AddError(SemanticErrors.InvalidArgumentType(Arguments[i], Arguments[i].TigerType, FunctionInfo.Parameters[i]));
            }
        }

        public override void GenerateCode(ILGenerator generator)
        {
            var saveFields = ContainingScope.FieldsToFunctionDeclaration(FunctionInfo);
            foreach (var fieldBuilder in saveFields)
                generator.Emit(OpCodes.Ldsfld, fieldBuilder);
            foreach (var expressionNode in Arguments)
                expressionNode.GenerateCode(generator);
            var method = FunctionInfo.MethodBuilder;
            generator.Emit(OpCodes.Call, method);
            LocalBuilder methodResult = null;
            if (!TigerType.AreOfSameType(FunctionInfo.ReturnType, TigerType.Void))
            {
                methodResult = generator.DeclareLocal(FunctionInfo.ReturnType.Type);
                generator.Emit(OpCodes.Stloc, methodResult);
            }
            foreach (var t in saveFields)
                generator.Emit(OpCodes.Stsfld, t);
            if (!TigerType.AreOfSameType(FunctionInfo.ReturnType, TigerType.Void))
                generator.Emit(OpCodes.Ldloc, methodResult);
        }
    }

    public abstract class LValueNode : ExpressionNode
    {
        protected LValueNode(IToken payload = null) : base(payload) { }

        public abstract void GenerateAssignCode(ILGenerator generator);
    }
}