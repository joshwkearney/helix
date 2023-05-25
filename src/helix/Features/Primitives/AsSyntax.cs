using Helix.Analysis;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Unions;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree AsExpression() {
            var first = this.BinaryExpression();

            while (this.Peek(TokenKind.AsKeyword) || this.Peek(TokenKind.IsKeyword)) {
                if (this.TryAdvance(TokenKind.AsKeyword)) {
                    var target = this.TopExpression();
                    var loc = first.Location.Span(target.Location);

                    first = new AsParseTree(loc, first, target);
                }
                else {
                    this.Advance(TokenKind.IsKeyword);
                    var nameTok = this.Advance(TokenKind.Identifier);

                    first = new IsParseSyntax() {
                        Location = first.Location.Span(nameTok.Location),
                        Target = first,
                        MemberName = nameTok.Value
                    };
                }
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

        public ISyntaxTree CheckTypes(TypeFrame types) {
            var arg = this.arg.CheckTypes(types).ToRValue(types);

            if (!this.target.AsType(types).TryGetValue(out var targetType)) {
                throw TypeException.ExpectedTypeExpression(this.target.Location);
            }

            arg = arg.UnifyTo(targetType, types);

            return arg;
        }
    }
}