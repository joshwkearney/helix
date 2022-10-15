using Trophy.Analysis.SyntaxTree;

namespace Trophy.Parsing.ParseTree {
    public interface IParseDeclaration {
        public TokenLocation Location { get; }

        public void DeclareNames(IdentifierPath scope, NamesRecorder names);

        public void DeclareTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types);

        public IDeclaration ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types);
    }
}