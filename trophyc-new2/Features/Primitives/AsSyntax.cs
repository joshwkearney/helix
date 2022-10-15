using System.Collections.Immutable;
using Trophy;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing {
    public partial class Parser {
        private IParseTree AsExpression() {
            var first = this.BinaryExpression();

            while (this.Peek(TokenKind.AsKeyword)) {
                if (this.TryAdvance(TokenKind.AsKeyword)) {
                    var target = this.TypeExpression();
                    var loc = first.Location.Span(this.tokens[this.pos - 1].Location);

                    first = new AsParseTree(loc, first, target);
                }
            }

            return first;
        }
    }
}

namespace Trophy.Features.Primitives {
    public class AsParseTree : IParseTree {
        private readonly IParseTree arg;
        private readonly ITypeTree target;

        public TokenLocation Location { get; }

        public AsParseTree(TokenLocation loc, IParseTree arg, ITypeTree target) {
            this.Location = loc;
            this.arg = arg;
            this.target = target;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            var arg = this.arg.ResolveTypes(scope, names, types);
            var target = this.target.ResolveNames(scope, names);

            if (arg.TryUnifyTo(target).TryGetValue(out var newArg)) {
                return newArg;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.Location, target, arg.ReturnType);
            }
        }
    }
}