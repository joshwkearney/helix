using Trophy.Analysis.Types;
using Trophy.Features;
using Trophy.Features.Aggregates;
using Trophy.Features.Functions;
using Trophy.Features.Variables;
using Trophy.Generation;
using Trophy.Parsing;
using static System.Formats.Asn1.AsnWriter;

namespace Trophy.Analysis {
    public interface ITypesRecorder {
        public Dictionary<TrophyType, DeclarationCG> TypeDeclarations { get; }

        public IdentifierPath CurrentScope { get; }
        public ITypesRecorder WithScope(IdentifierPath newScope);

        public Option<FunctionSignature> TryGetFunction(IdentifierPath path);
        public Option<VariableSignature> TryGetVariable(IdentifierPath path);
        public Option<AggregateSignature> TryGetAggregate(IdentifierPath path);

        public bool DeclareFunction(FunctionSignature sig);
        public bool DeclareVariable(VariableSignature sig);
        public bool DeclareAggregate(AggregateSignature sig);

        public TrophyType GetReturnType(ISyntax tree);
        public void SetReturnType(ISyntax tree, TrophyType type);

        public Option<ISyntax> TryGetValue(IdentifierPath path);
        public void SetValue(IdentifierPath path, ISyntax value);

        // Mixins
        public ISyntax GetValue(IdentifierPath path) {
            if (this.TryGetValue(path).TryGetValue(out var value)) {
                return value;
            }

            throw new InvalidOperationException(
                $"Compiler error: The path '{path}' does not contain a value.");
        }

        public Option<IdentifierPath> TryResolvePath(string name) {
            var scope = this.CurrentScope;

            while (true) {
                var path = scope.Append(name);
                if (this.TryGetValue(path).TryGetValue(out var _)) {
                    return path;
                }

                if (scope.Segments.Any()) {
                    scope = scope.Pop();
                }
                else {
                    return Option.None;
                }
            }
        }

        public IdentifierPath ResolvePath(string path) {
            if (this.TryResolvePath(path).TryGetValue(out var value)) {
                return value;
            }

            throw new InvalidOperationException(
                $"Compiler error: The path '{path}' does not contain a value.");
        }

        public Option<ISyntax> TryResolveValue(string name) {
            if (!this.TryResolvePath(name).TryGetValue(out var path)) {
                return Option.None;
            }

            return this.TryGetValue(path);
        }

        public ISyntax ResolveValue(string name) {
            return this.GetValue(this.ResolvePath(name));
        }

        public ITypesRecorder WithScope(string name) {
            var scope = this.CurrentScope.Append(name);

            return this.WithScope(scope);
        }

        public FunctionSignature GetFunction(IdentifierPath path) {
            if (this.TryGetFunction(path).TryGetValue(out var sig)) {
                return sig;
            }

            throw new InvalidOperationException();
        }

        public VariableSignature GetVariable(IdentifierPath path) {
            if (this.TryGetVariable(path).TryGetValue(out var sig)) {
                return sig;
            }

            throw new InvalidOperationException();
        }

        public AggregateSignature GetAggregate(IdentifierPath path) {
            if (this.TryGetAggregate(path).TryGetValue(out var sig)) {
                return sig;
            }

            throw new InvalidOperationException();
        }

        // TODO: Move this somewhere else
        public string GetVariableName();
    }

    public delegate void DeclarationCG(ICWriter writer);

    public class TypesRecorder : ITypesRecorder {
        private int tempCounter = 0;

        private readonly Option<ITypesRecorder> prev;
        //private readonly INamesRecorder names;

        //private readonly Dictionary<IdentifierPath, NameTarget> nameTargets = new();
        private readonly Dictionary<IdentifierPath, FunctionSignature> functions = new();
        private readonly Dictionary<IdentifierPath, VariableSignature> variables = new();
        private readonly Dictionary<IdentifierPath, AggregateSignature> aggregates = new();

        private readonly Dictionary<ISyntax, TrophyType> returnTypes = new();

        private readonly Dictionary<IdentifierPath, ISyntax> values = new() {
            { new IdentifierPath("void"), new TypeSyntax(default, PrimitiveType.Void) },
            { new IdentifierPath("int"), new TypeSyntax(default, PrimitiveType.Int) },
            { new IdentifierPath("bool"), new TypeSyntax(default, PrimitiveType.Bool) }
        };

        public IdentifierPath CurrentScope { get; }

        public Dictionary<TrophyType, DeclarationCG> TypeDeclarations { get; } = new();

        public TypesRecorder() {
            this.prev = Option.None;
            //this.names = names;
            this.CurrentScope = new IdentifierPath();
        }

        private TypesRecorder(ITypesRecorder prev, IdentifierPath newScope) {
            this.prev = Option.Some(prev);
            this.CurrentScope = newScope;
        }

        public ITypesRecorder WithScope(IdentifierPath newScope) {
            return new TypesRecorder(this, newScope);
        }

        //INamesRecorder INamesRecorder.WithScope(IdentifierPath newScope) {
        //    return this.WithScope(newScope);
        //}

        public void SetValue(IdentifierPath path, ISyntax target) {            
            this.values[path] = target;
        }

        public Option<ISyntax> TryGetValue(IdentifierPath path) {
            if (this.values.TryGetValue(path, out var value)) {
                return Option.Some(value);
            }

            if (this.prev.TryGetValue(out var prev)) {
                return prev.TryGetValue(path);
            }

            return Option.None;
        }

        public bool DeclareFunction(FunctionSignature sig) {
            if (this.functions.TryGetValue(sig.Path, out var old) && old != sig) {
                return false;
            }

            this.functions[sig.Path] = sig;
            return true;
        }

        public bool DeclareVariable(VariableSignature sig) {
            if (this.variables.TryGetValue(sig.Path, out var old) && old != sig) {
                return false;
            }

            this.variables[sig.Path] = sig;
            return true;
        }

        public bool DeclareAggregate(AggregateSignature sig) {
            if (this.aggregates.TryGetValue(sig.Path, out var old) && old != sig) {
                return false;
            }

            this.aggregates[sig.Path] = sig;
            return true;
        }

        public void SetReturnType(ISyntax tree, TrophyType type) {
            if (this.prev.TryGetValue(out var prev)) {
                prev.SetReturnType(tree, type);
            }
            else {
                this.returnTypes[tree] = type;
            }
        }

        public Option<FunctionSignature> TryGetFunction(IdentifierPath path) {
            if (this.functions.TryGetValue(path, out var value)) {
                return value;
            }

            if (this.prev.Select(x => x.GetFunction(path)).TryGetValue(out value)) {
                return value;
            }

            return Option.None;
        }

        public Option<VariableSignature> TryGetVariable(IdentifierPath path) {
            if (this.variables.TryGetValue(path, out var value)) {
                return value;
            }

            if (this.prev.TryGetValue(out var prev)) {
                return prev.TryGetVariable(path);
            }

            return Option.None;
        }

        public Option<AggregateSignature> TryGetAggregate(IdentifierPath path) {
            if (this.aggregates.TryGetValue(path, out var value)) {
                return value;
            }

            if (this.prev.Select(x => x.GetAggregate(path)).TryGetValue(out value)) {
                return value;
            }

            return Option.None;
        }

        public TrophyType GetReturnType(ISyntax tree) {
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