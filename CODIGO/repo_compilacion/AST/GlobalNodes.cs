using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Complements;
using Semantics; using Errors;

namespace AST
{
    public abstract class TigerNode : CommonTree
    {
        public int Column => CharPositionInLine;
        public virtual bool IsOK { get; protected set; }

        protected TigerScope ContainingScope { get; set; }
        private bool IsRoot => Parent == null;

        public virtual TigerType TigerType
        {
            get { return TigerType.Void; }
            set { throw new NotImplementedException(); }
        }

        protected TigerNode(IToken payload = null) : base(payload) { }

        protected List<TigerNode> GetNodesToRoot()
        {
            var nodes = new List<TigerNode>();
            var current = this;
            while (!current.IsRoot)
            {
                current = (TigerNode)current.Parent;
                nodes.Add(current);
            }
            return nodes;
        }

        public abstract void CheckSemantics(TigerScope scope, Report report);

        public abstract void GenerateCode(ILGenerator generator);
    }

    public class ProgramNode : ExpressionNode
    {
        private InstructionNode InstructionNode => Children[0] as InstructionNode;

        public static ModuleBuilder Module;
        public static TypeBuilder Program;

        public ProgramNode(IToken payload = null) : base(payload) { }

        public override void CheckSemantics(TigerScope scope, Report report)
        {
            ContainingScope = scope;

            InstructionNode.CheckSemantics(scope, report);

            TigerType = InstructionNode.TigerType;

            IsOK = InstructionNode.IsOK;
        }

        public override void GenerateCode(ILGenerator generator)
        {
            InstructionNode.GenerateCode(generator);
        }
    }
}
