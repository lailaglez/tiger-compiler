using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace Complements
{
    public class TigerScope
    {
        private static string[] BuiltInTypesNames;
        private static string[] BuiltInFunctionsNames;
        private static int ClassCounter;

        private readonly Dictionary<string, TigerType> TypeDictionary = new Dictionary<string, TigerType>();
        private readonly Dictionary<string, Info> FunctionVariableDictionary = new Dictionary<string, Info>();

        private readonly TigerScope Parent;
        public readonly string Name;

        private bool IsRoot => Parent == null;

        #region Constructors
        public TigerScope(TigerScope parent, string name)
        {
            Parent = parent;
            Name = name + "Class" + ClassCounter++;
        }

        public TigerScope(TypeBuilder typeBuilder)
        {
            Name = "MainClass";
            ClassCounter = 0;

            TypeDictionary.Add(TigerType.Int.Name, TigerType.Int);
            TypeDictionary.Add(TigerType.String.Name, TigerType.String);

            BuiltInTypesNames = TypeDictionary.Keys.ToArray();

            FunctionVariableDictionary.Add("print",
                new FunctionInfo("print", Name + "_print", TigerType.Void, new[] {TigerType.String}, this, BuiltInMethodBuilders.Print(typeBuilder)));
            FunctionVariableDictionary.Add("printi",
                new FunctionInfo("printi", Name + "_printi", TigerType.Void, new[] {TigerType.Int}, this, BuiltInMethodBuilders.PrintI(typeBuilder)));
            FunctionVariableDictionary.Add("printline",
                new FunctionInfo("printline", Name + "_printline", TigerType.Void, new[] {TigerType.String}, this, BuiltInMethodBuilders.PrintLine(typeBuilder)));
            FunctionVariableDictionary.Add("printiline",
                new FunctionInfo("printiline", Name + "_printiline", TigerType.Void, new[] {TigerType.Int}, this, BuiltInMethodBuilders.PrintILine(typeBuilder)));
            FunctionVariableDictionary.Add("getline",
                new FunctionInfo("getline", Name + "_getline", TigerType.String, new TigerType[0], this, BuiltInMethodBuilders.GetLine(typeBuilder)));
            FunctionVariableDictionary.Add("ord",
                new FunctionInfo("ord", Name + "_ord", TigerType.Int, new[] {TigerType.String}, this, BuiltInMethodBuilders.Ord(typeBuilder)));
            FunctionVariableDictionary.Add("chr",
                new FunctionInfo("chr", Name + "_chr", TigerType.String, new[] {TigerType.Int}, this, BuiltInMethodBuilders.Chr(typeBuilder)));
            FunctionVariableDictionary.Add("size",
                new FunctionInfo("size", Name + "_size", TigerType.Int, new[] {TigerType.String}, this, BuiltInMethodBuilders.Size(typeBuilder)));
            FunctionVariableDictionary.Add("substring",
                new FunctionInfo("substring", Name + "_substring", TigerType.String, new[] {TigerType.String, TigerType.Int, TigerType.Int}, this, BuiltInMethodBuilders.Substring(typeBuilder)));
            FunctionVariableDictionary.Add("concat",
                new FunctionInfo("concat", Name + "_concat", TigerType.String, new[] {TigerType.String, TigerType.String}, this, BuiltInMethodBuilders.Concat(typeBuilder)));
            FunctionVariableDictionary.Add("not",
                new FunctionInfo("not", Name + "_not", TigerType.Int, new[] {TigerType.Int}, this, BuiltInMethodBuilders.Not(typeBuilder)));
            FunctionVariableDictionary.Add("exit",
                new FunctionInfo("exit", Name + "_exit", TigerType.Void, new[] { TigerType.Int }, this, BuiltInMethodBuilders.Exit(typeBuilder)));

            BuiltInFunctionsNames = FunctionVariableDictionary.Keys.ToArray();
        }
        #endregion

        #region Types
        public void DefineType(string name, TigerType tigerType)
        {
            if (!TypeNameAvailable(name))
                throw new Exception("Type name not available");
            TypeDictionary.Add(name, tigerType);
        }

        public void DefineIncompleteType(string name, TigerType tigerType = null)
        {
            if (!TypeNameAvailable(name))
                throw new Exception("Type name not available");
            TypeDictionary.Add(name, tigerType ?? TigerType.Nil);
        }

        public void CompleteType(string name, TigerType tigerType)
        {
            TypeDictionary[name] = tigerType;
        }

        public void RemoveType(string name)
        {
            TypeDictionary.Remove(name);
        }
        
        public TigerType FindType(string name)
        {
            return TypeDictionary.ContainsKey(name)
                ? TypeDictionary[name]
                : Parent?.FindType(name);
        }

        public bool TypeNameAvailable(string name)
        {
            return !BuiltInTypesNames.Contains(name) && !TypeDictionary.ContainsKey(name);
        }

        public bool ValidReturnType(TigerType tigerType)
        {
            if (IsRoot)
                return false;
            return TigerType.AreOfSameType(tigerType, TigerType.Void) ||
                   TigerType.AreOfSameType(tigerType, TigerType.Nil) ||
                   (Parent.ExistsType(tigerType.Name) && !DefinesType(tigerType.Name));
        }

        private bool ExistsType(string name)
        {
            return FindType(name) != null;
        }

        private bool DefinesType(string name)
        {
            return TypeDictionary.ContainsKey(name);
        }
        #endregion

        #region Functions
        public FunctionInfo DefineFunction(string name, TigerType returnType, TigerType[] parameters, TigerScope containingScope, MethodBuilder methodBuilder = null)
        {
            if (!FunctionNameAvailable(name))
                throw new Exception("A variable or function with this name already exists");
            FunctionInfo functionInfo = new FunctionInfo(name, Name + "_" + name, returnType, parameters, containingScope, methodBuilder);
            FunctionVariableDictionary.Add(name, functionInfo);
            return functionInfo;
        }

        public void RemoveFunction(string name)
        {
            FunctionVariableDictionary.Remove(name);
        }

        public bool FunctionNameAvailable(string name)
        {
            return !BuiltInFunctionsNames.Contains(name) && !FunctionVariableDictionary.ContainsKey(name);
        }

        public bool ExistsFunction(string name)
        {
            return FindFunction(name) != null;
        }

        public FunctionInfo FindFunction(string name)
        {
            if (FunctionVariableDictionary.ContainsKey(name))
                return FunctionVariableDictionary[name] as FunctionInfo;
            return Parent?.FindFunction(name);
        }

        #endregion

        #region Variables
        public VariableInfo DefineVariable(string name, TigerType tigerType, TigerScope containingScope, bool assignable = true)
        {
            if (FunctionVariableDictionary.ContainsKey(name))
                throw new Exception("A variable or function with this name already exists");
            VariableInfo variableInfo = new VariableInfo(name, Name + "_" + name, tigerType, containingScope, assignable);
            FunctionVariableDictionary.Add(name, variableInfo);
            return variableInfo;
        }

        public void RemoveVariable(string name)
        {
            FunctionVariableDictionary.Remove(name);
        }

        public bool ExistsVariable(string name)
        {
            return FindVariable(name) != null;
        }

        public VariableInfo FindVariable(string name)
        {
            if (FunctionVariableDictionary.ContainsKey(name))
                return FunctionVariableDictionary[name] as VariableInfo;
            return Parent?.FindVariable(name);
        }

        public bool VariableNameAvailable(string name)
        {
            return FunctionNameAvailable(name);
        }
        #endregion

        public List<FieldBuilder> FieldsToFunctionDeclaration(FunctionInfo functionInfo)
        {
            var list = new List<FieldBuilder>();
            var current = this;

            while (!current.IsRoot)
            {
                list.AddRange(current.FunctionVariableDictionary.Where(x => x.Value is VariableInfo).Select(x => ((VariableInfo)x.Value).FieldBuilder));
                if (current.Name == functionInfo.ContainingScope.Name)
                    return list;
                current = current.Parent;
            }

            return new List<FieldBuilder>();
        }
    }
}