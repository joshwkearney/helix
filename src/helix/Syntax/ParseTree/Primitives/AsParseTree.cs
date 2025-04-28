using Helix.Parsing;
using Helix.TypeChecking;

namespace Helix.Syntax.ParseTree.Primitives {
    public record AsParseTree : IParseTree {
        public required TokenLocation Location { get; init; }
        
        public required IParseTree Operand { get; init; }

        public required IParseTree TypeTree { get; init; }

        public bool IsPure => this.Operand.IsPure && this.TypeTree.IsPure;

        public TypeCheckResult CheckTypes(TypeFrame types) {
            (var arg, types) = this.Operand.CheckTypes(types);

            if (!this.TypeTree.AsType(types).TryGetValue(out var targetType)) {
                throw TypeException.ExpectedTypeExpression(this.TypeTree.Location);
            }
            
            arg = arg.UnifyTo(targetType, types);
            
            return new TypeCheckResult(arg, types);
        }
    }
}