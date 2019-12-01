using LLVMSharp;
using System;
using System.Text;

namespace Attempt7 {
    public class InterpretationResult {
        public LanguageType ReturnType { get; }

        public ISymbol Result { get; }

        public Scope LastScope { get; }

        public InterpretationResult(ISymbol result, Scope scope, LanguageType type) {
            this.Result = result;
            this.LastScope = scope;
            this.ReturnType = type;
        }
    }

    public class CompilationResult {
        public LanguageType ReturnType { get; }

        public Scope LastScope { get; }

        public LLVMValueRef Result { get; }

        public CompilationResult(LLVMValueRef result, Scope scope, LanguageType type) {
            this.Result = result;
            this.LastScope = scope;
            this.ReturnType = type;
        }
    }

    public interface ISymbol {
        InterpretationResult Interpret(Scope scope);
        CompilationResult Compile(BuilderArgs compiler, Scope scope);
    }

    public interface IPrimitiveSymbol : ISymbol {
        object Value { get; }
    }

    public static class SymbolExtensions {
        public static InterpretationResult Interpret(this ISymbol symbol) {
            return symbol.Interpret(new Scope());
        }

        public static CompilationResult Compile(this ISymbol symbol) {
            return symbol.Compile(new BuilderArgs(), new Scope());
        }
    }

    public class BuilderArgs {
        public LLVMContextRef Context { get; }
        public IRBuilder Builder { get; }

        public BuilderArgs() {
            this.Context = LLVM.ContextCreate();
            this.Builder = new IRBuilder(this.Context);

            var mainType = LLVM.FunctionType(
                LLVM.IntType(32), 
                new LLVMTypeRef[0], 
                false
            );

            var mainModule = LLVM.ModuleCreateWithName("Program");
            var mainFunc = LLVM.AddFunction(mainModule, "main", mainType);

            var mainFuncBasicBlock = LLVM.AppendBasicBlock(mainFunc, "entry");
            this.Builder.PositionBuilderAtEnd(mainFuncBasicBlock);

            //this.tree.Accept(this);
            //var value = this.values.Pop();
            //var block = LLVM.AppendBasicBlock(mainFunc, "exit");

            //this.builder.CreateBr(block);
            //this.builder.PositionBuilderAtEnd(block);

            //this.builder.CreateRet(value);

            //LLVM.VerifyFunction(mainFunc, LLVMVerifierFailureAction.LLVMPrintMessageAction);

            //LLVMPassManagerBuilderRef passBuilder = LLVM.PassManagerBuilderCreate();
            //LLVM.PassManagerBuilderSetOptLevel(passBuilder, 3);

            //LLVMPassManagerRef pass = LLVM.CreatePassManager();
            //LLVM.PassManagerBuilderPopulateModulePassManager(passBuilder, pass);

            ////LLVM.RunPassManager(pass, mainModule);
            //LLVM.DumpModule(mainModule);
        }
    }
}