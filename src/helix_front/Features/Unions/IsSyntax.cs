using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Collections;
using Helix.Features.Aggregates;
using Helix.Features.Variables;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Unions {
    public record IsParseSyntax : IParseTree {
        public TokenLocation Location { get; init; }

        public IParseTree Target { get; init; }

        public string MemberName { get; init; }

        public bool IsPure => this.Target.IsPure;

        public IEnumerable<IParseTree> Children => this.Target.Children;
    }

    public record IsSyntax : IParseTree {
        public TokenLocation Location { get; init; }

        public IdentifierPath VariablePath { get; init; }

        public string MemberName { get; init; }

        public UnionType UnionSignature { get; init; }

        public IEnumerable<IParseTree> Children => Array.Empty<IParseTree>();

        public bool IsPure => true;

        public IParseTree ToRValue(TypeFrame types) => this;
    }
}