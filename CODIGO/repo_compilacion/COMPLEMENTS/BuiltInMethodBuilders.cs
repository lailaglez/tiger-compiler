using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Complements
{
    static class BuiltInMethodBuilders
    {
        public static MethodBuilder Print(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("print", MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                new[] { typeof(string) });
            var generator = methodBuilder.GetILGenerator();
            MethodInfo write = typeof(Console).GetMethod("Write", new[] { typeof(string) });
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, write);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder PrintI(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("printi", MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                new[] { typeof(int) });
            var generator = methodBuilder.GetILGenerator();
            MethodInfo write = typeof(Console).GetMethod("Write", new[] { typeof(int) });
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, write);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder PrintLine(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("printline", MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                new[] { typeof(string) });
            var generator = methodBuilder.GetILGenerator();
            MethodInfo writeLine = typeof(Console).GetMethod("WriteLine", new[] { typeof(string) });
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, writeLine);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder PrintILine(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("printiline", MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                new[] { typeof(int) });
            var generator = methodBuilder.GetILGenerator();
            MethodInfo writeLine = typeof(Console).GetMethod("WriteLine", new[] { typeof(int) });
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, writeLine);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder GetLine(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("getline", MethodAttributes.Public | MethodAttributes.Static, typeof(string),
                Type.EmptyTypes);
            var generator = methodBuilder.GetILGenerator();
            MethodInfo readLine = typeof(Console).GetMethod("ReadLine", Type.EmptyTypes);
            generator.Emit(OpCodes.Call, readLine);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder Ord(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("ord", MethodAttributes.Public | MethodAttributes.Static, typeof(int),
                new[] { typeof(string) });
            var generator = methodBuilder.GetILGenerator();
            var emptyLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldlen);  //pon en la pila a long del string
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Beq, emptyLabel);  //si es 0 retorna -1
            generator.Emit(OpCodes.Ldarg_0);  //si no, quedate con el primer char y halla su valor en entero
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ldelem, typeof(char));  //pon en la pila el primer char
            generator.Emit(OpCodes.Conv_I4);  //conviertelo a int
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(emptyLabel);
            generator.Emit(OpCodes.Ldc_I4, -1);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder Chr(TypeBuilder typeBuilder)
        {
            MethodInfo toString = typeof(object).GetMethod("ToString", Type.EmptyTypes);
            var methodBuilder = typeBuilder.DefineMethod("chr", MethodAttributes.Public | MethodAttributes.Static, typeof(string),
                new[] { typeof(int) });
            var generator = methodBuilder.GetILGenerator();
            var badLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Blt, badLabel); //si es < 0, lanza excepción
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4, 127);
            generator.Emit(OpCodes.Bgt, badLabel);  //si es > 127, también
            generator.Emit(OpCodes.Ldarg_0);  //pon el número en la pila
            generator.Emit(OpCodes.Box, typeof(char));
            generator.Emit(OpCodes.Callvirt, toString);
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(badLabel);
            generator.Emit(OpCodes.Ldstr, "Integer must be greater than or equal to 0 and less than or equal to 127.");
            generator.Emit(OpCodes.Newobj, typeof(ArgumentOutOfRangeException).GetConstructor(new[] { typeof(string) }));
            generator.Emit(OpCodes.Throw);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder Size(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("size", MethodAttributes.Public | MethodAttributes.Static, typeof(int),
                new[] { typeof(string) });
            var generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldlen);  //pon en la pila a long del string
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder Substring(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("substring", MethodAttributes.Public | MethodAttributes.Static, typeof(string),
                new[] { typeof(string), typeof(int), typeof(int) });
            var substring = typeof(string).GetMethod("Substring", new[] { typeof(int), typeof(int) });
            var generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Ldarg_2);
            generator.Emit(OpCodes.Call, substring);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder Concat(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("concat", MethodAttributes.Public | MethodAttributes.Static, typeof(string),
                new[] { typeof(string), typeof(string) });
            var concat = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });
            var generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldarg_1);
            generator.Emit(OpCodes.Call, concat);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder Not(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("not", MethodAttributes.Public | MethodAttributes.Static, typeof(int),
                new[] { typeof(int) });
            var generator = methodBuilder.GetILGenerator();
            var zeroLabel = generator.DefineLabel();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Beq, zeroLabel);  //si es 0 retorna -1
            generator.Emit(OpCodes.Ldc_I4_0);
            generator.Emit(OpCodes.Ret);
            generator.MarkLabel(zeroLabel);
            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }

        public static MethodBuilder Exit(TypeBuilder typeBuilder)
        {
            var methodBuilder = typeBuilder.DefineMethod("exit", MethodAttributes.Public | MethodAttributes.Static, typeof(void),
                new[] { typeof(int) });
            var exit = typeof(Environment).GetMethod("Exit", new[] { typeof(int) });
            var generator = methodBuilder.GetILGenerator();
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Call, exit);
            generator.Emit(OpCodes.Ret);
            return methodBuilder;
        }
    }
}
