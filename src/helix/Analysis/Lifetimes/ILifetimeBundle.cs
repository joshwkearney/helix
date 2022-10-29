using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.Lifetimes {
    public interface ILifetimeBundle {
        public IReadOnlyDictionary<IdentifierPath, IReadOnlyList<Lifetime>> ComponentLifetimes { get; }

        public IReadOnlyList<Lifetime> ScalarLifetimes => this.ComponentLifetimes[new IdentifierPath()];

        public IEnumerable<Lifetime> AllLifetimes => this.ComponentLifetimes.SelectMany(x => x.Value);

        public ILifetimeBundle Merge(ILifetimeBundle other) {
            var dict = this.ComponentLifetimes
                .Concat(other.ComponentLifetimes)
                .SelectMany(x => x.Value.Select(y => new { Key = x.Key, Value = y }))
                .GroupBy(x => x.Key)
                .ToDictionary(x => x.Key, x => (IReadOnlyList<Lifetime>)x.ToValueList());

            return new StructLifetimeBundle(dict);
        }
    }

    public class ScalarLifetimeBundle : ILifetimeBundle {
        public IReadOnlyDictionary<IdentifierPath, IReadOnlyList<Lifetime>> ComponentLifetimes { get; }

        public ScalarLifetimeBundle(IReadOnlyList<Lifetime> lifetimes) {
            this.ComponentLifetimes = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>() {
                { new IdentifierPath(), lifetimes }
            };
        }

        public ScalarLifetimeBundle(Lifetime lifetime) {
            this.ComponentLifetimes = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>() {
                { new IdentifierPath(), new[] { lifetime } }
            };
        }

        public ScalarLifetimeBundle() : this(new Lifetime()) {
            this.ComponentLifetimes = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>() {
                { new IdentifierPath(), Array.Empty<Lifetime>() }
            };
        }
    }

    public class StructLifetimeBundle : ILifetimeBundle {
        public IReadOnlyDictionary<IdentifierPath, IReadOnlyList<Lifetime>> ComponentLifetimes { get; }

        public StructLifetimeBundle(IReadOnlyDictionary<IdentifierPath, IReadOnlyList<Lifetime>> lifetimes) {
            this.ComponentLifetimes = lifetimes;
        }
    }
}