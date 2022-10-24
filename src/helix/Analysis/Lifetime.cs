using Helix.Parsing;
using System.IO;

namespace Helix.Analysis {
    public record Lifetime(bool IsStackBound,
                           IReadOnlyList<IdentifierPath> Dependencies,
                           IReadOnlyList<ISyntaxTree> Values) {

        public Lifetime() : this(false, Array.Empty<IdentifierPath>(), Array.Empty<ISyntaxTree>()) { }

        public Lifetime Merge(Lifetime other) {
            var automatic = IsStackBound || other.IsStackBound;
            var origins = Dependencies.Concat(other.Dependencies).ToArray();
            var values = Values.Concat(other.Values).ToArray();

            return new Lifetime(automatic, origins, values);
        }

        public Lifetime WithStackBinding(bool isStackBound) {
            return new Lifetime(isStackBound, Dependencies, Values);
        }

        public Lifetime AppendOrigin(IdentifierPath origins) {
            return new Lifetime(
                IsStackBound,
                Dependencies.Append(origins).ToArray(),
                Values);
        }

        public bool HasCompatibleRoots(Lifetime assignValue, SyntaxFrame types) {
            if (!this.IsStackBound && assignValue.IsStackBound) {
                return false;
            }

            var targetRoots = GetRootOrigins(this.Dependencies, types).ToArray();
            var assignRoots = GetRootOrigins(assignValue.Dependencies, types).ToArray();

            if (!targetRoots.Any()) {
                return true;
            }

            return !assignRoots.Except(targetRoots).Any();        
        }

        public static IEnumerable<IdentifierPath> GetRootOrigins(
            IEnumerable<IdentifierPath> paths,
            SyntaxFrame types) {

            return paths
                .Select(x => new { Path = x, Lifetime = types.Variables[x].Lifetime })
                .Where(x => x.Lifetime.Dependencies.Count == 1)
                .Where(x => x.Lifetime.Dependencies[0] == x.Path)
                .Select(x => x.Path);
        }
    }
}