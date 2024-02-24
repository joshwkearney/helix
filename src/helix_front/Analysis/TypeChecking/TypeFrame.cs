using Helix.Syntax;
using Helix.Analysis.Flow;
using Helix.Analysis.Types;
using Helix.Features;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using Helix.Generation;
using Helix.Parsing;
using Helix.Collections;
using System.Collections.Immutable;
using Helix.Analysis.Predicates;
using Helix.Features.Types;
using Helix.HelixMinusMinus;

namespace Helix.Analysis.TypeChecking {
    public class TypeFrame {
        private int tempCounter = 0;

        // Frame-specific things
        //public IdentifierPath Scope { get; }

        public ImmutableDictionary<string, LocalInfo> Locals { get; set; }

        public ImmutableHashSet<ISyntaxPredicate> AppliedPredicates { get; set; }

        public FunctionType CurrentFunction { get; set; } = null;

        // Global things
        //public Dictionary<IdentifierPath, HelixType> NominalSignatures { get; }

        public ControlFlowGraph ControlFlow { get; }

        // public Dictionary<ISyntaxTree, SyntaxTag> SyntaxTags { get; }

        // public Dictionary<IHmmStatement, SyntaxTag> HmmTags { get; }

        public TypeFrame() {
            this.Locals = ImmutableDictionary<string, LocalInfo>.Empty;

            this.Locals = this.Locals.Add(
                "void",
                new LocalInfo(PrimitiveType.Void));

            this.Locals = this.Locals.Add(
                "word",
                new LocalInfo(PrimitiveType.Word));

            this.Locals = this.Locals.Add(
                "bool",
                new LocalInfo(PrimitiveType.Bool));

            //this.NominalSignatures = new Dictionary<IdentifierPath, HelixType>();
            //this.Scope = new IdentifierPath();
            this.ControlFlow = new ControlFlowGraph();
            //this.SyntaxTags = new Dictionary<ISyntaxTree, SyntaxTag>();
            //this.HmmTags = new Dictionary<IHmmStatement, SyntaxTag>();
            this.AppliedPredicates = ImmutableHashSet<ISyntaxPredicate>.Empty;
        }

        public TypeFrame(TypeFrame prev) {
            //this.Scope = prev.Scope;
            this.ControlFlow = prev.ControlFlow;

            //this.SyntaxTags = prev.SyntaxTags;
            //this.NominalSignatures = prev.NominalSignatures;

            this.Locals = prev.Locals;
            this.AppliedPredicates = prev.AppliedPredicates;
        }

        //public TypeFrame(TypeFrame prev, string scopeSegment) : this(prev) {
        //    this.Scope = prev.Scope.Append(scopeSegment);
        //}

        //public TypeFrame(TypeFrame prev, IdentifierPath newScope) : this(prev) {
        //    this.Scope = newScope;
        //}

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }
    }
}