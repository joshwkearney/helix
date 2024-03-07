using Helix.Common;
using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeStore {
        private ImmutableDictionary<IValueLocation, IHelixType> signatures 
            = ImmutableDictionary<IValueLocation, IHelixType>.Empty;

        private ImmutableDictionary<IValueLocation, IHelixType> values 
            = ImmutableDictionary<IValueLocation, IHelixType>.Empty;

        private TypeStore(
            ImmutableDictionary<IValueLocation, IHelixType> signatures, 
            ImmutableDictionary<IValueLocation, IHelixType> currentTypes) {

            this.signatures = signatures;
            this.values = currentTypes;
        }

        public TypeStore() { }

        public TypeStore CreateScope() {
            return new TypeStore(this.signatures, this.values);
        }

        public TypeStore MergeWith(TypeStore other) {
            var resultValues = this.values;
            var resultSigs = this.signatures;

            foreach (var key in this.signatures.Keys.Intersect(other.signatures.Keys)) {
                Assert.IsTrue(this.signatures[key] == other.signatures[key]);
            }

            foreach (var (key, value) in other.signatures) {
                if (!resultSigs.ContainsKey(key)) {
                    resultSigs = resultSigs.Remove(key);
                }
            }

            foreach (var (key, value) in other.values) {
                if (this.values.TryGetValue(key, out var myValue)) {
                    if (value != myValue) {
                        resultValues = resultValues.Remove(key);
                    }
                }
            }

            return new TypeStore(resultSigs, resultValues);
        }

        public IHelixType GetSignature(IValueLocation location) {
            if (location is NamedLocation named && this.GetSingularTypes(named.Name).TryGetValue(out var type)) {
                return type;
            }
            else {
                Assert.IsTrue(this.signatures.ContainsKey(location));
                return this.signatures[location];
            }            
        }

        public IHelixType GetSignature(string name) {
            return this.GetSignature(new NamedLocation(name));
        }

        public void SetSignature(IValueLocation location, IHelixType type) {
            Assert.IsFalse(this.signatures.ContainsKey(location));
            this.signatures = this.signatures.SetItem(location, type);
        }        

        public void SetLocal(IValueLocation location, IHelixType valueType) {
            Assert.IsTrue(this.signatures.ContainsKey(location));
            this.values = this.values.SetItem(location, valueType);
        }

        public IHelixType GetLocalType(IValueLocation location) {
            return this.values.GetValueOrNone(location).OrElse(() => this.GetSignature(location));
        }

        public IHelixType GetLocalType(string name) {
            return this.GetSingularTypes(name).OrElse(() => this.GetLocalType(new NamedLocation(name)));
        }

        public void ClearLocal(IValueLocation location) {
            this.values = this.values.Remove(location);
            //this.currentValues.Remove(location);
        }

        private Option<IHelixType> GetSingularTypes(string name) {
            if (long.TryParse(name, out var longValue)) {
                return new SingularWordType(longValue);
            }
            else if (bool.TryParse(name, out var boolValue)) {
                return new SingularBoolType(boolValue);
            }
            else if (name == "void") {
                return new VoidType();
            }
            else {
                return Option.None;
            }
        }
    }
}
