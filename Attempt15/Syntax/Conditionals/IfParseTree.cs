using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;
using System;

namespace JoshuaKearney.Attempt15.Syntax.Conditionals {
    public class IfParseTree : IParseTree {
        public IParseTree Condition { get; }

        public IParseTree Affirmative { get; }

        public IParseTree Negative { get; }

        public IfParseTree(IParseTree cond, IParseTree affirm, IParseTree neg) {
            this.Condition = cond;
            this.Affirmative = affirm;
            this.Negative = neg;
        }

        public ISyntaxTree Analyze(AnalyzeEventArgs args) {
            var cond = this.Condition.Analyze(args);
            var affirm = this.Affirmative.Analyze(args);
            var neg = this.Negative.Analyze(args);

            if (!args.Unifier.TryUnifySyntax(cond, SimpleType.Boolean, out cond)) {
                throw new Exception();
            }

            if (args.Unifier.TryUnifySyntax(affirm, neg.ExpressionType, out var result)) {
                affirm = result;
            }
            else if (args.Unifier.TryUnifySyntax(neg, affirm.ExpressionType, out result)) {
                neg = result;
            }
            else {
                throw new Exception();
            }

            return new IfSyntaxTree(
                condition:  cond,
                affirm:     affirm,
                neg:        neg,
                returnType: affirm.ExpressionType
            );
        }
    }
}