using Helix.Common;
using Helix.Common.Collections;
using Helix.Common.Hmm;
using Helix.Common.Types;
using Helix.MiddleEnd.TypeChecking;
using System.Collections.Immutable;

namespace Helix.MiddleEnd.Interpreting {
    internal class AliasingTracker {
        // Definitions:

        // Value Location: A specific location in the program that can be used for storing values.
        //     There are two senses of "location": referring to the container that can hold values,
        //     which is an lvalue description, or referring to what is stored in that container, 
        //     which is an rvalue description. The two dictionaries below keep track of both of
        //     these relationships

        // Root: A location in the program that is capable of actually storing a value. These are
        //     storage locations that aren't simply names for other values, or temp variables.
        //     Roots are created in a program by initializing a variable "var x = 45", initializing
        //     a variable with a struct value "var x = new Point" (roots are created for the struct
        //     members as well, since those are essentially also local variables, and by creating
        //     arrays (each cell of the array is a root)

        // Referenced Root: A root that has been given another name by a reference. Looks like
        //    "ref temp_0 = x" in the IR

        // Boxed Root: A root that is being pointed to by a value, such as a pointer

        // Stores the roots that have been re-labeled by references. These references are essentially
        // new names for the same roots, which is lvalue semantics
        private ImmutableDictionary<IValueLocation, ValueSet<IValueLocation>> referencedRoots 
            = ImmutableDictionary<IValueLocation, ValueSet<IValueLocation>>.Empty;

        // Stores which roots are pointed to by which values. This dictionary is used for things like
        // pointers or arrays that have an address stored to a root
        private ImmutableDictionary<IValueLocation, ValueSet<IValueLocation>> boxedRoots 
            = ImmutableDictionary<IValueLocation, ValueSet<IValueLocation>>.Empty;

        private readonly TypeCheckingContext context;

        public AliasingTracker(TypeCheckingContext context) {
            this.context = context;
        }

        private AliasingTracker(
            TypeCheckingContext context,
            ImmutableDictionary<IValueLocation, ValueSet<IValueLocation>> referencedRoots,
            ImmutableDictionary<IValueLocation, ValueSet<IValueLocation>> boxedRoots) {

            this.context = context;
            this.referencedRoots = referencedRoots;
            this.boxedRoots = boxedRoots;
        }

        public AliasingTracker CreateScope() {
            return new AliasingTracker(this.context, this.referencedRoots, this.boxedRoots);
        }

        public AliasingTracker MergeWith(AliasingTracker other) {
            var referencedRootKeys = this.referencedRoots.Keys.Intersect(other.referencedRoots.Keys);
            var boxedRootKeys = this.boxedRoots.Keys.Intersect(other.boxedRoots.Keys);

            var newReferencedRoots = ImmutableDictionary<IValueLocation, ValueSet<IValueLocation>>.Empty;
            var newBoxedKeys = ImmutableDictionary<IValueLocation, ValueSet<IValueLocation>>.Empty;

            foreach (var key in referencedRootKeys) {
                var refs = this.referencedRoots[key]
                    .Concat(other.referencedRoots[key])
                    .Distinct()
                    .ToValueSet();

                newReferencedRoots = newReferencedRoots.SetItem(key, refs);
            }

            foreach (var key in boxedRootKeys) {
                var boxed = this.boxedRoots[key]
                    .Concat(other.boxedRoots[key])
                    .Distinct()
                    .ToValueSet();

                newBoxedKeys = newBoxedKeys.SetItem(key, boxed);
            }

            return new AliasingTracker(this.context, newReferencedRoots, newBoxedKeys);
        }

        public bool WasModifiedBy(AliasingTracker other) {
            var referencedRootKeys = this.referencedRoots.Keys.Intersect(other.referencedRoots.Keys);
            var boxedRootKeys = this.boxedRoots.Keys.Intersect(other.boxedRoots.Keys);

            foreach (var root in referencedRootKeys) {
                if (this.referencedRoots[root] != other.referencedRoots[root]) {
                    return true;
                }
            }

            foreach (var root in boxedRootKeys) {
                if (this.boxedRoots[root] != other.boxedRoots[root]) {
                    return true;
                }
            }

            return false;
        }

