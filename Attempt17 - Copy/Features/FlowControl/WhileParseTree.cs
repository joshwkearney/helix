using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.FlowControl {
    public class WhileParseTree : IParseTree {
        public TokenLocation Location { get; }

        public IParseTree Condition { get; }

        public IParseTree Body { get; }

        public WhileParseTree(TokenLocation loc, IParseTree cond, IParseTree body) {
            this.Location = loc;
            this.Condition = cond;
            this.Body = body;
        }

        public ISyntaxTree Analyze(Scope scope) {
            var cond = this.Condition.Analyze(scope);
            var body = this.Body.Analyze(scope);

            if (cond.ReturnType != IntType.Instance) {
                throw TypeCheckingErrors.UnexpectedType(this.Condition.Location, IntType.Instance, cond.ReturnType);
            }

            return new WhileSyntaxTree(cond, body);
        }
    }
}