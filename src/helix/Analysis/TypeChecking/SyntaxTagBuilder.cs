using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Syntax;

namespace Helix.Analysis.TypeChecking;

public class SyntaxTagBuilder {
    private readonly TypeFrame types;

    private ISyntaxPredicate predicate = ISyntaxPredicate.Empty;
    private HelixType returnType = PrimitiveType.Void;

    public SyntaxTagBuilder(TypeFrame types) {
        this.types = types;
    }

    public SyntaxTagBuilder WithChildren(IEnumerable<ISyntaxTree> children) {
        this.predicate = children
            .Select(x => x.GetPredicate(this.types))
            .Aggregate((x, y) => x.And(y));

        return this;
    }

    public SyntaxTagBuilder WithChildren(params ISyntaxTree[] children) {
        return this.WithChildren((IEnumerable<ISyntaxTree>)children);
    }

    public SyntaxTagBuilder WithReturnType(HelixType type) {
        this.returnType = type;

        return this;
    }

    public SyntaxTagBuilder WithPredicate(ISyntaxPredicate pred) {
        this.predicate = pred;

        return this;
    }

    public SyntaxTag Build() {
        return new SyntaxTag(
            this.returnType, 
            this.predicate);
    }
}