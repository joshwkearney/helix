using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy {
    public interface ISyntaxA {
        public TokenLocation Location { get; }

        public ISyntaxB CheckNames(INameRecorder names);
    }

    public interface ISyntaxB {
        public TokenLocation Location { get; }

        public ISyntaxC CheckTypes(ITypeRecorder types);
    }

    public interface ISyntaxC {
        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter);
    }

    public interface IDeclarationA {
        public TokenLocation Location { get; }

        public IDeclarationA DeclareNames(INameRecorder names);

        public IDeclarationB ResolveNames(INameRecorder names);
    }

    public interface IDeclarationB {
        public IDeclarationB DeclareTypes(ITypeRecorder types);

        public IDeclarationC ResolveTypes(ITypeRecorder types);
    }

    public interface IDeclarationC {
        public void GenerateCode(ICWriter writer);
    }
}