namespace Trophy.Analysis {
    public enum NameTarget {
        Function, Variable, Aggregate, Reserved
    }

    public interface INamesRecorder {
        public IdentifierPath CurrentScope { get; }

        public INamesRecorder WithScope(IdentifierPath newScope);

        public bool DeclareName(IdentifierPath path, NameTarget target);

        public Option<NameTarget> TryResolveName(IdentifierPath path);

        // Mixins
        public INamesRecorder WithScope(string name) {
            var scope = this.CurrentScope.Append(name);

            return this.WithScope(scope);
        }

        public bool DeclareName(string name, NameTarget target) {
            var path = this.CurrentScope.Append(name);

            return this.DeclareName(path, target);
        }

        public Option<IdentifierPath> TryFindPath(string name) {
            var scope = this.CurrentScope.Append(name);

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
}