using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Features.Primitives;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Features.Aggregates {
    public class NewUnionSyntax : IParseTree {
        private static int tempCounter = 0;

        private readonly HelixType unionType;
        private readonly UnionType sig;
        private readonly IReadOnlyList<string> names;
        private readonly IReadOnlyList<IParseTree> values;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => Enumerable.Empty<IParseTree>();

        public bool IsPure { get; }

        public NewUnionSyntax(TokenLocation loc, HelixType unionType, UnionType sig,
                              IReadOnlyList<string> names, IReadOnlyList<IParseTree> values,
                              IdentifierPath path) {
            this.Location = loc;
            this.unionType = unionType;
            this.sig = sig;
            this.names = names;
            this.values = values;
            this.tempPath = path;

            this.IsPure = this.values.All(x => x.IsPure);
        }

        public NewUnionSyntax(TokenLocation loc, HelixType unionType, UnionType sig,
                              IReadOnlyList<string> names, IReadOnlyList<IParseTree> values)
            : this(loc, unionType, sig, names, values, new IdentifierPath("$union" + tempCounter++)) { }
    }
}