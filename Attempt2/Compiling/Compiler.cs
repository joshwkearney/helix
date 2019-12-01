using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLVMSharp;
using Attempt2.Parsing;

namespace Attempt2.Compiling {
    public class Compiler : ISyntaxVisitor {
        private LLVMContextRef context;
        private IRBuilder builder;

        private readonly Stack<ISymbol> values = new Stack<ISymbol>();
        private readonly Stack<Scope> scopes = new Stack<Scope>();
        private readonly IAST tree;

        private readonly PrimitiveTypeSymbol int32TypeSymbol;
        private readonly PrimitiveTypeSymbol boolTypeSymbol;

        public Compiler(IAST tree) {
            this.context = LLVM.ContextCreate();
            this.builder = new IRBuilder(this.context);
            this.tree = tree;

            this.scopes.Push(new Scope());

            this.int32TypeSymbol = new PrimitiveTypeSymbol(LLVM.Int32Type());
            this.boolTypeSymbol = new PrimitiveTypeSymbol(LLVM.Int1Type());
        }

        public string Compile() {
            var mainType = LLVM.FunctionType(this.int32TypeSymbol.DefinedType, new LLVMTypeRef[0], false);
            var mainModule = LLVM.ModuleCreateWithName("Program");
            var mainFunc = LLVM.AddFunction(mainModule, "main", mainType);

            var mainFuncBasicBlock = LLVM.AppendBasicBlock(mainFunc, "entry");
            this.builder.PositionBuilderAtEnd(mainFuncBasicBlock);

            this.tree.Accept(this);
            var value = this.values.Pop();
            var block = LLVM.AppendBasicBlock(mainFunc, "exit");

            this.builder.CreateBr(block);
            this.builder.PositionBuilderAtEnd(block);

            if (!(value is IValueSymbol valueSym)) {
                CompilerErrors.ExpectedValueSymbol();
                return null;
            }

            this.builder.CreateRet(valueSym.Value);

            LLVM.VerifyFunction(mainFunc, LLVMVerifierFailureAction.LLVMPrintMessageAction);
            LLVM.DumpModule(mainModule);

            return "worked";
        }

        public void VisitBinaryExpression(BinaryExpression expr) {
            expr.LeftTarget.Accept(this);
            var left = this.EnsureIsType(this.values.Pop(), this.int32TypeSymbol);

            expr.RightTarget.Accept(this);
            var right = this.EnsureIsType(this.values.Pop(), this.int32TypeSymbol);

            LLVMValueRef result;

            switch (expr.Operator) {
                case BinaryOperator.Add:
                    result = this.builder.CreateAdd(left.Value, right.Value, "");
                    break;
                case BinaryOperator.Subtract:
                    result = this.builder.CreateSub(left.Value, right.Value, "");
                    break;
                case BinaryOperator.Multiply:
                    result = this.builder.CreateMul(left.Value, right.Value, "");
                    break;
                case BinaryOperator.Divide:
                    result = this.builder.CreateSDiv(left.Value, right.Value, "");
                    break;
                default:
                    throw new Exception("Unknown binary operator");
            }

            this.values.Push(new PrimitiveValueSymbol(result, this.int32TypeSymbol));
        }

        public void VisitBoolLiteral(BoolLiteral literal) {
            var value = LLVM.ConstInt(this.boolTypeSymbol.DefinedType, (ulong)(literal.Value ? 1 : 0), false);
            this.values.Push(new PrimitiveValueSymbol(value, this.boolTypeSymbol));
        }

        public void VisitIfExpression(IfExpression expr) {
            // Get bool expression
            expr.Condition.Accept(this);
            var cond = this.EnsureIsType(this.values.Pop(), this.boolTypeSymbol);

            // Get the current function
            var func = this.builder.GetInsertBlock().GetBasicBlockParent();

            // Create the new blocks
            var trueBlock = LLVM.AppendBasicBlock(func, "ifTrue");
            var falseBlock = LLVM.AppendBasicBlock(func, "ifFalse");
            var unbranchBlock = LLVM.AppendBasicBlock(func, "unbranch");

            // Create the conditional branch
            this.builder.CreateCondBr(cond.Value, trueBlock, falseBlock);

            // Write affimative expression
            this.builder.PositionBuilderAtEnd(trueBlock);
            expr.AffirmativeExpression.Accept(this);
            this.builder.CreateBr(unbranchBlock);
            var ifTrue = this.values.Pop() as IValueSymbol;
            trueBlock = this.builder.GetInsertBlock();

            // Get negative expression
            this.builder.PositionBuilderAtEnd(falseBlock);
            expr.NegativeExpression.Accept(this);
            this.builder.CreateBr(unbranchBlock);
            var ifFalse = this.values.Pop() as IValueSymbol;
            falseBlock = this.builder.GetInsertBlock();

            // Run checks against both
            if (ifTrue == null || ifFalse == null) {
                CompilerErrors.ExpectedValueSymbol();
            }

            if (ifTrue.TypeSymbol != ifFalse.TypeSymbol) {
                CompilerErrors.MismatchedTypes();
            }

            this.builder.PositionBuilderAtEnd(unbranchBlock);
            var phi = this.builder.CreatePhi(ifTrue.TypeSymbol.DefinedType, "");

            phi.AddIncoming(new[] { ifTrue.Value }, new[] { trueBlock }, 1);
            phi.AddIncoming(new[] { ifFalse.Value }, new[] { falseBlock }, 1);

            this.values.Push(new PrimitiveValueSymbol(phi, ifTrue.TypeSymbol));
        }

        public void VisitInt32Literal(Int32Literal literal) {
            var value = LLVM.ConstInt(this.int32TypeSymbol.DefinedType, (ulong)literal.Value, false);
            this.values.Push(new PrimitiveValueSymbol(value, this.int32TypeSymbol));
        }

        public void VisitUnaryExpression(UnaryExpression expr) {
            expr.Target.Accept(this);
            var target = this.EnsureIsType(this.values.Pop(), this.int32TypeSymbol);
            
            if (expr.Operator == UnaryOperator.Negate) {
                var intType = LLVM.Int32Type();
                var zero = LLVM.ConstInt(intType, 0, false);

                target = new PrimitiveValueSymbol(this.builder.CreateSub(zero, target.Value, ""), this.int32TypeSymbol);
            }

            this.values.Push(target);
        }

        public void VisitVariableDeclaration(VariableDeclaration decl) {
            decl.AssignExpression.Accept(this);
            var assign = this.values.Pop();

            this.scopes.Push(this.scopes.Peek().AddSymbol(decl.Name, assign));
            decl.AppendixExpression.Accept(this);
            this.scopes.Pop();
        }

        public void VisitVariableUsage(VariableUsage usage) {
            if (!this.scopes.Peek().Symbols.TryGetValue(usage.VariableName, out var symbol)) {
                CompilerErrors.UndeclaredVariable(usage.VariableName);
            }
            else {
                this.values.Push(symbol);
            }
        }

        private IValueSymbol EnsureIsType(ISymbol symbol, ISymbol type) {
            IValueSymbol value = symbol as IValueSymbol;

            if (value == null) {
                CompilerErrors.ExpectedValueSymbol();
            }

            if (value.TypeSymbol != type) {
                CompilerErrors.InvalidSymbolType(symbol, type);
            }

            return value;
        }
    }
}