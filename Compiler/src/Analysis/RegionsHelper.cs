using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trophy.Analysis;

namespace Trophy.Analysis {
    public static class RegionsHelper {
        public static bool IsStack(IdentifierPath path) {
            return path.Segments.Any() && path.Segments.Last() == "stack";
        }

        public static IdentifierPath GetClosestHeap(IdentifierPath path) {
            if (!path.Segments.Any()) {
                return path;
            }

            if (IsStack(path)) {
                return GetClosestHeap(path.Pop());
            }
            else {
                return path;
            }
        }

        public static IdentifierPath GetClosestStack(IdentifierPath path) {
            if (!path.Segments.Any()) {
                return path;
            }

            if (IsStack(path)) {
                return path;
            }
            else {
                return GetClosestStack(path.Pop());
            }
        }
    }
}
