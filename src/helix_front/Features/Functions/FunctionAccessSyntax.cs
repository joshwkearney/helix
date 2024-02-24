using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Analysis;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Features.Functions {
    public record FunctionAccessSyntax : IParseTree {
        public IdentifierPath FunctionPath { get; }

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => Enumerable.Empty<IParseTree>();

        public bool IsPure => true;

        public FunctionAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.FunctionPath = path;
        }

        public IParseTree ToRValue(TypeFrame types) => this;
    }
}