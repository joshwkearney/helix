using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Analysis;

namespace Helix.Features {
    public record TypeSyntax : ISyntaxTree {
        private readonly HelixType type;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public TypeSyntax(TokenLocation loc, HelixType type) {
            this.Location = loc;
            this.type = type;
        }

        public Option<HelixType> AsType(TypeFrame types) => this.type;

        public ISyntaxTree CheckTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
