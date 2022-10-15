using Trophy.Features.Aggregates;
using Trophy.Features.Functions;
using Trophy.Parsing.ParseTree;

namespace Trophy.Analysis {
    public class TypesRecorder {
        private Dictionary<IdentifierPath, VariableInfo> variables = new();
        private Dictionary<IdentifierPath, FunctionSignature> functions = new();
        private Dictionary<IdentifierPath, AggregateSignature> aggregates = new();
        private Dictionary<IParseTree, TrophyType> returnTypes = new();

        public void SetVariable(IdentifierPath path, TrophyType type, bool isWritable) {
            variables[path] = new VariableInfo() { Type = type, IsWritable = isWritable };
        }

        public TrophyType GetVariableType(IdentifierPath path) {
            return variables[path].Type;
        }

        public bool GetVariableWritablility(IdentifierPath path) {
            return variables[path].IsWritable;
        }

        public void SetFunction(FunctionSignature sig) {
            functions[sig.Path] = sig;
        }

        public FunctionSignature GetFunction(IdentifierPath path) {
            return functions[path];
        }

        public void SetAggregate(AggregateSignature sig) {
            aggregates[sig.Path] = sig;
        }

        public AggregateSignature GetAggregate(IdentifierPath path) {
            return this.aggregates[path];
        }

        public void SetReturnType(IParseTree tree, TrophyType type) {
            this.returnTypes[tree] = type;
        }

        public TrophyType GetReturnType(IParseTree tree) {
            return this.returnTypes.GetValueOrNone(tree).OrElse(() => PrimitiveType.Void);
        }

        private struct VariableInfo {
            public TrophyType Type { get; set; }

            public bool IsWritable { get; set; }
        }
    }
}
