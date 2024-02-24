using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Functions;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseTree InvokeExpression(IParseTree first) {
            this.Advance(TokenKind.OpenParenthesis);

            var args = new List<IParseTree>();

            while (!this.Peek(TokenKind.CloseParenthesis)) {
                args.Add(this.TopExpression());

                if (!this.TryAdvance(TokenKind.Comma)) {
                    break;
                }
            }

            var last = this.Advance(TokenKind.CloseParenthesis);
            var loc = first.Location.Span(last.Location);

            return new InvokeParseSyntax(loc, first, args);
        }
    }
}

namespace Helix.Features.Functions {
    public record InvokeParseSyntax : IParseTree {
        private static int tempCounter = 0;

        private readonly IParseTree target;
        private readonly IReadOnlyList<IParseTree> args;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => this.args.Prepend(this.target);

        public bool IsPure => false;

        public InvokeParseSyntax(TokenLocation loc, IParseTree target, 
            IReadOnlyList<IParseTree> args) {

            this.Location = loc;
            this.target = target;
            this.args = args;
        }
    }

    public record InvokeSyntax : IParseTree {
        private readonly FunctionType sig;
        private readonly IReadOnlyList<IParseTree> args;
        private readonly IdentifierPath funcPath;
        private readonly IdentifierPath invokeTempPath;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => this.args;

        public bool IsPure => false;

        public InvokeSyntax(
            TokenLocation loc,
            FunctionType sig,
            IReadOnlyList<IParseTree> args,
            IdentifierPath path,
            IdentifierPath tempPath) {

            this.Location = loc;
            this.sig = sig;
            this.args = args;
            this.funcPath = path;
            this.invokeTempPath = tempPath;
        }

        public IParseTree ToRValue(TypeFrame types) => this;
    }
}