using System;
using System.Collections.Generic;
using Trophy.Analysis.Types;

namespace Trophy.Analysis {
    public delegate (TrophyType, IDeclarationC) MetaTypeGenerator(TrophyType[] args);

    public interface ITypeRecorder {
        public void DeclareVariable(IdentifierPath path, VariableInfo info);

        public void DeclareFunction(IdentifierPath path, FunctionSignature sig);

        public void DeclareStruct(IdentifierPath path, AggregateSignature sig);

        public void DeclareUnion(IdentifierPath path, AggregateSignature sig);

        public void DeclareMethodPath(TrophyType type, string name, IdentifierPath path);

        public void DeclareMetaType(MetaType meta, MetaTypeGenerator generator);

        public IOption<VariableInfo> TryGetVariable(IdentifierPath path);

        public IOption<FunctionSignature> TryGetFunction(IdentifierPath path);

        public IOption<AggregateSignature> TryGetStruct(IdentifierPath path);

        public IOption<AggregateSignature> TryGetUnion(IdentifierPath path);

        public IOption<IdentifierPath> TryGetMethodPath(TrophyType type, string name);

        public TrophyType InstantiateMetaType(MetaType type, TrophyType[] args);

        public IOption<ISyntaxC> TryUnifyTo(ISyntaxC target, TrophyType newType);

        public void PushFlow();

        public void PopFlow();
    }
}