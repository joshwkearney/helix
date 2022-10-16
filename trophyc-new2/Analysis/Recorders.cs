using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Features.Aggregates;
using Trophy.Features.Functions;
using Trophy.Parsing;

namespace Trophy.Analysis {
    public enum NameTarget {
        Function, Variable, Aggregate, Reserved
    }

    public static class RecorderExtensions {
        public static void PushScope(this ISyntaxNavigator syntax, string name) {
            syntax.PushScope(syntax.CurrentScope.Append(name));
        }


        public static bool DeclareName(this INamesRecorder names, string name, NameTarget target) {
            var path = names.CurrentScope.Append(name);

            return names.DeclareName(path, target);
        }

        public static bool DeclareVariable(this ITypesRecorder types, string name, 
                                           TrophyType type, bool isWritable) {

            var path = types.CurrentScope.Append(name);
            var sig = new VariableSignature(path, type, isWritable);

            return types.DeclareVariable(sig);
        }

        public static bool DeclareVariable(this ITypesRecorder types, IdentifierPath path,
                                           TrophyType type, bool isWritable) {

            var sig = new VariableSignature(path, type, isWritable);

            return types.DeclareVariable(sig);
        }

        public static Option<NameTarget> TryResolveName(this INamesObserver names, string name) {
            return ResolveNameHelper(names, name).Select(x => x.target);
        }

        public static Option<IdentifierPath> TryFindPath(this INamesObserver names, string name) {
            return ResolveNameHelper(names, name).Select(x => x.path);
        }

        private static Option<(IdentifierPath path, NameTarget target)> ResolveNameHelper(INamesObserver names, string name) {
            var scope = names.CurrentScope.Append(name);

            while (true) {
                var path = scope.Append(name);

                if (names.TryResolveName(path).TryGetValue(out var target)) {
                    return (path, target);
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

    public struct VariableSignature { 
        public TrophyType Type { get; }

        public bool IsWritable { get; }

        public IdentifierPath Path { get; }

        public VariableSignature(IdentifierPath path, TrophyType type, bool isWritable) {
            this.Path = path;
            this.Type = type;
            this.IsWritable = isWritable;
        }
    }

    public interface ISyntaxNavigator {
        public IdentifierPath CurrentScope { get; }

        public void PushScope(IdentifierPath scope);

        public void PopScope();
    }

    public interface INamesObserver {
        public IdentifierPath CurrentScope { get; }

        public Option<NameTarget> TryResolveName(IdentifierPath path);
    }

    public interface INamesRecorder : INamesObserver, ISyntaxNavigator {
        public new IdentifierPath CurrentScope { get; }

        public bool DeclareName(IdentifierPath path, NameTarget target);
    }

    public interface ITypesObserver {      
        public FunctionSignature GetFunction(IdentifierPath path);

        public VariableSignature GetVariable(IdentifierPath path);

        public AggregateSignature GetAggregate(IdentifierPath path);

        public bool IsReserved(IdentifierPath path);

        public TrophyType GetReturnType(ISyntaxTree tree);
    }

    public interface ITypesRecorder : ITypesObserver, INamesObserver, ISyntaxNavigator {
        public new IdentifierPath CurrentScope { get; }

        public bool DeclareFunction(FunctionSignature sig);

        public bool DeclareVariable(VariableSignature sig);

        public bool DeclareAggregate(AggregateSignature sig);

        public bool DeclareReserved(IdentifierPath path);

        public void SetReturnType(ISyntaxTree tree, TrophyType type);

        public string GetVariableName();
    }

    public class NamesRecorder : INamesRecorder {
        private readonly Stack<IdentifierPath> scopes = new();

        private readonly Dictionary<IdentifierPath, NameTarget> targets = new() {
            { new IdentifierPath("int"), NameTarget.Reserved },
            { new IdentifierPath("bool"), NameTarget.Reserved },
            { new IdentifierPath("void"), NameTarget.Reserved }
        };

        public IdentifierPath CurrentScope => this.scopes.Peek();

        public NamesRecorder() {
            this.scopes.Push(new IdentifierPath());
        }

        public bool DeclareName(IdentifierPath path, NameTarget target) {
            if (this.targets.TryGetValue(path, out var old) && old != target) {
                return false;
            }

            this.targets[path] = target;
            return true;
        }

        public void PopScope() => this.scopes.Pop();

        public void PushScope(IdentifierPath scope) => this.scopes.Push(scope);

        public Option<NameTarget> TryResolveName(IdentifierPath path) {
            return this.targets.GetValueOrNone(path);
        }
    }

    public class TypesRecorder : ITypesRecorder {
        private int tempCounter = 0;

        private readonly INamesRecorder names;
        private readonly Stack<IdentifierPath> scopes = new();

        private readonly Dictionary<IdentifierPath, FunctionSignature> functions = new();
        private readonly Dictionary<IdentifierPath, VariableSignature> variables = new();
        private readonly Dictionary<IdentifierPath, AggregateSignature> aggregates = new();
        private readonly Dictionary<ISyntaxTree, TrophyType> returnTypes = new();

        private readonly HashSet<IdentifierPath> reserved = new() {
            new IdentifierPath("int"), new IdentifierPath("bool"), 
            new IdentifierPath("void")
        };

        public IdentifierPath CurrentScope => this.scopes.Peek();

        public TypesRecorder(INamesRecorder names) {
            this.names = names;
            this.scopes.Push(new IdentifierPath());
        }

        public void PopScope() => this.scopes.Pop();

        public void PushScope(IdentifierPath scope) => this.scopes.Push(scope);

        public Option<NameTarget> TryResolveName(IdentifierPath path) {
            return this.names.TryResolveName(path);
        }

        public bool DeclareFunction(FunctionSignature sig) {
            if (!names.DeclareName(sig.Path, NameTarget.Function)) {
                return false;
            }

            this.functions[sig.Path] = sig;
            return true;
        }

        public bool DeclareVariable(VariableSignature sig) {
            if (!names.DeclareName(sig.Path, NameTarget.Variable)) {
                return false;
            }

            this.variables[sig.Path] = sig;
            return true;
        }

        public bool DeclareAggregate(AggregateSignature sig) {
            if (!names.DeclareName(sig.Path, NameTarget.Aggregate)) {
                return false;
            }

            this.aggregates[sig.Path] = sig;
            return true;
        }

        public bool DeclareReserved(IdentifierPath path) {
            if (!names.DeclareName(path, NameTarget.Reserved)) {
                return false;
            }

            this.reserved.Add(path);
            return true;
        }

        public void SetReturnType(ISyntaxTree tree, TrophyType type) {
            this.returnTypes[tree] = type;
        }

        public FunctionSignature GetFunction(IdentifierPath path) {
            return this.functions[path];
        }

        public VariableSignature GetVariable(IdentifierPath path) {
            return this.variables[path];
        }

        public AggregateSignature GetAggregate(IdentifierPath path) {
            return this.aggregates[path];
        }

        public bool IsReserved(IdentifierPath path) {
            return this.reserved.Contains(path);
        }

        public TrophyType GetReturnType(ISyntaxTree tree) {
            return this.returnTypes[tree];
        }

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }
    }
}