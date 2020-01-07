using Attempt17.Parsing;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Attempt17.Features.IntLiteral {
    public class IntLiteralParseTree : IParseTree {
        public TokenLocation Location { get; }

        public long Value { get; }

        public IntLiteralParseTree(TokenLocation location, long value) {
            this.Location = location;
            this.Value = value;
        }

        public ISyntaxTree Analyze(Scope scope) {
            var tree = new IntLiteralSyntaxTree(this.Value);

            return tree;
        }
    }
}