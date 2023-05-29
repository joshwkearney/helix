//using Helix.Syntax;
//using Helix.Analysis.Types;
//using Helix.Features.Aggregates;
//using Helix.Features.Functions;
//using Helix.Analysis.TypeChecking;
//using Helix.Features.Types;
//using System.Collections.Immutable;

//namespace Helix.Analysis {
//    public interface ITypeContext {
//        public IReadOnlyDictionary<IdentifierPath, ISyntaxTree> GlobalSyntaxValues { get; }

//        public IReadOnlyDictionary<IdentifierPath, HelixType> GlobalNominalSignatures { get; }

//        public IReadOnlyDictionary<ISyntaxTree, HelixType> ReturnTypes { get; }

//        public IReadOnlyDictionary<ISyntaxTree, IReadOnlyList<VariableCapture>> CapturedVariables { get; }
//    }
//}