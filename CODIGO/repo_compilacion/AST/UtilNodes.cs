using System.Linq;
using System.Reflection.Emit;
using Antlr.Runtime;
using Complements;
using Errors;

namespace AST
{
    public abstract class UtilNode : TigerNode
    {
        protected UtilNode(IToken payload = null) : base(payload) { }
    }

    public class IdNode : UtilNode
    {
        public IdNode(IToken payload = null) : base(payload) { }

        public string Name => Text;

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            IsOK = true;
        }

        public override void GenerateCode(ILGenerator generator) { }
    }

    public class DeclarationListNode : UtilNode
    {
        private DeclarationBlockNode[] DeclarationBlockNodes => Children.Cast<DeclarationBlockNode>().ToArray();

        public DeclarationListNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            DeclarationBlockNodes.ToList().ForEach(d => d.CheckSemantics(scope, report));

            IsOK = DeclarationBlockNodes.All(d => d.IsOK);
        }

        public override void GenerateCode(ILGenerator generator)
        {
            foreach (var declBlock in DeclarationBlockNodes)
                declBlock.GenerateCode(generator);
        }
    }

    public class InstructionSequenceNode : ExpressionNode
    {
        private InstructionNode[] InstructionNodes => Children?.Cast<InstructionNode>().ToArray() ?? new InstructionNode[0];

        public InstructionSequenceNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            //Check children
            InstructionNodes.ToList().ForEach(i => i.CheckSemantics(scope, report));

            if (InstructionNodes.Any(e => !e.IsOK))
                return;
            //If type is not void (no break found inside) assign last instruction type
            if (!TigerType.AreCompatible(TigerType, TigerType.Void))
                TigerType = InstructionNodes.Length == 0 ? TigerType.Void : InstructionNodes.Last().TigerType;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            if(InstructionNodes.Length == 0)
                return;

            for (var i = 0; i < InstructionNodes.Length - 1; i++)
            {
                InstructionNodes[i].GenerateCode(generator);
                if(!TigerType.AreOfSameType(InstructionNodes[i].TigerType, TigerType.Void))
                    generator.Emit(OpCodes.Pop);
            }

            InstructionNodes.Last().GenerateCode(generator);
        }
    }
}
