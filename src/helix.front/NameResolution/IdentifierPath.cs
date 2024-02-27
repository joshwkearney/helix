using System.Collections.Immutable;

namespace Helix.Frontend.NameResolution {
    internal class IdentifierPath : IEquatable<IdentifierPath> {
        private readonly int hashCode;
        private readonly ImmutableList<string> segments;

        public ImmutableList<string> Segments {
            get => segments ?? [];
        }

        public bool IsEmpty => segments.IsEmpty;

        public IdentifierPath(params string[] segments) : this((IEnumerable<string>)segments) { }

        public IdentifierPath(IEnumerable<string> segments) {
            this.segments = segments.ToImmutableList();
            hashCode = segments.Aggregate(13, (x, y) => x + 7 * y.GetHashCode());
        }

        public IdentifierPath() : this([]) { }

        public IdentifierPath Append(string segment) {
            return new IdentifierPath(Segments.Add(segment));
        }

        public IdentifierPath Append(IdentifierPath path) {
            return new IdentifierPath(Segments.AddRange(path.Segments));
        }

        public IdentifierPath Pop() {
            if (Segments.IsEmpty) {
                return new IdentifierPath();
            }

            return new IdentifierPath(Segments.RemoveAt(Segments.Count - 1));
        }

        public bool StartsWith(IdentifierPath path) {
            if (path.Segments.Count > Segments.Count) {
                return false;
            }

            return path.Segments
                .Zip(Segments, (x, y) => x == y)
                .Aggregate(true, (x, y) => x && y);
        }

        public bool Equals(IdentifierPath? other) {
            if (other is null) {
                return false;
            }

            return Segments.SequenceEqual(other.Segments);
        }

        public override string ToString() {
            if (segments == null) {
                return "";
            }

            return string.Join("/", segments);
        }

        public override bool Equals(object? obj) {
            if (obj is IdentifierPath path) {
                return Equals(path);
            }

            return false;
        }

        public override int GetHashCode() => hashCode;

        public static bool operator ==(IdentifierPath path1, IdentifierPath path2) {
            return path1.Equals(path2);
        }

        public static bool operator !=(IdentifierPath path1, IdentifierPath path2) {
            return !path1.Equals(path2);
        }
    }
}