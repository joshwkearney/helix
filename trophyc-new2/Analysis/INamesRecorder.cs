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
}