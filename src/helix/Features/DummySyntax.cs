using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using helix.Syntax;
using Helix.Analysis;
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

        public ISyntaxTree CheckTypes(EvalFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(EvalFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}