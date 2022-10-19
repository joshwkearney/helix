using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis;
using Trophy.Generation;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Features {
    public record DummySyntax : ISyntaxTree {
        public TokenLocation Location { get; }

        public DummySyntax(TokenLocation loc) {
            this.Location = loc;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}