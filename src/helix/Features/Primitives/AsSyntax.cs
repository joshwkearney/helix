﻿using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree AsExpression(BlockBuilder block) {
            var first = this.BinaryExpression(block);

            while (this.TryAdvance(TokenKind.AsKeyword)) {
                var target = this.TopExpression(block);
                var loc = first.Location.Span(target.Location);

                first = new AsParseTree(loc, first, target);
            }

            return first;
        }
    }
}

namespace Helix.Features.Primitives {
    public record AsParseTree : ISyntaxTree {
        private readonly ISyntaxTree arg;
        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.arg, this.target };

        public bool IsPure { get; }

        public AsParseTree(TokenLocation loc, ISyntaxTree arg, ISyntaxTree target) {
            this.Location = loc;
            this.arg = arg;
            this.target = target;

            this.IsPure = this.target.IsPure && this.arg.IsPure;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var arg = this.arg.CheckTypes(types).ToRValue(types);

            if (!this.target.AsType(types).TryGetValue(out var targetType)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.target.Location);
            }

            arg = arg.UnifyTo(targetType, types);

            return arg;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}