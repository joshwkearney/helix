using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt17.Experimental {
    public class Scope {
        private readonly IOption<Scope> head;

        public Dictionary<IdentifierPath, VariableInfo> Variables { get; } = new Dictionary<IdentifierPath, VariableInfo>();

        public Dictionary<IdentifierPath, FunctionInfo> Functions { get; } = new Dictionary<IdentifierPath, FunctionInfo>();

        public IdentifierPath Path { get; }

        public Scope() {
            this.head = Option.None<Scope>();
            this.Path = new IdentifierPath();
        }

        private Scope(Scope head, IdentifierPath path) {
            this.head = Option.Some(head);
            this.Path = path;
        }

        public Scope GetFrame(Func<IdentifierPath, IdentifierPath> pathSelector) {
            return new Scope(this, pathSelector(this.Path));
        }

        public bool IsPathTaken(IdentifierPath path) {
            if (this.Variables.ContainsKey(path)) {
                return true;
            }

            if (this.Functions.ContainsKey(path)) {
                return true;
            }

            if (this.head.TryGetValue(out var head) && head.IsPathTaken(path)) {
                return true;
            }

            return false;
        }

        public bool IsNameTaken(string name) {            
            return this.GetPossiblePaths(name).Select(this.IsPathTaken).Aggregate((x, y) => x || y);
        }

        public IOption<VariableInfo> FindVariable(string name) {
            var paths = this.GetPossiblePaths(name);

            // Try the closest variables first
            foreach (var path in paths) {
                if (this.Variables.TryGetValue(path, out var info)) {
                    return Option.Some(info);
                }
            }

            // Next try the head's variables
            if (this.head.TryGetValue(out var head)) {
                if (head.FindVariable(name).TryGetValue(out var info)) {
                    return Option.Some(info);
                }
            }

            return Option.None<VariableInfo>();
        }

        public IOption<FunctionInfo> FindFunction(string name) {
            var paths = this.GetPossiblePaths(name);

            // Try the closest variables first
            foreach (var path in paths) {
                if (this.Functions.TryGetValue(path, out var info)) {
                    return Option.Some(info);
                }
            }

            // Next try the head's variables
            if (this.head.TryGetValue(out var head)) {
                if (head.FindFunction(name).TryGetValue(out var info)) {
                    return Option.Some(info);
                }
            }

            return Option.None<FunctionInfo>();
        }

        public IOption<FunctionInfo> FindFunction(IdentifierPath path) {
            if (this.Functions.TryGetValue(path, out var info)) {
                return Option.Some(info);
            }

            // Next try the head's variables
            if (this.head.TryGetValue(out var head)) {
                if (head.FindFunction(path).TryGetValue(out info)) {
                    return Option.Some(info);
                }
            }

            return Option.None<FunctionInfo>();
        }

        private List<IdentifierPath> GetPossiblePaths(string name) {
            var allPaths = new List<IdentifierPath>();
            var basePath = this.Path;

            while (true) {
                allPaths.Add(basePath.Append(name));

                if (basePath == new IdentifierPath()) {
                    break;
                }

                basePath = basePath.Pop();
            }

            return allPaths;
        }
    }
}