﻿using System;
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
}