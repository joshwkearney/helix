using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Trophy.Features.Aggregates;
using Trophy.Features.Functions;
using Trophy.Parsing;

namespace Trophy.Analysis {
    public enum NameTarget {
        Function, Variable, Aggregate, Reserved
    }
    
    public record VariableSignature { 
        public TrophyType Type { get; }

        public bool IsWritable { get; }

        public IdentifierPath Path { get; }

        public VariableSignature(IdentifierPath path, TrophyType type, bool isWritable) {
            this.Path = path;
            this.Type = type;
            this.IsWritable = isWritable;
        }
    }

    public interface INamesObserver {
        public Option<NameTarget> TryResolveName(IdentifierPath path);

        // Mixins
        public Option<IdentifierPath> TryFindPath(IdentifierPath scope, string name) {
            scope = scope.Append(name);

            while (true) {
                var path = scope.Append(name);

                if (this.TryResolveName(path).TryGetValue(out var target)) {
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
    }

    public interface INamesRecorder : INamesObserver {
        public IdentifierPath CurrentScope { get; }

        public INamesRecorder WithScope(IdentifierPath newScope);

        public bool DeclareName(IdentifierPath path, NameTarget target);

        // Mixins
        public bool DeclareName(string name, NameTarget target) {
            var path = this.CurrentScope.Append(name);

            return this.DeclareName(path, target);
        }

        public INamesRecorder WithScope(string name) {
            var scope = this.CurrentScope.Append(name);

            return this.WithScope(scope);
        }
    }

    public interface ITypesObserver {      
        public FunctionSignature GetFunction(IdentifierPath path);

        public VariableSignature GetVariable(IdentifierPath path);

        public AggregateSignature GetAggregate(IdentifierPath path);

        public bool IsReserved(IdentifierPath path);

        public TrophyType GetReturnType(ISyntaxTree tree);
    }

    public interface ITypesRecorder : ITypesObserver, INamesRecorder {
        public new ITypesRecorder WithScope(IdentifierPath newScope);

        public bool DeclareFunction(FunctionSignature sig);

        public bool DeclareVariable(VariableSignature sig);

        public bool DeclareAggregate(AggregateSignature sig);

        public void DeclareReserved(IdentifierPath path);

        public void SetReturnType(ISyntaxTree tree, TrophyType type);

        public string GetVariableName();
    }

    public class NamesRecorder : INamesRecorder {
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

    public class TypesRecorder : ITypesRecorder {
        private int tempCounter = 0;

        private readonly Option<ITypesRecorder> prev;
        private readonly INamesRecorder names;

        private readonly Dictionary<IdentifierPath, NameTarget> nameTargets = new();
        private readonly Dictionary<IdentifierPath, FunctionSignature> functions = new();
        private readonly Dictionary<IdentifierPath, VariableSignature> variables = new();
        private readonly Dictionary<IdentifierPath, AggregateSignature> aggregates = new();
        private readonly Dictionary<ISyntaxTree, TrophyType> returnTypes = new();

        private readonly HashSet<IdentifierPath> reserved = new() {
            new IdentifierPath("int"), new IdentifierPath("bool"), 
            new IdentifierPath("void")
        };

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
            this.reserved.Add(path);
        }

        public void SetReturnType(ISyntaxTree tree, TrophyType type) {
            this.returnTypes[tree] = type;
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
            if (this.reserved.Contains(path)) {
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