using Attempt16.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Attempt16.Analysis {
    public struct IdentifierPath : IEquatable<IdentifierPath> {
        private readonly ImmutableList<string> segments;

        public ImmutableList<string> Segments {
            get => this.segments ?? ImmutableList<string>.Empty;
        }

        public IdentifierPath(params string[] segments) {
            this.segments = segments.ToImmutableList();
        }

        public IdentifierPath(IEnumerable<string> segments) {
            this.segments = segments.ToImmutableList();
        }

        public IdentifierPath Append(string segment) {
            return new IdentifierPath(this.Segments.Add(segment));
        }

        public IdentifierPath Append(IdentifierPath path) {
            return new IdentifierPath(this.Segments.AddRange(path.Segments));
        }

        public IdentifierPath Pop() {
            if (this.Segments.IsEmpty) {
                throw new Exception();
            }
            else {
                return new IdentifierPath(this.Segments.RemoveAt(this.Segments.Count - 1));
            }
        }

        public bool StartsWith(IdentifierPath path) {
            if (path.Segments.Count > this.Segments.Count) {
                return false;
            }

            return path.Segments
                .Zip(this.Segments, (x, y) => x == y)
                .Aggregate(true, (x, y) => x && y);
        }

        public bool IsPathToVariable() {
            return this.Segments.Any() && this.Segments.First() == "%var";
        }

        public bool Equals(IdentifierPath other) {
            return this.Segments.SequenceEqual(other.Segments);
        }

        public override string ToString() {
            return string.Join(".", this.segments);
        }

        public override bool Equals(object obj) {
            if (obj is IdentifierPath path) {
                return this.Equals(path);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.Segments.Aggregate(7, (x, y) => x + 23 * y.GetHashCode());
        }

        public static bool operator==(IdentifierPath path1, IdentifierPath path2) {
            return path1.Equals(path2);
        }

        public static bool operator !=(IdentifierPath path1, IdentifierPath path2) {
            return !path1.Equals(path2);
        }
    }

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