using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System;

namespace JoshuaKearney.Attempt15.Syntax.Logic {
    public class BooleanUnaryParseTree : IParseTree {
        public IParseTree Operand { get; }

        public BooleanUnaryOperationKind Operation { get; }

        public BooleanUnaryParseTree(IParseTree operand, BooleanUnaryOperationKind kind) {
            this.Operand = operand;
            this.Operation = kind;
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var operand = this.Operand.Analyze(args);

            if (!args.Unifier.TryUnifySyntax(operand, SimpleType.Boolean, out operand)) {
                throw new Exception();
            }

            switch (this.Operation) {
                case BooleanUnaryOperationKind.ConvertBoolToInt:
                    return new BooleanUnarySyntaxTree(
                        operand:    operand,
                        kind:       this.Operation,
                        returnType: SimpleType.Int
                    );
                case BooleanUnaryOperationKind.ConvertBoolToReal:
                    return new BooleanUnarySyntaxTree(
                        operand:    operand,
                        kind:       this.Operation,
                        returnType: SimpleType.Float
                    );
                default:
                    throw new Exception();
            }
        }
    }
}