        public void RegisterInvoke(IReadOnlyList<string> args, IReadOnlyList<IHelixType> argTypes) {
            Assert.IsTrue(args.Count == argTypes.Count);

            foreach (var (arg, type) in args.Zip(argTypes)) {
                foreach (var mem in this.GetMembers(type)) {
                    var memLocation = mem.CreateLocation(arg);
                    var roots = this.GetBoxedRoots(memLocation, mem.Type);

                    foreach (var root in roots) {
                        this.SetBoxedRoots(root, mem.Type, [UnknownLocation.Instance]);
                    }
                }
            }
        }

        public void RegisterAssignment(string targetLValue, string assignRValue, IHelixType assignType) {
            foreach (var mem in this.GetMembers(assignType)) {
                var targets = this.GetReferencedRoots(mem.CreateLocation(targetLValue));
                var assignedRoots = this.GetBoxedRoots(mem.CreateLocation(assignRValue), mem.Type);

                if (targets.Count == 1 && !targets.First().IsUnknown) {
                    // We only have one target lvalue which isn't unknown, so we know exactly what
                    // location this lvalue is modifying. That means we reset its aliases (what is
                    // stored in it) and only alias the value we're assigning

                    this.SetBoxedRoots(targets.First(), mem.Type, assignedRoots);
                }
                else {
                    // Here the target could be multiple roots or unknown, so in this case we have
                    // to treat the assignment as a black box and add our new assigned roots to the
                    // old ones

                    foreach (var target in targets) {
                        var allRoots = this
                            .GetBoxedRoots(target, mem.Type)
                            .Concat(assignedRoots)
                            .ToArray();

                        this.SetBoxedRoots(target, mem.Type, allRoots);
                    }
                }
            }
        }

        public void RegisterFunctionParameter(string parameterName, IHelixType parameterType) {
            foreach (var mem in this.GetMembers(parameterType)) {
                var parLocation = mem.CreateLocation(parameterName);

                this.SetReferencedRoots(parLocation, [parLocation]);
                this.SetBoxedRoots(parLocation, mem.Type, [UnknownLocation.Instance]);
            }
        }

        public void RegisterLocal(string variableName, IHelixType variableType, string assignValue) {
            foreach (var mem in this.GetMembers(variableType)) {
                var variableLocation = mem.CreateLocation(variableName);

                this.SetReferencedRoots(variableLocation, [variableLocation]);

                // Box anything the parameter value is boxing
                var assignRValue = mem.CreateLocation(assignValue);
                var aliases = this.GetBoxedRoots(assignRValue, mem.Type);

                this.SetBoxedRoots(variableLocation, mem.Type, aliases);
            }
        }

        public void RegisterLocalWithoutAliasing(string variableName, IHelixType variableType) {
            foreach (var mem in this.GetMembers(variableType)) {
                var variableLocation = mem.CreateLocation(variableName);

                this.SetReferencedRoots(variableLocation, [variableLocation]);
                this.SetBoxedRoots(variableLocation, mem.Type, []);
            }
        }

        public void RegisterNewStruct(string resultName, IHelixType variableType, IReadOnlyList<HmmNewFieldAssignment> assignments) {
            foreach (var assign in assignments) {
                Assert.IsTrue(assign.Field.HasValue);
            }

            foreach (var mem in this.GetMembers(variableType)) {
                var variableLocation = mem.CreateLocation(resultName);

                this.SetReferencedRoots(variableLocation, [variableLocation]);
            }

            foreach (var assign in assignments) {
                var assignType = this.context.Types[assign.Value];
                var assignValue = new NamedLocation(assign.Value);

                var baseTargetLocation = new MemberAccessLocation() {
                    Parent = new NamedLocation(resultName),
                    Member = assign.Field.GetValue()
                };

                foreach (var mem in this.GetMembers(assignType)) {
                    var assignValueLocation = mem.CreateLocation(assignValue);
                    var boxedRoots = this.GetBoxedRoots(assignValueLocation, mem.Type);
                    var targetLocation = mem.CreateLocation(baseTargetLocation);

                    this.SetBoxedRoots(targetLocation, mem.Type, boxedRoots);
                }
            }
        }

