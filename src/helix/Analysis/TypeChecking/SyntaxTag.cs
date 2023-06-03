using Helix.Analysis.Flow;
using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Features.FlowControl;
using Helix.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.TypeChecking {
    public record SyntaxTag {
        public IReadOnlyList<VariableCapture> CapturedVariables { get; }

        public ISyntaxPredicate Predicate { get; }

        public HelixType ReturnType { get; }

        public LifetimeBounds Bounds { get; }

        public SyntaxTag(
            HelixType returnType,
            LifetimeBounds bounds, 
            IReadOnlyList<VariableCapture> cap, 
            ISyntaxPredicate pred) {

            this.ReturnType = returnType;
            this.Bounds = bounds;
            this.CapturedVariables = cap;
            this.Predicate = pred;
        }
    }

    public class SyntaxTagBuilder {
        private readonly TypeFrame types;

        private IReadOnlyList<VariableCapture> CapturedVariables = Array.Empty<VariableCapture>();
        private ISyntaxPredicate Predicate = ISyntaxPredicate.Empty;
        private HelixType ReturnType = PrimitiveType.Void;
        private LifetimeBounds Bounds = new LifetimeBounds();

        public static SyntaxTagBuilder AtFrame(TypeFrame types) {
            return new SyntaxTagBuilder(types);
        }

        public static SyntaxTagBuilder AtFrame(TypeFrame types, ISyntaxTree syntax) {
            var tag = types.SyntaxTags[syntax];

            return AtFrame(types)
                .WithLifetimes(tag.Bounds)
                .WithCapturedVariables(tag.CapturedVariables)
                .WithPredicate(tag.Predicate)
                .WithReturnType(tag.ReturnType);
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

        public SyntaxTagBuilder WithLifetimes(LifetimeBounds bounds) {
            this.Bounds = bounds;

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
                this.Bounds, 
                this.CapturedVariables,
                this.Predicate);

            this.types.SyntaxTags[syntax] = tag;
        }
    }
}
