using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Arrays;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Arrays {
    public record ArrayTypeSyntax : ISyntaxTree {
        private readonly ISyntaxTree inner;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children {
            get => new[] { this.inner };
        }

        public bool IsPure => this.inner.IsPure;

        public ArrayTypeSyntax(TokenLocation loc, ISyntaxTree inner) {
            this.Location = loc;
            this.inner = inner;
        }

        Option<HelixType> ISyntaxTree.AsType(SyntaxFrame types) {
            return this.inner
                .AsType(types)
                .Select(x => new ArrayType(x))
                .Select(x => (HelixType)x);
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
