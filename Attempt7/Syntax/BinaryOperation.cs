using System;
using System.Collections.Generic;
using System.Text;
using LLVMSharp;

namespace Attempt7.Syntax {
    public enum BinaryOperator {
        Addition, Subtraction, Multiplication, Division
    }

    public class BinaryOperation : ISymbol {
        public BinaryOperator Operator { get; }

        public ISymbol Right { get; }

        public ISymbol Left { get; }

        public LanguageType ReturnType { get; }

        public BinaryOperation(BinaryOperator op, ISymbol left, ISymbol right) {
            this.Operator = op;
            this.Left = left;
            this.Right = right;
        }

        public CompilationResult Compile(BuilderArgs compiler, Scope scope) {
            var left = this.Left.Compile(compiler, scope);
            var right = this.Right.Compile(compiler, left.LastScope);

            if (left.ReturnType != LanguageType.Int32Type || right.ReturnType != LanguageType.Int32Type) {
                throw new Exception();
            }

            LLVMValueRef result;

            switch (this.Operator) {
                case BinaryOperator.Addition:
                    result = compiler.Builder.CreateAdd(left.Result, right.Result, "");
                    break;
                case BinaryOperator.Subtraction:
                    result = compiler.Builder.CreateSub(left.Result, right.Result, "");
                    break;
                case BinaryOperator.Multiplication:
                    result = compiler.Builder.CreateMul(left.Result, right.Result, "");
                    break;
                case BinaryOperator.Division:
                    result = compiler.Builder.CreateSDiv(left.Result, right.Result, "");
                    break;
                default:
                    throw new NotImplementedException();
            }

            return new CompilationResult(result, right.LastScope, LanguageType.Int32Type);
        }

        public InterpretationResult Interpret(Scope scope) {
            var leftSym = this.Left.Interpret(scope);
            var rightSym = this.Right.Interpret(leftSym.LastScope);

            if (leftSym.ReturnType != LanguageType.Int32Type || rightSym.ReturnType != LanguageType.Int32Type) {
                throw new Exception();
            }

            int left = (int)(leftSym.Result as IPrimitiveSymbol).Value;
            int right = (int)(rightSym.Result as IPrimitiveSymbol).Value;
            int result;

            switch (this.Operator) {
                case BinaryOperator.Addition:
                    result = left + right;
                    break;
                case BinaryOperator.Subtraction:
                    result = left - right;
                    break;
                case BinaryOperator.Multiplication:
                    result = left * right;
                    break;
                case BinaryOperator.Division:
                    result = left / right;
                    break;
                default:
                    throw new NotImplementedException();
            }

            return new InterpretationResult(new Int32Literal(result), rightSym.LastScope, LanguageType.Int32Type);
        }

        public override string ToString() {
            return $"{this.Operator}({this.Left},{this.Right})";
        }
    }
}