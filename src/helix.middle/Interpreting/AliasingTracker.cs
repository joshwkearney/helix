using Helix.Common;
using Helix.Common.Collections;
using Helix.Common.Hmm;
using Helix.Common.Types;
using Helix.MiddleEnd.TypeChecking;
using System.Collections.Immutable;
using System.ComponentModel.Design;

namespace Helix.MiddleEnd.Interpreting
{
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

        public void RegisterInvoke(string result, IHelixType resultType, IReadOnlyList<string> args, IReadOnlyList<IHelixType> argTypes) {
            Assert.IsTrue(args.Count == argTypes.Count);

            foreach (var (arg, type) in args.Zip(argTypes)) {
                foreach (var mem in type.GetMembers(this.context)) {
                    var memLocation = mem.CreateLocation(arg);
                    var roots = this.GetBoxedRoots(memLocation, mem.Type);

                    // TODO: Don't reset read-only pointers

                    // For the locations that can be passed to the function, set their location to unknown
                    // and clear it because the function might change it
                    foreach (var root in roots) {
                        this.SetBoxedRoots(root, mem.Type, [UnknownLocation.Instance]);
                        this.context.Types.ClearType(root);
                    }
                }
            }

            // TODO: Can we be more specific here than unknown? Maybe set the root to a combination of the 
            // input roots?
            this.RegisterLocal(result, resultType);
        }

