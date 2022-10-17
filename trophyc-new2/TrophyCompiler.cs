using Trophy.Parsing;
using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.Features.Aggregates;
using Trophy.Features.Functions;
using Trophy.Features.Variables;
using Trophy.Analysis.Types;

namespace Trophy {
    public class TrophyCompiler {
        private readonly string input;

        public TrophyCompiler(string input) {
            this.input = input.Replace("\r\n", "\n").Replace('\r', '\n').Replace("\t", "    ");
        }

        public string Compile() {
            // ToList() is after each step so lazy evaluation doesn't mess
            // up the order of the steps

            try {
                var lexer = new Lexer(this.input);
                var parser = new Parser(lexer.GetTokens());
                var names = new NamesRecorder();
                var types = new TypesRecorder(names);
                var writer = new CWriter();

                var parseStats = parser.Parse();

                foreach (var stat in parseStats) {
                    stat.DeclareNames(names);
                }

                foreach (var stat in parseStats) {
                    stat.DeclareTypes(types);
                }

                var stats = parseStats.Select(x => x.CheckTypes(types)).ToArray();

                foreach (var stat in stats) {
                    stat.GenerateCode(writer);
                }

                return writer.ToString();
            }
            catch (TrophyException ex) {
                Console.WriteLine(ex.CreateConsoleMessage(this.input));

                return "";
            }
        }

        private class NamesRecorder : INamesRecorder {
            private readonly Option<INamesRecorder> prev;

            private readonly Dictionary<IdentifierPath, NameTarget> targets = new() {
                { new IdentifierPath("int"), NameTarget.Reserved },
                { new IdentifierPath("bool"), NameTarget.Reserved },
                { new IdentifierPath("void"), NameTarget.Reserved }
            };

            public IdentifierPath CurrentScope { get; }

            public NamesRecorder() {
                this.prev = Option.None;
                this.CurrentScope = new IdentifierPath();
            }

            private NamesRecorder(INamesRecorder prev, IdentifierPath scope) {
                this.prev = Option.Some(prev);
                this.CurrentScope = scope;
            }

            public INamesRecorder WithScope(IdentifierPath newScope) {
                return new NamesRecorder(this, newScope);
            }

            public bool DeclareName(IdentifierPath path, NameTarget target) {
                if (this.prev.TryGetValue(out var prev)) {
                    return prev.DeclareName(path, target);
                }

                if (this.targets.TryGetValue(path, out var old) && old != target) {
                    return false;
                }

                this.targets[path] = target;
                return true;
            }

            public Option<NameTarget> TryResolveName(IdentifierPath path) {
                if (this.prev.TryGetValue(out var prev)) {
                    return prev.TryResolveName(path);
                }

                return this.targets.GetValueOrNone(path);
            }
        }

        private class TypesRecorder : ITypesRecorder {
            private int tempCounter = 0;

            private readonly Option<ITypesRecorder> prev;
            private readonly INamesRecorder names;

            private readonly Dictionary<IdentifierPath, NameTarget> nameTargets = new();
            private readonly Dictionary<IdentifierPath, FunctionSignature> functions = new();
            private readonly Dictionary<IdentifierPath, VariableSignature> variables = new();
            private readonly Dictionary<IdentifierPath, AggregateSignature> aggregates = new();

            private readonly Dictionary<ISyntaxTree, TrophyType> returnTypes = new();

            public IdentifierPath CurrentScope { get; }

            public TypesRecorder(INamesRecorder names) {
                this.prev = Option.None;
                this.names = names;
                this.CurrentScope = new IdentifierPath();
            }

            private TypesRecorder(INamesRecorder names, ITypesRecorder prev, IdentifierPath newScope) {
                this.prev = Option.Some(prev);
                this.names = names;
                this.CurrentScope = newScope;
            }

            public ITypesRecorder WithScope(IdentifierPath newScope) {
                return new TypesRecorder(this.names, this, newScope);
            }

            INamesRecorder INamesRecorder.WithScope(IdentifierPath newScope) {
                return this.WithScope(newScope);
            }

            public bool DeclareName(IdentifierPath path, NameTarget target) {
                if (this.nameTargets.TryGetValue(path, out var old) && old != target) {
                    return false;
                }

                this.nameTargets[path] = target;
                return true;
            }

            public Option<NameTarget> TryResolveName(IdentifierPath path) {
                if (this.nameTargets.TryGetValue(path, out var value)) {
                    return value;
                }

                return this.names.TryResolveName(path);
            }

            public bool DeclareFunction(FunctionSignature sig) {
                if (!this.DeclareName(sig.Path, NameTarget.Function)) {
                    return false;
                }

                if (this.functions.TryGetValue(sig.Path, out var old) && old != sig) {
                    return false;
                }

                this.functions[sig.Path] = sig;
                return true;
            }

            public bool DeclareVariable(VariableSignature sig) {
                if (!this.DeclareName(sig.Path, NameTarget.Variable)) {
                    return false;
                }

                if (this.variables.TryGetValue(sig.Path, out var old) && old != sig) {
                    return false;
                }

                this.variables[sig.Path] = sig;
                return true;
            }

            public bool DeclareAggregate(AggregateSignature sig) {
                if (!this.DeclareName(sig.Path, NameTarget.Aggregate)) {
                    return false;
                }

                if (this.aggregates.TryGetValue(sig.Path, out var old) && old != sig) {
                    return false;
                }

                this.aggregates[sig.Path] = sig;
                return true;
            }

            public void DeclareReserved(IdentifierPath path) {
                this.DeclareName(path, NameTarget.Reserved);
            }

            public void SetReturnType(ISyntaxTree tree, TrophyType type) {
                if (this.prev.TryGetValue(out var prev)) {
                    prev.SetReturnType(tree, type);
                }
                else {
                    this.returnTypes[tree] = type;
                }
            }

            public FunctionSignature GetFunction(IdentifierPath path) {
                if (this.functions.TryGetValue(path, out var value)) {
                    return value;
                }

                if (this.prev.Select(x => x.GetFunction(path)).TryGetValue(out value)) {
                    return value;
                }

                throw new Exception();
            }

            public VariableSignature GetVariable(IdentifierPath path) {
                if (this.variables.TryGetValue(path, out var value)) {
                    return value;
                }

                if (this.prev.Select(x => x.GetVariable(path)).TryGetValue(out value)) {
                    return value;
                }

                throw new Exception();
            }

            public AggregateSignature GetAggregate(IdentifierPath path) {
                if (this.aggregates.TryGetValue(path, out var value)) {
                    return value;
                }

                if (this.prev.Select(x => x.GetAggregate(path)).TryGetValue(out value)) {
                    return value;
                }

                throw new Exception();
            }

            public bool IsReserved(IdentifierPath path) {
                if (this.nameTargets.TryGetValue(path, out var target) && target == NameTarget.Reserved) {
                    return true;
                }

                if (this.prev.Select(x => x.IsReserved(path)).HasValue) {
                    return true;
                }

                return false;
            }

            public TrophyType GetReturnType(ISyntaxTree tree) {
                if (this.returnTypes.TryGetValue(tree, out var value)) {
                    return value;
                }

                if (this.prev.Select(x => x.GetReturnType(tree)).TryGetValue(out value)) {
                    return value;
                }

                throw new Exception();
            }

            public string GetVariableName() {
                return "$t_" + this.tempCounter++;
            }
        }
    }
}
