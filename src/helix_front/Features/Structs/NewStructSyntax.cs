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
    public class NewStructSyntax : IParseTree {
        private static int tempCounter = 0;

        private readonly bool isTypeChecked;
        private readonly StructType sig;
        private readonly HelixType structType;
        private readonly IReadOnlyList<string> names;
        private readonly IReadOnlyList<IParseTree> values;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => Enumerable.Empty<IParseTree>();

        public bool IsPure { get; }

        public NewStructSyntax(TokenLocation loc, HelixType structType, StructType sig,
                               IReadOnlyList<string> names, IReadOnlyList<IParseTree> values, 
                               IdentifierPath scope, bool isTypeChecked = false) {
            this.Location = loc;
            this.sig = sig;
            this.structType = structType;
            this.names = names;
            this.values = values;
            this.isTypeChecked = isTypeChecked;
            this.path = scope.Append("$struct" + tempCounter++);

            this.IsPure = this.values.All(x => x.IsPure);
        }

        public IParseTree ToRValue(TypeFrame types) {
            if (!this.isTypeChecked) {
                throw TypeException.RValueRequired(this.Location);
            }

            return this;
        }
    }
}