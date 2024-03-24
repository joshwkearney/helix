using Helix.Common;
using Helix.Common.Types;
using Helix.MiddleEnd.Interpreting;
using System.Collections.Immutable;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeStore {
        private ImmutableDictionary<string, IHelixType> values
            = ImmutableDictionary<string, IHelixType>.Empty;

        private readonly AnalysisContext context;

        private TypeStore(
            AnalysisContext context,
            ImmutableDictionary<string, IHelixType> currentTypes) {

            this.context = context;
            this.values = currentTypes;
        }

        public IHelixType this[IValueLocation index] {
            get => this.GetValueLocation(index);
            set => this.SetValueLocation(index, value);
        }

        public IHelixType this[string name] {
            get => this.GetSingularTypes(name).OrElse(() => this.values[name]);
            set => this.values = this.values.SetItem(name, value);
        }

        public TypeStore(AnalysisContext context) {
            this.context = context;
        }

        public TypeStore CreateScope() {
            return new TypeStore(this.context, this.values);
        }

        public void SetImplications(IReadOnlyDictionary<IValueLocation, IHelixType> implications) {
            foreach (var (key, value) in implications) {
                this.SetValueLocation(key, value);
            }
        }

        public bool WasModifiedBy(TypeStore other) {
            var keys = this.values.Keys.Intersect(other.values.Keys);

            foreach (var key in keys) {
                if (this.values[key] != other.values[key]) {
                    return true;
                }
            }

            return false;
        }

        public TypeStore MergeWith(TypeStore other) {
            var resultValues = new Dictionary<string, IHelixType>();
            var keys = this.values.Keys.Union(other.values.Keys);

            foreach (var key in keys) {
                if (this.values.ContainsKey(key) && other.values.ContainsKey(key)) {
                    // TODO: Fix not having a token location here
                    resultValues[key] = this.context.Unifier.UnifyTypes(this.values[key], other.values[key], default);
                }
                else if (this.values.ContainsKey(key)) {
                    resultValues[key] = this.values[key];
                }
                else {
                    resultValues[key] = other.values[key];
                }
            }

            return new TypeStore(this.context, resultValues.ToImmutableDictionary());
        }

        public void ClearType(IValueLocation location) {
            this.SelectValueLocation(location, x => x.GetSupertype());
        }

        private IHelixType GetValueLocation(IValueLocation loc) {
            if (loc is NamedLocation named) {
                if (this.GetSingularTypes(named.Name).TryGetValue(out var sing)) {
                    return sing;
                }
                else {
                    Assert.IsTrue(this.values.ContainsKey(named.Name));
                    return this.values[named.Name];
                }
            }
            else if (loc is MemberAccessLocation memberAccess) {
                var parentType = this.GetValueLocation(memberAccess.Parent);

                if (parentType is NominalType nom) {
                    Assert.IsTrue(nom.GetStructSignature(this.context).HasValue);

                    var sig = nom.GetStructSignature(this.context).GetValue();
                    Assert.IsTrue(sig.Members.Any(x => x.Name == memberAccess.Member));

                    return sig.Members.First(x => x.Name == memberAccess.Member).Type;
                }
                else {
                    Assert.IsTrue(parentType is SingularStructType);

                    var sing = (SingularStructType)parentType;
                    Assert.IsTrue(sing.Members.Any(x => x.Name == memberAccess.Member));

                    return sing.Members.First(x => x.Name == memberAccess.Member).Type;
                }
            }

            throw Assert.Fail();
        }

        private void SetValueLocation(IValueLocation loc, IHelixType newType) {
            if (loc is NamedLocation named) {
                this.values = this.values.SetItem(named.Name, newType);
            }

            this.SelectValueLocation(loc, _ => newType);
        }

        private void SelectValueLocation(IValueLocation loc, Func<IHelixType, IHelixType> typeSelector) {
            if (loc is NamedLocation named) {
                Assert.IsTrue(this.values.ContainsKey(named.Name));

                this.values = this.values.SetItem(named.Name, typeSelector(this.values[named.Name]));
            }
            else if (loc is MemberAccessLocation memberAccess) {
                this.SelectValueLocation(memberAccess.Parent, parentType => {
                    SingularStructType sing;

                    if (parentType is NominalType nom) {
                        Assert.IsTrue(nom.GetStructSignature(this.context).HasValue);

                        var sig = nom.GetStructSignature(this.context).GetValue();

                        sing = new SingularStructType() {
                            StructType = nom,
                            Members = sig.Members.ToValueSet()
                        };
                    }
                    else {
                        Assert.IsTrue(parentType is SingularStructType);

                        sing = (SingularStructType)parentType;
                    }

                    Assert.IsTrue(sing.Members.Any(x => x.Name == memberAccess.Member));

                    var initialType = sing.Members.First(x => x.Name == memberAccess.Member).Type;

                    var newMems = sing.Members
                        .Where(x => x.Name != memberAccess.Member)
                        .Append(new StructMember() { IsMutable = false, Name = memberAccess.Member, Type = typeSelector(initialType) })
                        .ToValueSet();

                    Assert.IsTrue(newMems.Select(x => x.Name).ToHashSet().SetEquals(sing.Members.Select(x => x.Name)));

                    return new SingularStructType() {
                        StructType = sing.StructType,
                        Members = newMems
                    };
                });
            }
            else {
                Assert.Fail();
            }
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
