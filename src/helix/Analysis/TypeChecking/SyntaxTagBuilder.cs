using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Syntax;

namespace Helix.Analysis.TypeChecking;

public class SyntaxTagBuilder {
    private readonly TypeFrame types;

    private IReadOnlyList<VariableCapture> capturedVariables = Array.Empty<VariableCapture>();
    private ISyntaxPredicate predicate = ISyntaxPredicate.Empty;
    private HelixType returnType = PrimitiveType.Void;

    public SyntaxTagBuilder(TypeFrame types) {
        this.types = types;
    }

    public SyntaxTagBuilder WithChildren(IEnumerable<ISyntaxTree> children) {
        this.capturedVariables = children
            .SelectMany(x => x.GetCapturedVariables(this.types))
            .ToArray();

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

    public SyntaxTagBuilder WithCapturedVariables(IEnumerable<VariableCapture> cap) {
        this.capturedVariables = cap.ToArray();

        return this;
    }

    public SyntaxTagBuilder WithCapturedVariables(params VariableCapture[] cap) {
        return this.WithCapturedVariables((IEnumerable<VariableCapture>)cap);
    }

    public SyntaxTagBuilder WithPredicate(ISyntaxPredicate pred) {
        this.predicate = pred;

        return this;
    }

    public SyntaxTag Build() {
        return new SyntaxTag(
            this.returnType, 
            this.capturedVariables, 
            this.predicate);
    }
}