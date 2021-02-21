using Attempt20.Analysis.Types;
using Attempt20.Parsing;

namespace Attempt20.Analysis {
    public interface ITypeRecorder {
        public void DeclareVariable(IdentifierPath path, VariableInfo info);

        public void DeclareFunction(IdentifierPath path, FunctionSignature sig);

        public void DeclareStruct(IdentifierPath path, AggregateSignature sig);

        public void DeclareUnion(IdentifierPath path, AggregateSignature sig);

        public void DeclareMethodPath(TrophyType type, string name, IdentifierPath path);

        public IOption<VariableInfo> TryGetVariable(IdentifierPath path);

        public IOption<FunctionSignature> TryGetFunction(IdentifierPath path);

        public IOption<AggregateSignature> TryGetStruct(IdentifierPath path);

        public IOption<AggregateSignature> TryGetUnion(IdentifierPath path);

        public IOption<IdentifierPath> TryGetMethodPath(TrophyType type, string name);

        public IOption<ISyntaxC> TryUnifyTo(ISyntaxC target, TrophyType newType);
    }
}