using Helix.Parsing;

namespace Helix.Analysis {
    public record Lifetime(bool IsStackBound,
                           IReadOnlyList<IdentifierPath> Origins,
                           IReadOnlyList<ISyntaxTree> Values) {

        public Lifetime() : this(false, Array.Empty<IdentifierPath>(), Array.Empty<ISyntaxTree>()) { }

        public Lifetime Merge(Lifetime other) {
            var automatic = IsStackBound || other.IsStackBound;
            var origins = Origins.Concat(other.Origins).ToArray();
            var values = Values.Concat(other.Values).ToArray();

            return new Lifetime(automatic, origins, values);
        }

        public Lifetime WithStackBinding(bool isStackBound) {
            return new Lifetime(isStackBound, Origins, Values);
        }

        public Lifetime AppendOrigin(IdentifierPath origins) {
            return new Lifetime(
                IsStackBound,
                Origins.Append(origins).ToArray(),
                Values);
        }

        public bool HasCompatibleOrigins(Lifetime assignValue, SyntaxFrame types) {
            if (!this.IsStackBound && assignValue.IsStackBound) {
                return false;
            }

            var targetRoots = GetRootOrigins(this.Origins, types).ToArray();
            var assignRoots = GetRootOrigins(assignValue.Origins, types).ToArray();

            if (!targetRoots.Any()) {
                return true;
            }

            return !assignRoots.Except(targetRoots).Any();        
        }

        public static IEnumerable<IdentifierPath> GetRootOrigins(
            IEnumerable<IdentifierPath> paths,
            SyntaxFrame types) {

            var stack = new Stack<IdentifierPath>(paths);

            while (stack.Count > 0) {
                var path = stack.Pop();
                var sig = types.Variables[path];

                if (sig.Lifetime.Origins.Count == 1 && sig.Lifetime.Origins[0] == path) {
                    yield return path;
                    continue;
                }

                foreach (var newPath in sig.Lifetime.Origins) {
                    stack.Push(newPath);
                }
            }
        }
    }
}