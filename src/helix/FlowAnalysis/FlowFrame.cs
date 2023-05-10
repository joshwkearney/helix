using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features;
using Helix.Features.Aggregates;
using Helix.Features.Functions;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Parsing;
using System.Runtime.CompilerServices;

namespace Helix.Analysis {
    public interface ITypedFrame {
        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }
    }

    public class FlowFrame : ITypedFrame {
        // General things
        public IDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

        public IDictionary<ISyntaxTree, LifetimeBundle> Lifetimes { get; }

        public LifetimeGraph LifetimeGraph { get; }

        public IDictionary<IdentifierPath, VariableSignature> Variables { get; }

        public IDictionary<IdentifierPath, FunctionSignature> Functions { get; }

        public IDictionary<IdentifierPath, StructSignature> Structs { get; }

        // Frame-specific things
        //public IDictionary<IdentifierPath, Lifetime> VariableLifetimes { get; }

        public FlowFrame(EvalFrame frame) {
            this.ReturnTypes = frame.ReturnTypes;
            this.Variables = frame.Variables;
            this.Functions = frame.Functions;
            this.Structs = frame.Structs;

            this.LifetimeGraph = new();
            this.Lifetimes = new Dictionary<ISyntaxTree, LifetimeBundle>();
            //this.VariableLifetimes = new Dictionary<IdentifierPath, Lifetime>();
        }

        public FlowFrame(FlowFrame prev) {
            this.ReturnTypes = prev.ReturnTypes;
            this.Variables = prev.Variables;
            this.Functions = prev.Functions;
            this.Structs = prev.Structs;

            this.LifetimeGraph = prev.LifetimeGraph;
            this.Lifetimes = prev.Lifetimes;
            //this.VariableLifetimes = new StackedDictionary<IdentifierPath, Lifetime>(prev.VariableLifetimes);
        }
    }
}