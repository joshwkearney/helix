using System;
using System.Collections.Generic;
using Trophy.Analysis.Types;

namespace Trophy.Analysis {
    public delegate (ITrophyType, IDeclarationC) MetaTypeGenerator(ITrophyType[] args);

    public interface ITypeRecorder {
        public void DeclareVariable(IdentifierPath path, VariableInfo info);

        public void DeclareFunction(IdentifierPath path, FunctionSignature sig);

        public void DeclareStruct(IdentifierPath path, AggregateSignature sig);

        public void DeclareUnion(IdentifierPath path, AggregateSignature sig);

        public void DeclareMethodPath(ITrophyType type, string name, IdentifierPath path);

        public void DeclareMetaType(GenericType meta, MetaTypeGenerator generator);

        public IOption<VariableInfo> TryGetVariable(IdentifierPath path);

        public IOption<FunctionSignature> TryGetFunction(IdentifierPath path);

        public IOption<AggregateSignature> TryGetStruct(IdentifierPath path);

        public IOption<AggregateSignature> TryGetUnion(IdentifierPath path);

        public IOption<IdentifierPath> TryGetMethodPath(ITrophyType type, string name);

        public ITrophyType InstantiateMetaType(GenericType type, ITrophyType[] args);

        public IOption<ISyntaxC> TryUnifyTo(ISyntaxC target, ITrophyType newType);

        public void PushFlow();

        public void PopFlow();
    }
}