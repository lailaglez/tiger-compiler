using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using Antlr.Runtime;
using AST;
using Complements;
using Syntaxis;
using Errors;

namespace repo_compilacion
{
    static class Program
    {
        private static int Main(string[] args)
        {
            Console.WriteLine("Tiger Compiler version 1.0");
            Console.WriteLine(@"Copyright (C) 2015-2016 Laila González Fernández & Mario César Muñiz Jiménez");

            //Creating report, parser and lexer
            var arg = args[0];
            var report = new Report();
            var characters = new ANTLRFileStream(arg);
            var lexer = new TigerLexer(report, characters);
            var tokens = new CommonTokenStream(lexer);
            var parser = new TigerParser(report, tokens)
            {
                TraceDestination = Console.Out,
                TreeAdaptor = new TreeAdaptor()
            };

            //Syntactic analysis
            var ret = (ProgramNode)parser.program().Tree;

            if (!report.IsOk)
            {
                foreach (var message in report)
                    Console.WriteLine($"({message.Line}, {message.Column}): {message.Text}");
                return 1;
            }

            //Preparing assembly
            var name = Path.GetFileNameWithoutExtension(arg);
            var filename = name + ".exe";
            var assemblyName = new AssemblyName(name);
            var assembly = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
            var moduleBuilder = assembly.DefineDynamicModule(name, filename);
            var programType = moduleBuilder.DefineType(name + ".Program", TypeAttributes.Public);
            var mainMethod = programType.DefineMethod("Main", MethodAttributes.Static | MethodAttributes.Public, typeof(void), Type.EmptyTypes);
            var generator = mainMethod.GetILGenerator();

            assembly.SetEntryPoint(mainMethod);

            ProgramNode.Module = moduleBuilder;
            ProgramNode.Program = programType;

            //Semantic analysis
            ret.CheckSemantics(new TigerScope(programType), report);

            if (!report.IsOk)
            {
                foreach (var message in report)
                    Console.WriteLine($"({message.Line}, {message.Column}): {message.Text}");
                return 1;
            }

            generator.BeginExceptionBlock();
            //Code generation
            ret.GenerateCode(generator);

            if (!TigerType.AreOfSameType(ret.TigerType, TigerType.Void))
                generator.Emit(OpCodes.Pop);

            generator.BeginCatchBlock(typeof(Exception));
            generator.BeginScope();
            LocalBuilder exception = generator.DeclareLocal(typeof(Exception));
            MethodInfo writeLineSO = typeof(TextWriter).GetMethod("WriteLine", new Type[] { typeof(string), typeof(object) });
            MethodInfo writeLineS = typeof(TextWriter).GetMethod("WriteLine", new Type[] { typeof(string) });
            MethodInfo standardErrorOutput = typeof(Console).GetProperty("Error").GetGetMethod();
            generator.Emit(OpCodes.Stloc, exception);
            generator.Emit(OpCodes.Call, standardErrorOutput);
            generator.Emit(OpCodes.Ldstr, "Exception of type '{0}' was thrown.");
            generator.Emit(OpCodes.Ldloc, exception);
            generator.Emit(OpCodes.Callvirt, typeof(Exception).GetMethod("GetType", Type.EmptyTypes));
            generator.Emit(OpCodes.Callvirt, typeof(Type).GetProperty("Name").GetGetMethod());
            generator.Emit(OpCodes.Callvirt, writeLineSO);

            generator.Emit(OpCodes.Call, standardErrorOutput);
            generator.Emit(OpCodes.Ldloc, exception);
            generator.Emit(OpCodes.Callvirt, typeof(Exception).GetProperty("Message").GetGetMethod());
            generator.Emit(OpCodes.Callvirt, writeLineS);

            generator.EndScope();

            generator.Emit(OpCodes.Ldc_I4_1);
            generator.Emit(OpCodes.Call, typeof(Environment).GetMethod("Exit"));

            generator.EndExceptionBlock();

            generator.Emit(OpCodes.Ret);
            programType.CreateType();
            assembly.Save(filename);

            return 0;
        }
    }
}