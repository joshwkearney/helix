using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.ParseTree.Arrays {
    public record ArrayTypeParseTree : IParseTree {
        public required TokenLocation Location { get; init; }
        
        public required IParseTree Operand { get; init; }
        
        Option<HelixType> IParseTree.AsType(TypeFrame types) {
            return this.Operand
                .AsType(types)
                .Select(x => new ArrayType(x))
                .Select(x => (HelixType)x);
        }

        public TypeCheckResult<ITypedTree> CheckTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }
    }
}
