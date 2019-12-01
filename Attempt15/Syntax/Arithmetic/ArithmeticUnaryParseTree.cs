using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System;

namespace JoshuaKearney.Attempt15.Syntax.Arithmetic {
    public class ArithmeticUnaryParseTree : IParseTree {
        public IParseTree Operand { get; }

        public ArithmeticUnaryOperationKind Operation { get; }

        public ArithmeticUnaryParseTree(IParseTree operand, ArithmeticUnaryOperationKind op) {
            this.Operand = operand;
            this.Operation = op;
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var operand = this.Operand.Analyze(args);

            switch (this.Operation) {
                case ArithmeticUnaryOperationKind.ConvertIntToReal:
                    if (!args.Unifier.TryUnifySyntax(operand, SimpleType.Boolean, out operand)) {
                        throw new Exception();
                    }

                    return new ArithmeticUnarySyntaxTree(
                        operand:    operand,
                        kind:       this.Operation,
                        returnType: new SimpleType(TrophyTypeKind.Float)
                    );
                default:
                    throw new Exception();
            }
        }
    }
}