        public void RegisterAssignment(string targetLValue, string assignRValue) {
            var assignType = this.context.Types[assignRValue];

            foreach (var mem in assignType.GetMembers(this.context)) {
                var lValueLocation = mem.CreateLocation(targetLValue);
                var targets = this.GetReferencedRoots(lValueLocation);
                var assignedRoots = this.GetBoxedRoots(mem.CreateLocation(assignRValue), mem.Type);

                if (targets.Count == 1 && !targets.First().IsUnknown) {
                    // We only have one target lvalue which isn't unknown, so we know exactly what
                    // location this lvalue is modifying. That means we reset its aliases (what is
                    // stored in it) and only alias the value we're assigning

                    this.SetBoxedRoots(targets.First(), mem.Type, assignedRoots);
                    this.context.Types[targets.First()] = mem.Type;
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
                        this.context.Types.ClearType(lValueLocation);
                    }
                }                
            }
        }

        public void RegisterFunctionParameter(string parameterName, IHelixType parameterType) {
            foreach (var mem in parameterType.GetMembers(this.context)) {
                var parLocation = mem.CreateLocation(parameterName);

                this.SetReferencedRoots(parLocation, [parLocation]);
                this.SetBoxedRoots(parLocation, mem.Type, [UnknownLocation.Instance]);

                this.context.Types[parLocation] = mem.Type;
            }
        }

        public void RegisterLocal(string variableName, string assignValue) {
            var signature = this.context.Types[assignValue].GetSupertype();

            foreach (var mem in signature.GetMembers(this.context)) {
                var variableLocation = mem.CreateLocation(variableName);
                var assignLocation = mem.CreateLocation(assignValue);
                var aliases = this.GetBoxedRoots(assignLocation, mem.Type);
                var assignType = this.context.Types[assignLocation];

                // This variable is a reference to itself
                this.SetReferencedRoots(variableLocation, [variableLocation]);

                // Box anything the parameter value is boxing
                this.SetBoxedRoots(variableLocation, signature, aliases);

                // Set this member's type to the actual assign expression's type
                // instead of the member's type (the assign expression can be
                // more specific)
                this.context.Types[variableLocation] = assignType;
            }
        }

        public void RegisterLocal(string variableName, IHelixType signature) {
            foreach (var mem in signature.GetMembers(this.context)) {
                var variableLocation = mem.CreateLocation(variableName);

                this.SetReferencedRoots(variableLocation, [variableLocation]);
                this.SetBoxedRoots(variableLocation, mem.Type, []);

                this.context.Types[variableLocation] = mem.Type;
            }
        }

        public void RegisterIf(string variableName, IHelixType signature, string affirmValue, string negativeValue) {
            foreach (var mem in signature.GetMembers(this.context)) {
                var variableLocation = mem.CreateLocation(variableName);

                this.SetReferencedRoots(variableLocation, [variableLocation]);

                // Box anything the parameter value is boxing
                var assign1 = mem.CreateLocation(affirmValue);
                var assign2 = mem.CreateLocation(negativeValue);

                var aliases1 = this.GetBoxedRoots(assign1, mem.Type);
                var aliases2 = this.GetBoxedRoots(assign2, mem.Type);

                this.SetBoxedRoots(variableLocation, mem.Type, aliases1.Concat(aliases2));
                this.context.Types[variableLocation] = mem.Type;

                var affirmType = this.context.Types[assign1];
                var negType = this.context.Types[assign2];

                if (affirmType == negType) {
                    this.context.Types[variableLocation] = affirmType;
                }
            }
        }

        public void RegisterNewStruct(string resultName, IHelixType variableType, IReadOnlyList<HmmNewFieldAssignment> assignments) {
            foreach (var assign in assignments) {
                Assert.IsTrue(assign.Field.HasValue);
            }

            foreach (var mem in variableType.GetMembers(this.context)) {
                var variableLocation = mem.CreateLocation(resultName);

                this.SetReferencedRoots(variableLocation, [variableLocation]);
            }

            this.context.Types[new NamedLocation(resultName)] = variableType;

            foreach (var assign in assignments) {
                var assignType = this.context.Types[assign.Value];
                var assignLocation = new NamedLocation(assign.Value);

                var baseTargetLocation = new MemberAccessLocation() {
                    Parent = new NamedLocation(resultName),
                    Member = assign.Field.GetValue()
                };

                foreach (var mem in assignType.GetMembers(this.context)) {
                    var assignValueLocation = mem.CreateLocation(assignLocation);
                    var boxedRoots = this.GetBoxedRoots(assignValueLocation, mem.Type);
                    var targetLocation = mem.CreateLocation(baseTargetLocation);

                    this.SetBoxedRoots(targetLocation, mem.Type, boxedRoots);
                    this.context.Types[targetLocation] = assignType;

                    // TODO: How to get the type this is supposed to be for the signature?
                }
            }
        }

        public void RegisterNewUnion(string resultName, IHelixType resultType, string assignValue) {
            this.RegisterLocal(resultName, resultType);
        }

        public void RegisterMemberAccessReference(string resultName, string accessTarget, string memberName) {
            var memberLocation = new MemberAccessLocation() {
                Member = memberName,
                Parent = new NamedLocation(accessTarget)
            };

            var memberType = this.context.Types[memberLocation].GetSupertype();

            foreach (var mem in memberType.GetMembers(this.context)) {
                var roots = this
                    .GetReferencedRoots(memberLocation)
                    .Select(mem.CreateLocation)
                    .ToArray();

                var resultMemberLocation = mem.CreateLocation(resultName);

                this.SetReferencedRoots(resultMemberLocation, roots);
                this.context.Types[resultMemberLocation] = mem.Type;
            }
        }

        public void RegisterMemberAccess(string resultName, string accessTarget, string memberName) {
            var memberLocation = new MemberAccessLocation() {
                Member = memberName,
                Parent = new NamedLocation(accessTarget)
            };

            var memberType = this.context.Types[memberLocation];

            foreach (var mem in memberType.GetMembers(this.context)) {
                var boxedRoots = this
                    .GetBoxedRoots(memberLocation, mem.Type)
                    .Select(mem.CreateLocation)
                    .ToArray();

                var resultMemberLocation = mem.CreateLocation(resultName);

                this.SetBoxedRoots(resultMemberLocation, mem.Type, boxedRoots);
                this.context.Types[resultMemberLocation] = mem.Type;
            }
        }

        public void RegisterArrayIndexReference(string resultName, string accessTarget, string index, IHelixType elementType) {
            // TODO pending fixed length array types

            foreach (var mem in elementType.GetMembers(this.context)) {
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

            foreach (var mem in elementType.GetMembers(this.context)) {
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
            this.context.Types[new NamedLocation(resultName)] = new ArrayType() { InnerType = elementType };
        }

        public void RegisterDereferencedPointerReference(string resultName, IHelixType resultType, string dereferenceTarget) {
            var dereferenceType = this.context.Types[dereferenceTarget].GetSupertype();
            var derefLocation = new NamedLocation(dereferenceTarget);
            var roots = this.GetBoxedRoots(derefLocation, dereferenceType);

            foreach (var mem in resultType.GetMembers(this.context)) {
                var memRoots = roots.Select(mem.CreateLocation);
                var memLocation = mem.CreateLocation(resultName);

                this.SetReferencedRoots(memLocation, memRoots);
                this.context.Types[memLocation] = mem.Type;
            }
        }

        public void RegisterDereferencedPointer(string resultName, IHelixType resultType, string dereferenceTarget) {
            var dereferenceType = this.context.Types[dereferenceTarget];
            var derefLocation = new NamedLocation(dereferenceTarget);
            var roots = this.GetBoxedRoots(derefLocation, dereferenceType);

            foreach (var mem in resultType.GetMembers(this.context)) {
                var memRoots = roots.Select(mem.CreateLocation);
                var memLocation = mem.CreateLocation(resultName);

                this.SetBoxedRoots(memLocation, mem.Type, memRoots);
                this.context.Types[memLocation] = mem.Type;
            }
        }

        public void RegisterAddressOf(string resultName, string addressOfTarget) {
            // Note: We don't have to assign members because this will always return a pointer,
            // which doesn't have any members

            var addressOfType = new PointerType() { InnerType = this.context.Types[addressOfTarget].GetSupertype() };
            var derefTargetLocation = new NamedLocation(addressOfTarget);
            var resultLocation = new NamedLocation(resultName);
            var roots = this.GetReferencedRoots(derefTargetLocation);

            this.SetBoxedRoots(resultLocation, addressOfType, roots);

            this.context.Types[resultLocation] = addressOfType;
        }

        private void SetReferencedRoots(IValueLocation referenceName, IEnumerable<IValueLocation> lvalues) {
            if (referenceName.IsUnknown) {
                return;
            }

            var values = lvalues.Select(x => x.IsUnknown ? UnknownLocation.Instance : x).ToValueSet();

            this.referencedRoots = this.referencedRoots.SetItem(referenceName, values);
        }

        public ValueSet<IValueLocation> GetReferencedRoots(IValueLocation rvalue) {
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

        public ValueSet<IValueLocation> GetBoxedRoots(IValueLocation rValueName, IHelixType rValueType) {
            if (!rValueType.DoesAliasLValues()) {
                return [];
            }

            if (rValueName.IsUnknown) {
                return [UnknownLocation.Instance];
            }

            Assert.IsTrue(this.boxedRoots.ContainsKey(rValueName));
            return this.boxedRoots[rValueName];
        }        
    }
}
