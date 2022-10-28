using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.Lifetimes {
    public interface ILifetimeBundle {
        public IReadOnlyDictionary<IdentifierPath, IReadOnlyList<Lifetime>> ComponentLifetimes { get; }

        public IReadOnlyList<Lifetime> ScalarLifetimes => this.ComponentLifetimes[new IdentifierPath()];

        public IEnumerable<Lifetime> AllLifetimes => this.ComponentLifetimes.SelectMany(x => x.Value);

        public ILifetimeBundle Merge(ILifetimeBundle other) {
            var lifetimes = this.ComponentLifetimes
                .Concat(other.ComponentLifetimes)
                .ToDictionary(x => x.Key, x => x.Value);

            return new LifetimeBundleWrapper(lifetimes);
        }

        private class LifetimeBundleWrapper : ILifetimeBundle {
            public IReadOnlyDictionary<IdentifierPath, IReadOnlyList<Lifetime>> ComponentLifetimes { get; }

            public LifetimeBundleWrapper(IReadOnlyDictionary<IdentifierPath, IReadOnlyList<Lifetime>> lifetimes) {
                this.ComponentLifetimes = lifetimes;
            }
        }
    }

    public class ScalarLifetimeBundle : ILifetimeBundle {
        public IReadOnlyDictionary<IdentifierPath, IReadOnlyList<Lifetime>> ComponentLifetimes { get; }

        public ScalarLifetimeBundle(IReadOnlyList<Lifetime> lifetimes) {
            this.ComponentLifetimes = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>() {
                { new IdentifierPath(), lifetimes }
            };
        }

        public ScalarLifetimeBundle(Lifetime lifetime) : this(new[] { lifetime }) { }

        public ScalarLifetimeBundle() : this(Array.Empty<Lifetime>()) { }
    }
}