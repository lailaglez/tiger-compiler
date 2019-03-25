using System;
using System.Linq;
using System.Reflection;
using Antlr.Runtime;
using Antlr.Runtime.Tree;
using Syntaxis;

namespace AST
{
    public class TreeAdaptor : CommonTreeAdaptor
    {
        public override object Create(IToken payload)
        {
            if (payload==null)
                return new EmptyNode();

            var fields = typeof(TigerParser).GetFields();

            foreach (var field in fields)
                if (field.IsStatic && (int)field.GetRawConstantValue() == payload.Type)
                {
                    var name = GetClass(field.Name);
                    Type type = Assembly.GetExecutingAssembly().GetType(name);
                    return Activator.CreateInstance(type, payload);
                }

            return new EmptyNode(payload);
        }

        private static string GetClass(string name)
        {
            var parts = name.Split('_');
            var className = parts.Aggregate("", (current, part) => current + (part[0] + part.Substring(1).ToLower()));
            return "AST." + className;
        }
    }
}
