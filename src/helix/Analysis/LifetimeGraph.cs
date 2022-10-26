using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Helix.Analysis {
    public class LifetimeGraph {
        private ImmutableHashSet<Lifetime> allLifetimes = ImmutableHashSet.Create<Lifetime>();
        private readonly Dictionary<Lifetime, List<Lifetime>> parentLifetimes = new();
        private readonly Dictionary<Lifetime, List<Lifetime>> childLifetimes = new();

        public IReadOnlySet<Lifetime> AllLifetimes => this.allLifetimes;

        public void AddPrecursor(Lifetime childLifetime, Lifetime parentLifetime) {
            this.allLifetimes = this.allLifetimes.Add(childLifetime).Add(parentLifetime);

            if (!this.parentLifetimes.TryGetValue(childLifetime, out var parentList)) {
                this.parentLifetimes[childLifetime] = parentList = new();
            }

            parentList.Add(parentLifetime);
        }

        public void AddDerived(Lifetime parentLifetime, Lifetime childLifetime) {
            this.allLifetimes = this.allLifetimes.Add(childLifetime).Add(parentLifetime);

            if (!this.childLifetimes.TryGetValue(parentLifetime, out var childList)) {
                this.childLifetimes[parentLifetime] = childList = new();
            }

            childList.Add(childLifetime);
        }

        public void AddRoot(Lifetime root) {
            this.AddDerived(root, root);
            this.AddPrecursor(root, root);
        }

        public IReadOnlySet<Lifetime> GetDerivedLifetimes(Lifetime time, IReadOnlySet<Lifetime> roots) {
            var visited = new HashSet<Lifetime>();
            var stack = new Stack<Lifetime>(new[] { time });
            var children = new HashSet<Lifetime>();

            while (stack.Count > 0) {
                var item = stack.Pop();

                if (visited.Contains(item)) {
                    continue;
                }
                else {
                    visited.Add(item);
                }

                if (roots.Contains(item)) {
                    // We found a child lifetime that is in our root set, so return
                    // it and be done
                    children.Add(item);
                }
                else if (this.childLifetimes.TryGetValue(item, out var list)) {
                    // Transitively search the children of our children
                    foreach (var child in list) {
                        stack.Push(child);
                    }
                }
                else {
                    // Here we have found a child lifetime that has no other children and
                    // is also not in our root set, which is a problem. But, this lifetime
                    // may be derived from lifetimes that are in our root set, so we can 
                    // flip the search direction and try to express this lifetime's parents
                    // in terms of our roots.
                    var parents = GetPrecursorLifetimes(item, roots);

                    if (parents.All(roots.Contains)) {
                        // Success! Item has been expressed in terms of our roots, so we can
                        // just be dependent on those roots
                        foreach (var parent in parents) {
                            children.Add(parent);
                        }
                    }
                    else {
                        // Item is itself a root and cannot be further expressed in terms of 
                        // anything else, so just add it as a root and be done
                        children.Add(item);
                    }
                }
            }

            return children;
        }

        public IReadOnlySet<Lifetime> GetPrecursorLifetimes(Lifetime time, IReadOnlySet<Lifetime> roots) {
            var visited = new HashSet<Lifetime>();
            var stack = new Stack<Lifetime>(new[] { time });
            var parents = new HashSet<Lifetime>();

            while (stack.Count > 0) {
                var item = stack.Pop();

                if (visited.Contains(item)) {
                    continue;
                }

                if (roots.Contains(item) || !this.parentLifetimes.TryGetValue(item, out var list)) {
                    parents.Add(item);
                    visited.Add(item);
                }
                else {
                    foreach (var parent in list) {
                        stack.Push(parent);
                    }
                }
            }

            return parents;
        }
    }
}