        public void RegisterNewUnion(string resultName, IHelixType resultType, string assignValue) {
            this.RegisterLocal(resultName, resultType, assignValue);
        }

        public void RegisterMemberAccessReference(string resultName, string accessTarget, string memberName, IHelixType memberType) {
            foreach (var mem in this.GetMembers(memberType)) {
                var memberLocation = new MemberAccessLocation() {
                    Member = memberName,
                    Parent = new NamedLocation(accessTarget)
                };

                var roots = this
                    .GetReferencedRoots(memberLocation)
                    .Select(mem.CreateLocation)
                    .ToArray();

                var resultMemberLocation = mem.CreateLocation(resultName);

                this.SetReferencedRoots(resultMemberLocation, roots);
            }
        }

        public void RegisterMemberAccess(string resultName, string accessTarget, string memberName, IHelixType memberType) {
            foreach (var mem in this.GetMembers(memberType)) {
                var memberLocation = new MemberAccessLocation() {
                    Member = memberName,
                    Parent = new NamedLocation(accessTarget)
                };

                var boxedRoots = this
                    .GetBoxedRoots(memberLocation, mem.Type)
                    .Select(mem.CreateLocation)
                    .ToArray();

                var resultMemberLocation = mem.CreateLocation(resultName);

                this.SetBoxedRoots(resultMemberLocation, mem.Type, boxedRoots);
            }
        }

        public void RegisterArrayIndexReference(string resultName, string accessTarget, string index, IHelixType elementType) {
            // TODO pending fixed length array types

            foreach (var mem in this.GetMembers(elementType)) {
                //var memberLocation = new MemberAccessLocation() {
                //    Member = index.ToString(),
                //    Parent = new NamedLocation(accessTarget)
                //};

                //var roots = this
                //    .GetReferencedRoots(memberLocation)
                //    .Select(mem.CreateLocation)
                //    .ToArray();

                var resultMemberLocation = mem.CreateLocation(resultName);

                this.SetReferencedRoots(resultMemberLocation, [UnknownLocation.Instance]);
            }
        }

        public void RegisterArrayIndex(string resultName, string accessTarget, string index, IHelixType elementType) {
            // TODO pending fixed length array types

            foreach (var mem in this.GetMembers(elementType)) {
                //var memberLocation = new MemberAccessLocation() {
                //    Member = memberName,
                //    Parent = new NamedLocation(accessTarget)
                //};

                //var boxedRoots = this
                //    .GetBoxedRoots(memberLocation, mem.Type)
                //    .Select(mem.CreateLocation)
                //    .ToArray();

                var resultMemberLocation = mem.CreateLocation(resultName);

                this.SetBoxedRoots(resultMemberLocation, mem.Type, [UnknownLocation.Instance]);
            }
        }

        public void RegisterArrayLiteral(string resultName, long length, IHelixType elementType) {
            // TODO pending fixed length array types
        }

        public void RegisterDereferencedPointerReference(string resultName, IHelixType resultType, string dereferenceTarget, IHelixType dereferenceType) {
            var derefLocation = new NamedLocation(dereferenceTarget);
            var roots = this.GetBoxedRoots(derefLocation, dereferenceType);

            foreach (var mem in this.GetMembers(resultType)) {
                var memRoots = roots.Select(mem.CreateLocation);
                var memLocation = mem.CreateLocation(resultName);

                this.SetReferencedRoots(memLocation, memRoots);
            }
        }

