using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Syntax;

namespace Helix.Analysis.TypeChecking {
    public record SyntaxTag {
        public IReadOnlyList<VariableCapture> CapturedVariables { get; }

        public ISyntaxPredicate Predicate { get; }

        public HelixType ReturnType { get; }
        
        public SyntaxTag(
            HelixType returnType,
            IReadOnlyList<VariableCapture> cap, 
            ISyntaxPredicate pred) {

            this.ReturnType = returnType;
            this.CapturedVariables = cap;
            this.Predicate = pred;
        }

        public SyntaxTagBuilder ToBuilder(TypeFrame types) {
            return SyntaxTagBuilder.AtFrame(types)
                .WithCapturedVariables(this.CapturedVariables)
                .WithPredicate(this.Predicate)
                .WithReturnType(this.ReturnType);
        }
    }

    public class SyntaxTagBuilder {
        private readonly TypeFrame types;

        private IReadOnlyList<VariableCapture> CapturedVariables = Array.Empty<VariableCapture>();
        private ISyntaxPredicate Predicate = ISyntaxPredicate.Empty;
        private HelixType ReturnType = PrimitiveType.Void;

        public static SyntaxTagBuilder AtFrame(TypeFrame types) {
            return new SyntaxTagBuilder(types);
        }

        public static SyntaxTagBuilder AtFrame(TypeFrame types, ISyntaxTree syntax) {
            return types.SyntaxTags[syntax].ToBuilder(types);
        }

        private SyntaxTagBuilder(TypeFrame types) {
            this.types = types;
        }

        public SyntaxTagBuilder WithChildren(IEnumerable<ISyntaxTree> children) {
            this.CapturedVariables = children
                .SelectMany(x => x.GetCapturedVariables(this.types))
                .ToArray();

            this.Predicate = children
                .Select(x => x.GetPredicate(this.types))
                .Aggregate((x, y) => x.And(y));

            return this;
        }

        public SyntaxTagBuilder WithChildren(params ISyntaxTree[] children) {
            return this.WithChildren((IEnumerable<ISyntaxTree>)children);
        }

        public SyntaxTagBuilder WithReturnType(HelixType type) {
            this.ReturnType = type;

            return this;
        }

        public SyntaxTagBuilder WithCapturedVariables(IEnumerable<VariableCapture> cap) {
            this.CapturedVariables = cap.ToArray();

            return this;
        }

        public SyntaxTagBuilder WithCapturedVariables(params VariableCapture[] cap) {
            return this.WithCapturedVariables((IEnumerable<VariableCapture>)cap);
        }

        public SyntaxTagBuilder WithPredicate(ISyntaxPredicate pred) {
            this.Predicate = pred;

            return this;
        }

        public void BuildFor(ISyntaxTree syntax) {
            var tag = new SyntaxTag(
                this.ReturnType, 
                this.CapturedVariables, 
                this.Predicate);

            this.types.SyntaxTags[syntax] = tag;
        }
    }
}
