using System.Reflection.Emit;

namespace Complements
{
    public class Info
    {
        public string Name;
        public string StaticName;
        public TigerScope ContainingScope;
    }

    public class VariableInfo:Info
    {
        public readonly TigerType TigerType;
        public readonly bool Assignable;
        public FieldBuilder FieldBuilder;

        public VariableInfo(string name, string staticName, TigerType tigerType, TigerScope containingScope, bool assignable)
        {
            Name = name;
            StaticName = staticName;
            TigerType = tigerType;
            Assignable = assignable;
            ContainingScope = containingScope;
        }
    }

    public class FunctionInfo : Info
    {
        public int ParameterNumber => Parameters.Length;

        public readonly TigerType ReturnType;
        public readonly TigerType[] Parameters;
        public MethodBuilder MethodBuilder;

        public FunctionInfo(string name, string staticName, TigerType returnType, TigerType[] tigerTypes, TigerScope containingScope, MethodBuilder methodBuilder)
        {
            Name = name;
            StaticName = staticName;
            ReturnType = returnType;
            Parameters = tigerTypes;
            ContainingScope = containingScope;
            MethodBuilder = methodBuilder;
        }
    }
}