        public void RegisterDereferencedPointer(string resultName, IHelixType resultType, string dereferenceTarget, IHelixType dereferenceType) {
            var derefLocation = new NamedLocation(dereferenceTarget);
            var roots = this.GetBoxedRoots(derefLocation, dereferenceType);

            foreach (var mem in this.GetMembers(resultType)) {
                var memRoots = roots.Select(mem.CreateLocation);
                var memLocation = mem.CreateLocation(resultName);

                this.SetBoxedRoots(memLocation, mem.Type, memRoots);
            }
        }

        public void RegisterAddressOf(string resultName, string addressOfTarget, IHelixType addressOfType) {
            // Note: We don't have to assign members because this will always return a pointer,
            // which doesn't have any members

            var derefTargetLocation = new NamedLocation(addressOfTarget);
            var resultLocation = new NamedLocation(resultName);
            var roots = this.GetReferencedRoots(derefTargetLocation);

            this.SetBoxedRoots(resultLocation, addressOfType, roots);
        }

        private void SetReferencedRoots(IValueLocation referenceName, IEnumerable<IValueLocation> lvalues) {
            if (referenceName.IsUnknown) {
                return;
            }

            var values = lvalues.Select(x => x.IsUnknown ? UnknownLocation.Instance : x).ToValueSet();

            this.referencedRoots = this.referencedRoots.SetItem(referenceName, values);
        }

        private ValueSet<IValueLocation> GetReferencedRoots(IValueLocation rvalue) {
            if (rvalue.IsUnknown) {
                return [UnknownLocation.Instance];
            }

            Assert.IsTrue(this.referencedRoots.ContainsKey(rvalue));
            return this.referencedRoots[rvalue];
        }

        private void SetBoxedRoots(IValueLocation rValueName, IHelixType rValueType, IEnumerable<IValueLocation> lvalues) {
            if (rValueName.IsUnknown) {
                return;
            }

            if (rValueType.DoesAliasLValues()) {
                var values = lvalues.Select(x => x.IsUnknown ? UnknownLocation.Instance : x).ToValueSet();

                this.boxedRoots = this.boxedRoots.SetItem(rValueName, values);
            }
            else {
                this.boxedRoots = this.boxedRoots.SetItem(rValueName, []);
            }            
        }

        private ValueSet<IValueLocation> GetBoxedRoots(IValueLocation rValueName, IHelixType rValueType) {
            if (!rValueType.DoesAliasLValues()) {
                return [];
            }

            if (rValueName.IsUnknown) {
                return [UnknownLocation.Instance];
            }

            Assert.IsTrue(this.boxedRoots.ContainsKey(rValueName));
            return this.boxedRoots[rValueName];
        }

        private IEnumerable<MemberFactory> GetMembers(IHelixType type) => this.GetMembersHelper([], type);

        private IEnumerable<MemberFactory> GetMembersHelper(IReadOnlyList<string> previous, IHelixType type) {
            yield return new MemberFactory(type, previous);

            if (type.GetStructSignature(this.context).TryGetValue(out var structType)) {
                foreach (var mem in structType.Members) {
                    var segments = previous.Append(mem.Name).ToArray();

                    foreach (var results in this.GetMembersHelper(segments, mem.Type)) {
                        yield return results;
                    }
                }
            }
        }

        private class MemberFactory {
            public IHelixType Type { get; }

            public IReadOnlyList<string> MemberChain { get; }

            public MemberFactory(IHelixType type, IReadOnlyList<string> mems) {
                this.MemberChain = mems;
                this.Type = type;
            }

            public IValueLocation CreateLocation(IValueLocation parent) {
                foreach (var mem in this.MemberChain) {
                    parent = new MemberAccessLocation() {
                        Parent = parent,
                        Member = mem
                    };
                }

                return parent;
            }

            public IValueLocation CreateLocation(string name) => this.CreateLocation(new NamedLocation(name));
        }
    }
}
