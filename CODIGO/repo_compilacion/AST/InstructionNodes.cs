using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using Semantics; using Errors;

namespace AST
{
    public abstract class InstructionNode : TigerNode
    {
        protected InstructionNode(IToken payload = null) : base(payload) { }
    }

    public abstract class ExpressionNode : InstructionNode
    {
        public override bool IsOK => !TigerType.AreCompatible(TigerType, TigerType.Error);

        public override TigerType TigerType { get; set; } = TigerType.Error;

        protected ExpressionNode(IToken payload = null) : base(payload) { }
    }

    public interface IBreakableNode
    {
        Label End { get; set; }
    }

    public class WhileNode : InstructionNode, IBreakableNode
    {
        private ExpressionNode Condition => Children[0] as ExpressionNode;
        private InstructionNode InstructionNode => Children[1] as InstructionNode;

        public Label End { get; set; }

        public WhileNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            Condition.CheckSemantics(scope, report);
            InstructionNode.CheckSemantics(scope, report);

            if (!Condition.IsOK || !InstructionNode.IsOK)
                return;

            IsOK = true;

            //Check children types
            if (!TigerType.AreCompatible(Condition.TigerType, TigerType.Int))
            {
                report.AddError(SemanticErrors.InvalidConditionType(Condition, Condition.TigerType));
                IsOK = false;
            }
            if (!TigerType.AreCompatible(InstructionNode.TigerType, TigerType.Void))
            {
                report.AddError(SemanticErrors.InvalidWhileBodyType(InstructionNode));
                IsOK = false;
            }
        }

        public override void GenerateCode(ILGenerator generator)
        {
            Label loopCondition = generator.DefineLabel();
            Label loopEnd = generator.DefineLabel();
            End = loopEnd;

            generator.MarkLabel(loopCondition);
            Condition.GenerateCode(generator);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Beq, loopEnd);
            InstructionNode.GenerateCode(generator);
            generator.Emit(OpCodes.Br, loopCondition);
            generator.MarkLabel(loopEnd);
        }
    }

    public class ForNode : InstructionNode, IBreakableNode
    {
        private IdNode IdNode => Children[0] as IdNode;
        private ExpressionNode FromExpression => Children[1] as ExpressionNode;
        private ExpressionNode ToExpression => Children[2] as ExpressionNode;
        private InstructionNode DoInstruction => Children[3] as InstructionNode;

        private VariableInfo VariableInfo;

        public Label End { get; set; }

        public ForNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            IdNode.CheckSemantics(scope, report);
            FromExpression.CheckSemantics(scope, report);
            ToExpression.CheckSemantics(scope, report);

            if (!IdNode.IsOK || !FromExpression.IsOK || !ToExpression.IsOK)
                return;

            //Check loop bounds type
            if (!TigerType.AreCompatible(FromExpression.TigerType, TigerType.Int) || !TigerType.AreCompatible(ToExpression.TigerType, TigerType.Int))
            {
                report.AddError(!TigerType.AreCompatible(FromExpression.TigerType, TigerType.Int)
                    ? SemanticErrors.InvalidForBoundType(FromExpression, FromExpression.TigerType)
                    : SemanticErrors.InvalidForBoundType(ToExpression, ToExpression.TigerType));
                return;
            }

            IsOK = true;
            
            //Define new scope
            TigerScope childScope = new TigerScope(scope, "For");
            VariableInfo = childScope.DefineVariable(IdNode.Name, TigerType.Int, ContainingScope, false);

            DoInstruction.CheckSemantics(childScope, report);

            if (!DoInstruction.IsOK)
                return;

            if (!TigerType.AreCompatible(DoInstruction.TigerType, TigerType.Void))
            {
                report.AddError(SemanticErrors.InvalidForBodyType(DoInstruction));
                IsOK = false;
            }
        }

        public override void GenerateCode(ILGenerator generator)
        {
            Label loopCondition = generator.DefineLabel();
            Label loopEnd = generator.DefineLabel();
            var from = ProgramNode.Program.DefineField(VariableInfo.StaticName, typeof (int),
                FieldAttributes.Public | FieldAttributes.Static);
            var to = generator.DeclareLocal(typeof (int));
            End = loopEnd;
            FromExpr ession.GenerateCode(generator);     //genera el codigo de la variable from
            generator.Emit(OpCodes.Stsfld, from);
            VariableInfo.FieldBuilder = from;
            ToExpression.GenerateCode(generator);       //genera el codigo de la variable to
            generator.Emit(OpCodes.Stloc, to);
            generator.MarkLabel(loopCondition);         //comienza el for
            generator.Emit(OpCodes.Ldsfld, from);       //guarda from y to en la pila
            generator.Emit(OpCodes.Ldloc, to);
            generator.Emit(OpCodes.Bgt, loopEnd);       //si from > to sal del for
            DoInstruction.GenerateCode(generator);      //si no genera la instruccion
            generator.Emit(OpCodes.Ldsfld, from); 
            generator.Emit(OpCodes.Ldc_I4_1); 
            generator.Emit(OpCodes.Add);  
            generator.Emit(OpCodes.Stsfld, from);       //sumale 1 a from
            generator.Emit(OpCodes.Br, loopCondition);  //vuelve a iterar
            generator.MarkLabel(loopEnd);               //fin del ciclo
        }
    }

    public class BreakNode : InstructionNode
    {
        private IBreakableNode Owner;

        public BreakNode(IToken payload) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            IsOK = true;
            
            //Check if break is in correct position
            var nodes = GetNodesToRoot().TakeWhile(x => !(x is MethodDeclarationNode)).ToList();
            Owner = (nodes.FirstOrDefault(x => x is IBreakableNode)) as IBreakableNode;

            if (Owner == null)
            {
                report.AddError(SemanticErrors.BreakInIncorrectPostion(this));
                return;
            }

            //Set containing InstructionSequenceNodes' types to void
            nodes.TakeWhile(x => !(x is IBreakableNode)).Where(n => n is InstructionSequenceNode).ToList()
                .ForEach(n => ((InstructionSequenceNode) n).TigerType = TigerType.Void);
        }

        public override void GenerateCode(ILGenerator generator)
        {
            generator.Emit(OpCodes.Br, Owner.End);
        }
    }

    public class AssignNode : InstructionNode
    {
        private LValueNode LValueNode => Children[0] as LValueNode;
        private ExpressionNode ExpressionNode => Children[1] as ExpressionNode;

        public AssignNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            LValueNode.CheckSemantics(scope, report);
            ExpressionNode.CheckSemantics(scope, report);

            if (!LValueNode.IsOK || !ExpressionNode.IsOK)
                return;

            //Check children types
            if (!LValueNode.TigerType.Assignable(ExpressionNode.TigerType))
                report.AddError(SemanticErrors.InvalidAssignType(ExpressionNode, ExpressionNode.TigerType, LValueNode.TigerType));

            var variableAccess = LValueNode as VariableAccessNode;
            if (variableAccess != null && !variableAccess.VariableInfo.Assignable)
                report.AddError(SemanticErrors.NonAssignableVariable(LValueNode, variableAccess.VariableInfo.Name));

            IsOK = true;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            ExpressionNode.GenerateCode(generator);
            LValueNode.GenerateAssignCode(generator);
        }
    }

    public abstract class DeclarationNode : InstructionNode
    {
        protected DeclarationNode(IToken payload = null) : base(payload) { }
    }

    public class EmptyNode : InstructionNode
    {
        public EmptyNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report) { }

        public override void GenerateCode(ILGenerator generator) { }
    }
}
