using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Attempt6.Syntax;

namespace Attempt6.Compiling {
    public class Compiler : ISyntaxVisitor {
        private LLVMContextRef context;
        private IRBuilder builder;

        private FunctionCallCompiler funcCompiler;

        // Stores values
        private readonly Dictionary<VariableLocation, LLVMValueRef> readOnlyVariables = new Dictionary<VariableLocation, LLVMValueRef>();

        // Stores pointers to values
        private readonly Dictionary<VariableLocation, LLVMValueRef> mutableVariables = new Dictionary<VariableLocation, LLVMValueRef>();

        private readonly Stack<LLVMValueRef> values = new Stack<LLVMValueRef>();
        private readonly ISyntax tree;

        public Compiler(ISyntax tree) {
            this.context = LLVM.ContextCreate();
            this.builder = new IRBuilder(this.context);
            this.funcCompiler = new FunctionCallCompiler(this.builder);
            this.tree = tree;
        }

        public string Compile() {
            var mainType = LLVM.FunctionType(PrimitiveType.Int32Type.LlvmType, new LLVMTypeRef[0], false);
            var mainModule = LLVM.ModuleCreateWithName("Program");
            var mainFunc = LLVM.AddFunction(mainModule, "main", mainType);

            var mainFuncBasicBlock = LLVM.AppendBasicBlock(mainFunc, "entry");
            this.builder.PositionBuilderAtEnd(mainFuncBasicBlock);

            this.tree.Accept(this);
            var value = this.values.Pop();
            var block = LLVM.AppendBasicBlock(mainFunc, "exit");

            this.builder.CreateBr(block);
            this.builder.PositionBuilderAtEnd(block);

            this.builder.CreateRet(value);

            LLVM.VerifyFunction(mainFunc, LLVMVerifierFailureAction.LLVMPrintMessageAction);

            LLVMPassManagerBuilderRef passBuilder = LLVM.PassManagerBuilderCreate();
            LLVM.PassManagerBuilderSetOptLevel(passBuilder, 3);

            LLVMPassManagerRef pass = LLVM.CreatePassManager();
            LLVM.PassManagerBuilderPopulateModulePassManager(passBuilder, pass);

            //LLVM.RunPassManager(pass, mainModule);
            LLVM.DumpModule(mainModule);

            return "worked";
        }
       
        public void Visit(Int32Literal literal) {
            var value = LLVM.ConstInt(PrimitiveType.Int32Type.LlvmType, (ulong)literal.Value, false);
            this.values.Push(value);
        }

        public void Visit(FunctionCallExpression syntax) {
            List<LLVMValueRef> arguments = new List<LLVMValueRef>();

            foreach (var arg in syntax.Arguments) {
                arg.Accept(this);
                arguments.Add(this.values.Pop());
            }

            this.values.Push(this.funcCompiler.Compile(syntax.Target, arguments));
        }

        public void Visit(VariableAssignment syntax) {
            syntax.AssignExpression.Accept(this);
            var assign = this.values.Pop();

            if (syntax.IsReadOnly) {
                assign.SetValueName(syntax.Variable.Name);

                this.readOnlyVariables.Add(syntax.Variable, assign);
                syntax.ScopeExpression.Accept(this);
                this.readOnlyVariables.Remove(syntax.Variable);
            }
            else if (this.mutableVariables.TryGetValue(syntax.Variable, out var address)) {
                this.builder.CreateStore(assign, address);
                syntax.ScopeExpression.Accept(this);
            }
            else {
                // Get the variable address space
                address = this.builder.CreateAlloca(syntax.ExpressionType.LlvmType, syntax.Variable.Name + "_ptr");

                // Store the assignment value in the variable
                this.builder.CreateStore(assign, address);

                this.mutableVariables.Add(syntax.Variable, address);
                syntax.ScopeExpression.Accept(this);
                this.mutableVariables.Remove(syntax.Variable);
            }
        }

        public void Visit(VariableLocation syntax) {
            if (this.readOnlyVariables.TryGetValue(syntax, out var value)) {
                this.values.Push(value);
            }
            else {
                this.values.Push(this.builder.CreateLoad(this.mutableVariables[syntax], syntax.Name));
            }
        }

        public void Visit(Statement syntax) {
            syntax.StatementExpression.Accept(this);
            this.values.Pop();

            syntax.ReturnExpression.Accept(this);
        }
    }

    public class FunctionCallCompiler : IFunctionSyntaxVisitor {
        private IRBuilder builder;

        private IFunctionSyntax target;
        private IReadOnlyList<LLVMValueRef> arguments;
        private LLVMValueRef value;

        public FunctionCallCompiler(IRBuilder builder) {
            this.builder = builder;
        }

        public LLVMValueRef Compile(IFunctionSyntax func, IReadOnlyList<LLVMValueRef> args) {
            this.target = func;
            this.arguments = args;

            if (this.arguments.Count != func.ExpressionType.ParameterTypes.Count) {
                throw new Exception();
            }

            this.target.Accept(this);
            return this.value;
        }

        public void Visit(IntrinsicFunction func) {
            var left = this.arguments[0];
            var right = this.arguments[1];

            switch (func.Kind) {
                case IntrinsicFunctionKind.AddInt32:
                    this.value = this.builder.CreateAdd(left, right, "");
                    break;
                case IntrinsicFunctionKind.SubtractInt32:
                    this.value = this.builder.CreateSub(left, right, "");
                    break;
                case IntrinsicFunctionKind.MultiplyInt32:
                    this.value = this.builder.CreateMul(left, right, "");
                    break;
                case IntrinsicFunctionKind.DivideInt32:
                    this.value = this.builder.CreateSDiv(left, right, "");
                    break;
                default:
                    throw new Exception();
            }
        }
    }
}