﻿using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Analysis;
using Helix.Analysis.Predicates;

namespace Helix.Parsing {
    public partial class Parser {
        public IParseTree BreakStatement() {
            Token start;
            bool isBreak;

            if (this.Peek(TokenKind.BreakKeyword)) {
                start = this.Advance(TokenKind.BreakKeyword);
                isBreak = true;
            }
            else {
                start = this.Advance(TokenKind.ContinueKeyword);
                isBreak = false;
            }

            if (!this.isInLoop.Peek()) {
                throw new ParseException(
                    start.Location, 
                    "Invalid Statement", 
                    "Break and continue statements must only appear inside of loops");
            }

            return new BreakContinueSyntax(start.Location, isBreak);
        }
    }
}

namespace Helix.Features.FlowControl {
    public record BreakContinueSyntax : IParseTree {
        private readonly bool isbreak;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => Enumerable.Empty<IParseTree>();

        public bool IsPure => false;

        public BreakContinueSyntax(TokenLocation loc, bool isbreak) {
            this.Location = loc;
            this.isbreak = isbreak;
        }

        public IParseTree ToRValue(TypeFrame types) => this;
    }
}
