using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Predicates;
using System;
using Helix.Features.Primitives;
using System.ComponentModel;

namespace Helix.Parsing
{
    public partial class Parser {
        private IParseTree Block() {
            var start = this.Advance(TokenKind.OpenBrace);

            IParseTree stat;
            if (this.Peek(TokenKind.CloseBrace)) {
                stat = new VoidLiteral(start.Location);
            }
            else {
                stat = this.BlockStatement();
            }

            this.Advance(TokenKind.CloseBrace);

            return stat;
        }

        private IParseTree BlockStatement() {
            var first = this.Statement();

            if (!this.Peek(TokenKind.CloseBrace)) {
                first = new BlockSyntax(first, this.BlockStatement());
            }

            return first;
        }
    }
}

namespace Helix.Features.FlowControl
{
    public record BlockSyntax : IParseTree {
        private static int blockCounter = 0;

        public TokenLocation Location { get; }

        public IParseTree First { get; }

        public IParseTree Second { get; }

        public IEnumerable<IParseTree> Children => new[] { this.First, this.Second };

        public bool IsPure { get; }

        public IdentifierPath Path { get; }

        public bool IsStatement => true;

        public BlockSyntax(IParseTree first, IParseTree second, IdentifierPath path) {
            this.Location = first.Location.Span(second.Location);
            this.First = first;
            this.Second = second;
            this.IsPure = this.First.IsPure && this.Second.IsPure;
            this.Path = path;
        }

        public BlockSyntax(IParseTree first, IParseTree second) 
            : this(first, second, new IdentifierPath("$b" + blockCounter++)) { }

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) {
            this.First.ToImperativeSyntax(writer);
            return this.Second.ToImperativeSyntax(writer);
        }
    }
}
