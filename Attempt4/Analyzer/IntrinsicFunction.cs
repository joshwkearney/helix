using System.Collections.Generic;

namespace Attempt4 {
    public class IntrinsicFunction : IAnalyzedSyntax {
        public static IntrinsicFunction AddInt32 { get; } = new IntrinsicFunction(
            LanguageType.Int32Type,
            new FunctionParameter(LanguageType.Int32Type, "num1"), 
            new FunctionParameter(LanguageType.Int32Type, "num2")
        );

        public static IntrinsicFunction SubtractInt32 { get; } = new IntrinsicFunction(
            LanguageType.Int32Type,
            new FunctionParameter(LanguageType.Int32Type, "num1"),
            new FunctionParameter(LanguageType.Int32Type, "num2")
        );

        public static IntrinsicFunction MultiplyInt32 { get; } = new IntrinsicFunction(
            LanguageType.Int32Type,
            new FunctionParameter(LanguageType.Int32Type, "num1"),
            new FunctionParameter(LanguageType.Int32Type, "num2")
        );

        public static IntrinsicFunction DivideInt32 { get; } = new IntrinsicFunction(
            LanguageType.Int32Type,
            new FunctionParameter(LanguageType.Int32Type, "num1"),
            new FunctionParameter(LanguageType.Int32Type, "num2")
        );

        public IReadOnlyList<FunctionParameter> Parameters { get; }

        public LanguageType ExpressionType { get; }

        public IntrinsicFunction(LanguageType type, params FunctionParameter[] pars) {
            this.ExpressionType = type;
            this.Parameters = pars;
        }

        public void Accept(IAnalyzedSyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}