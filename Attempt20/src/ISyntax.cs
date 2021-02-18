using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20 {
    public interface IParsedSyntax {
        public TokenLocation Location { get; }

        public IParsedSyntax CheckNames(INameRecorder names);

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types);
    }

    public interface ISyntax {
        public TokenLocation Location { get; }

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter);
    }

    public interface IParsedDeclaration {
        public TokenLocation Location { get; }

        public void DeclareNames(INameRecorder names);

        public void ResolveNames(INameRecorder names);

        public void DeclareTypes(INameRecorder names, ITypeRecorder types);

        public IDeclaration ResolveTypes(INameRecorder names, ITypeRecorder types);
    }

    public interface IDeclaration {
        public TokenLocation Location { get; }

        public void GenerateCode(ICWriter declWriter);
    }
}