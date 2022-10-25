using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

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

        public Option<HelixType> AsType(SyntaxFrame types) => this.type;

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
