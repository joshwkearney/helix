namespace Trophy.Parsing {
    public enum NameTarget {
        Function, Variable, Struct, Union, Reserved
    }

    public class NamesRecorder {
        private int tempCounter = 0;
        private readonly Dictionary<IdentifierPath, NameTarget> names = new();
        
        public bool TrySetName(IdentifierPath scope, string name, NameTarget target) {
            scope = scope.Append(name);

            if (this.names.ContainsKey(scope)) {
                return false;
            }

            this.names[scope] = target;
            return true;
        }

        public Option<NameTarget> TryGetName(IdentifierPath name) {
            if (this.names.TryGetValue(name, out var value)) {
                return value;
            }

            return Option.None;
        }

        public Option<IdentifierPath> TryFindName(IdentifierPath scope, string name) {
            while (true) {
                var path = scope.Append(name);

                if (this.TryGetName(path).HasValue) {
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

        public string GetTempVariableName() {
            return "$trophy_names_temp_" + this.tempCounter++;
        }
    }
}
