using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.FlowControl {
    public enum IfKind {
        Expression, Statement
    }

    public class IfParseTree : IParseTree {
        public TokenLocation Location { get; }

        public IfKind Kind { get; }

        public IParseTree Condition { get; }

        public IParseTree Affirmative { get; }

        public IOption<IParseTree> Negative { get; }

        public IfParseTree(TokenLocation loc, IfKind kind, IParseTree cond, IParseTree affirm, IOption<IParseTree> neg) {
            this.Location = loc;
            this.Kind = kind;
            this.Condition = cond;
            this.Affirmative = affirm;
            this.Negative = neg;
        }

        public ISyntaxTree Analyze(Scope scope) {
            var cond = this.Condition.Analyze(scope);
            var affirm = this.Affirmative.Analyze(scope);
            var neg = this.Negative.Select(x => x.Analyze(scope));

            if (cond.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(this.Condition.Location, IntType.Instance, cond.ReturnType);
            }

            if (this.Kind == IfKind.Expression) {


                if (affirm.ReturnType != neg.GetValue().ReturnType) {
                    throw TypeCheckingErrors.UnexpectedType(this.Negative.GetValue().Location, affirm.ReturnType, neg.GetValue().ReturnType);
                }
            }

            return new IfSyntaxTree(this.Kind, cond, affirm, neg);
        }
    }
}