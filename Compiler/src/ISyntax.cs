using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy {
    public enum VariableUsageKind {
        Captured, CapturedAndMutated, Region
    }

    public interface ISyntaxA {
        public TokenLocation Location { get; }

        public ISyntaxB CheckNames(INamesRecorder names);

        public IOption<ITrophyType> ResolveToType(INamesRecorder names) => Option.None<ITrophyType>();
    }

    public interface ISyntaxB {
        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage { get; }

        public ISyntaxC CheckTypes(ITypesRecorder types);
    }

    public interface ISyntaxC {
        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter);
    }

    public interface IDeclarationA {
        public TokenLocation Location { get; }

        public IDeclarationA DeclareNames(INamesRecorder names);

        public IDeclarationB ResolveNames(INamesRecorder names);
    }

    public interface IDeclarationB {
        public IDeclarationB DeclareTypes(ITypesRecorder types);

        public IDeclarationC ResolveTypes(ITypesRecorder types);
    }

    public interface IDeclarationC {
        public void GenerateCode(ICWriter writer);
    }
}