using Attempt16.Types;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt16.Analysis {
    public class Scope {
        private ImmutableDictionary<IdentifierPath, ILanguageType> TypeVariables { get; }

        private ImmutableDictionary<IdentifierPath, VariableInfo> Variables { get; }

        public IdentifierPath Path { get; }

        public Scope() {
            this.TypeVariables = ImmutableDictionary<IdentifierPath, ILanguageType>.Empty;
            this.Variables = ImmutableDictionary<IdentifierPath, VariableInfo>.Empty;
            this.Path = new IdentifierPath();
        }

        public Scope(IdentifierPath path, ImmutableDictionary<IdentifierPath, ILanguageType> typeVariables, ImmutableDictionary<IdentifierPath, VariableInfo> variables) {
            this.Path = path;
            this.TypeVariables = typeVariables;
            this.Variables = variables;
        }

        public IOption<ILanguageType> FindType(string name) {
            var path = this.Path.Append("");

            while (!path.Segments.IsEmpty) {
                path = path.Pop();

                if (this.TypeVariables.TryGetValue(path.Append(name), out var type)) {
                    return Option.Some(type);
                }
            }

            return Option.None<ILanguageType>();
        }

        public IOption<ILanguageType> FindType(IdentifierPath path) {
            if (path.Segments.First() == "%var") {
                return this.FindType(new IdentifierPath(path.Segments.Skip(1))).Select(x => new VariableType(x));
            }
            else if (this.TypeVariables.TryGetValue(path, out var type)) {
                return Option.Some(type);
            }
            else {
                return Option.None<ILanguageType>();
            }
        }

        public IOption<VariableInfo> FindVariable(string name) {
            var path = this.Path.Append("blank");

            while (!path.Segments.IsEmpty) {
                path = path.Pop();

                if (this.Variables.TryGetValue(path.Append(name), out var info)) {
                    return Option.Some(info);
                }
            }

            return Option.None<VariableInfo>();
        }

        public Scope SelectPath(Func<IdentifierPath, IdentifierPath> func) {
            return new Scope(func(this.Path), this.TypeVariables, this.Variables);
        }

        public Scope AppendVariable(string name, VariableInfo info) {
            return new Scope(
                this.Path,
                this.TypeVariables,
                this.Variables.Add(this.Path.Append(name), info)
            );
        }

        public Scope AppendTypeVariable(string name, ILanguageType type) {
            return new Scope(
                this.Path,
                this.TypeVariables.Add(this.Path.Append(name), type),
                this.Variables
            );
        }
    }
}