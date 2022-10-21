using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.Arrays;
using Trophy.Generation;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Features.Arrays {
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

        Option<TrophyType> ISyntaxTree.AsType(SyntaxFrame types) {
            return this.inner
                .AsType(types)
                .Select(x => new ArrayType(x))
                .Select(x => (TrophyType)x);
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
