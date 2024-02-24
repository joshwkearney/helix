using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        public IParseTree DereferenceExpression(IParseTree first) {
            var op = this.Advance(TokenKind.Star);
            var loc = first.Location.Span(op.Location);

            return new DereferenceParseSyntax(
                loc, 
                first);
        }
    }
}

namespace Helix.Features.Variables {
    // Dereference syntax is split into three classes: this one that does
    // some basic type checking so it's easy for the parser to spit out
    // a single class, a dereference rvalue, and a dereference lvaulue.
    // This is for clarity because dereference rvalues and lvalues have
    // very different semantics, especially when it comes to lifetimes
    public record DereferenceParseSyntax : IParseTree {
        private static int derefCounter = 0;
        private readonly IParseTree target;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceParseSyntax(TokenLocation loc, IParseTree target) {
            this.Location = loc;
            this.target = target;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            return this.target.AsType(types)
                .Select(x => new PointerType(x))
                .Select(x => (HelixType)x);
        }
    }

    public record DereferenceSyntax : IParseTree {
        private readonly bool isLValue;
        private readonly IParseTree target;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceSyntax(
            TokenLocation loc, 
            IParseTree target, 
            IdentifierPath tempPath,
            bool isLValue) {

            this.Location = loc;
            this.target = target;
            this.tempPath = tempPath;
            this.isLValue = isLValue;
        }
    }
}
