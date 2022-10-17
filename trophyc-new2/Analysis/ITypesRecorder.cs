using Trophy.Features.Aggregates;
using Trophy.Features.Functions;
using Trophy.Features.Variables;
using Trophy.Parsing;

namespace Trophy.Analysis {
    public interface ITypesRecorder : INamesRecorder {
        public new ITypesRecorder WithScope(IdentifierPath newScope);

        public bool DeclareFunction(FunctionSignature sig);

        public bool DeclareVariable(VariableSignature sig);

        public bool DeclareAggregate(AggregateSignature sig);

        public void DeclareReserved(IdentifierPath path);

        public void SetReturnType(ISyntaxTree tree, TrophyType type);

        public FunctionSignature GetFunction(IdentifierPath path);

        public VariableSignature GetVariable(IdentifierPath path);

        public AggregateSignature GetAggregate(IdentifierPath path);

        public bool IsReserved(IdentifierPath path);

        public TrophyType GetReturnType(ISyntaxTree tree);

        public string GetVariableName();
    }
}