using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Features.Functions;
using System.Collections.Immutable;
using Helix.Features.Arrays;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseTree ArrayLiteral() {
            var start = this.Advance(TokenKind.OpenBracket);
            var args = new List<IParseTree>();

            while (!this.Peek(TokenKind.CloseBracket)) {
                args.Add(this.TopExpression());

                if (!this.Peek(TokenKind.CloseBracket)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            var end = this.Advance(TokenKind.CloseBracket);
            var loc = start.Location.Span(end.Location);

            return new ArrayLiteralSyntax(loc, args);
        }
    }
}

namespace Helix.Features.Arrays {
    public record ArrayLiteralSyntax : IParseTree {
        private static int tempCounter = 0;

        private readonly IReadOnlyList<IParseTree> args;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => this.args;

        public bool IsPure => this.args.All(x => x.IsPure);

        public ArrayLiteralSyntax(TokenLocation loc, IReadOnlyList<IParseTree> args) {
            this.Location = loc;
            this.args = args;
            this.tempPath = new IdentifierPath("$array" + tempCounter++);
        }

        public ArrayLiteralSyntax(TokenLocation loc, IReadOnlyList<IParseTree> args, IdentifierPath tempPath) {
            this.Location = loc;
            this.args = args;
            this.tempPath = tempPath;
        }
    }
}