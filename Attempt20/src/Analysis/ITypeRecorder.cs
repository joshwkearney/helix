using Attempt20.Analysis.Types;

namespace Attempt20.Analysis {
    public interface ITypeRecorder {
        public void DeclareVariable(IdentifierPath path, VariableInfo info);

        public void DeclareFunction(IdentifierPath path, FunctionSignature sig);

        public void DeclareStruct(IdentifierPath path, StructSignature sig);

        public IOption<VariableInfo> TryGetVariable(IdentifierPath path);

        public IOption<FunctionSignature> TryGetFunction(IdentifierPath path);

        public IOption<StructSignature> TryGetStruct(IdentifierPath path);

        public IOption<ISyntax> TryUnifyTo(ISyntax target, TrophyType newType);
    }
}