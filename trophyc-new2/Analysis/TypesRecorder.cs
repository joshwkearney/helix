//using Trophy.Features.Aggregates;
//using Trophy.Features.Functions;
//using Trophy.Parsing;

//namespace Trophy.Analysis {
//    public enum NameTarget {
//        Function, Variable, Aggregate, Reserved
//    }

//    public class TypesRecorder46 {
//        private int tempCounter = 0;
//        private readonly Dictionary<IdentifierPath, NameTarget> names = new() {
//            { new IdentifierPath("int"), NameTarget.Reserved },
//            { new IdentifierPath("bool"), NameTarget.Reserved },
//            { new IdentifierPath("void"), NameTarget.Reserved }
//        };

//        private readonly Dictionary<IdentifierPath, VariableInfo> variables = new();
//        private readonly Dictionary<IdentifierPath, FunctionSignature> functions = new();
//        private readonly Dictionary<IdentifierPath, AggregateSignature> aggregates = new();
//        private readonly Dictionary<ISyntaxTree, TrophyType> returnTypes = new();

//        public bool TrySetNameTarget(IdentifierPath scope, string name, NameTarget target) {
//            scope = scope.Append(name);

//            if (this.names.ContainsKey(scope)) {
//                return false;
//            }

//            this.names[scope] = target;
//            return true;
//        }

//        public Option<NameTarget> TryGetNameTarget(IdentifierPath name) {
//            if (this.names.TryGetValue(name, out var value)) {
//                return value;
//            }

//            return Option.None;
//        }

//        public Option<IdentifierPath> TryFindPath(IdentifierPath scope, string name) {
//            while (true) {
//                var path = scope.Append(name);

//                if (this.TryGetNameTarget(path).HasValue) {
//                    return path;
//                }

//                if (scope.Segments.Any()) {
//                    scope = scope.Pop();
//                }
//                else {
//                    return Option.None;
//                }
//            }
//        }

//        public string GetTempVariableName() {
//            return "$trophy_names_temp_" + this.tempCounter++;
//        }

//        public void SetVariable(IdentifierPath path, TrophyType type, bool isWritable) {
//            variables[path] = new VariableInfo() { Type = type, IsWritable = isWritable };
//        }

//        public TrophyType GetVariableType(IdentifierPath path) {
//            return variables[path].Type;
//        }

//        public bool GetVariableWritablility(IdentifierPath path) {
//            return variables[path].IsWritable;
//        }

//        public void SetFunction(FunctionSignature sig) {
//            functions[sig.Path] = sig;
//        }

//        public FunctionSignature GetFunction(IdentifierPath path) {
//            return functions[path];
//        }

//        public void SetAggregate(AggregateSignature sig) {
//            aggregates[sig.Path] = sig;
//        }

//        public AggregateSignature GetAggregate(IdentifierPath path) {
//            return this.aggregates[path];
//        }

//        public void SetReturnType(ISyntaxTree tree, TrophyType type) {
//            this.returnTypes[tree] = type;
//        }

//        public TrophyType GetReturnType(ISyntaxTree tree) {
//            return this.returnTypes[tree];
//        }

//        private struct VariableInfo {
//            public TrophyType Type { get; set; }

//            public bool IsWritable { get; set; }
//        }
//    }
//}
