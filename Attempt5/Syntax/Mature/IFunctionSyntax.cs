using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt6.Syntax {
    public interface IFunctionSyntax {
        FunctionType ExpressionType { get; }
        void Accept(IFunctionSyntaxVisitor visitor);
    }

    public interface IFunctionSyntaxVisitor {
        void Visit(IntrinsicFunction func);
    }

    public enum IntrinsicFunctionKind {
        AddInt32, SubtractInt32, MultiplyInt32, DivideInt32
    }

    public class IntrinsicFunction : IFunctionSyntax {
        public static IntrinsicFunction MultiplyInt32 { get; } = new IntrinsicFunction(IntrinsicFunctionKind.MultiplyInt32, PrimitiveType.Int32Type, PrimitiveType.Int32Type, PrimitiveType.Int32Type);
        public static IntrinsicFunction DivideInt32 { get; } = new IntrinsicFunction(IntrinsicFunctionKind.DivideInt32, PrimitiveType.Int32Type, PrimitiveType.Int32Type, PrimitiveType.Int32Type);
        public static IntrinsicFunction AddInt32 { get; } = new IntrinsicFunction(IntrinsicFunctionKind.AddInt32, PrimitiveType.Int32Type, PrimitiveType.Int32Type, PrimitiveType.Int32Type);
        public static IntrinsicFunction SubtractInt32 { get; } = new IntrinsicFunction(IntrinsicFunctionKind.SubtractInt32, PrimitiveType.Int32Type, PrimitiveType.Int32Type, PrimitiveType.Int32Type);

        public FunctionType ExpressionType { get; }

        public IntrinsicFunctionKind Kind { get; }

        public IntrinsicFunction(IntrinsicFunctionKind kind, ILanguageType returnType, params ILanguageType[] paramTypes) {
            this.Kind = kind;
            this.ExpressionType = new FunctionType(returnType, paramTypes);
        }

        public void Accept(IFunctionSyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}