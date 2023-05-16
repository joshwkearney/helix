using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features {
    public record DummySyntax : ISyntaxTree {
        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public DummySyntax(TokenLocation loc) {
            this.Location = loc;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}