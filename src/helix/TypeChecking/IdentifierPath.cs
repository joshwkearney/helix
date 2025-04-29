using System.Collections.Immutable;

namespace Helix.TypeChecking;

public class IdentifierPath : IEquatable<IdentifierPath> {
    private readonly int hashCode;
    private readonly ImmutableList<string> segments;

    public ImmutableList<string> Segments {
        get => this.segments ?? ImmutableList<string>.Empty;
    }

    public bool IsEmpty => !this.segments.Any();

    public IdentifierPath(params string[] segments) : this((IEnumerable<string>)segments) { }

    public IdentifierPath(IEnumerable<string> segments) {
        this.segments = segments.ToImmutableList();
        this.hashCode = segments.Aggregate(13, (x, y) => x + 7 * y.GetHashCode());
    }

    public IdentifierPath() : this(Array.Empty<string>()) { }

    public IdentifierPath Append(string segment) {
        return new IdentifierPath(this.Segments.Add(segment));
    }

    public IdentifierPath Append(IdentifierPath path) {
        return new IdentifierPath(this.Segments.AddRange(path.Segments));
    }

    public IdentifierPath Pop() {
        if (this.Segments.IsEmpty) {
            return new IdentifierPath();
        }
                
        return new IdentifierPath(this.Segments.RemoveAt(this.Segments.Count - 1));
    }

    public bool StartsWith(IdentifierPath path) {
        if (path.Segments.Count > this.Segments.Count) {
            return false;
        }

        return path.Segments
            .Zip(this.Segments, (x, y) => x == y)
            .Aggregate(true, (x, y) => x && y);
    }

    public bool Equals(IdentifierPath other) {
        return this.Segments.SequenceEqual(other.Segments);
    }

    public override string ToString() {
        if (this.segments == null) {
            return "";
        }

        return string.Join("/", this.segments);
    }

    public string ToCName() {
        if (this.segments == null) {
            return "";
        }

        return string.Join('$', this.segments);
    }

    public override bool Equals(object obj) {
        if (obj is IdentifierPath path) {
            return this.Equals(path);
        }

        return false;
    }

    public override int GetHashCode() => this.hashCode;

    public static bool operator ==(IdentifierPath path1, IdentifierPath path2) {
        return path1.Equals(path2);
    }

    public static bool operator !=(IdentifierPath path1, IdentifierPath path2) {
        return !path1.Equals(path2);
    }
}