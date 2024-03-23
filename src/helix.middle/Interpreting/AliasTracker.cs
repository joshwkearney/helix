using Helix.Common;
using Helix.Common.Hmm;
using Helix.Common.Types;
using Helix.MiddleEnd.FlowTyping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.MiddleEnd.Interpreting {
    internal class AliasTracker {
        private readonly AnalysisContext context;

        public AliasTracker(AnalysisContext context) {
            this.context = context;
        }

        public void RegisterInvoke(string result, IHelixType resultType, IReadOnlyList<string> args, IReadOnlyList<IHelixType> argTypes) {
            Assert.IsTrue(args.Count == argTypes.Count);

            foreach (var (arg, type) in args.Zip(argTypes)) {
                foreach (var mem in type.GetMembers(this.context)) {
                    var memLocation = mem.CreateLocation(arg);
                    var roots = this.context.Aliases.GetBoxedRoots(memLocation, mem.Type);

                    // TODO: Don't reset read-only pointers

                    // For the locations that can be passed to the function, set their location to unknown
                    // and clear it because the function might change it
                    foreach (var root in roots) {
                        this.context.Aliases.SetBoxedRoots(root, mem.Type, [UnknownLocation.Instance]);
                        this.context.Types.ClearType(root);
                        this.context.Predicates.MutateLocation(root, Option.None);
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
                var rValueLocation = mem.CreateLocation(assignRValue);
                var targets = this.context.Aliases.GetReferencedRoots(lValueLocation);
                var assignedRoots = this.context.Aliases.GetBoxedRoots(rValueLocation, mem.Type);

                if (targets.Count == 1 && !targets.First().IsUnknown) {
                    // We only have one target lvalue which isn't unknown, so we know exactly what
                    // location this lvalue is modifying. That means we reset its aliases (what is
                    // stored in it) and only alias the value we're assigning

                    this.context.Aliases.SetBoxedRoots(targets.First(), mem.Type, assignedRoots);
                    this.context.Types[targets.First()] = mem.Type;

                    this.context.Predicates.MutateLocation(targets.First(), this.context.Predicates[rValueLocation]);
                }
                else {
                    // Here the target could be multiple roots or unknown, so in this case we have
                    // to treat the assignment as a black box and add our new assigned roots to the
                    // old ones

                    foreach (var target in targets) {
                        var allRoots = this
                            .context.Aliases.GetBoxedRoots(target, mem.Type)
                            .Concat(assignedRoots)
                            .ToArray();

                        this.context.Aliases.SetBoxedRoots(target, mem.Type, allRoots);

                        // Merge types and predicates rather than clearing them
                        this.context.Types[target] = this.context.Unifier.UnifyTypes(this.context.Types[target], mem.Type, default);
                        this.context.Predicates.MutateLocation(target, this.context.Predicates[target].SelectMany(x => this.context.Predicates[rValueLocation].Select(y => x.Or(x))));
                    }
                }
            }
        }

        public void RegisterFunctionParameter(string parameterName, IHelixType parameterType) {
            foreach (var mem in parameterType.GetMembers(this.context)) {
                var parLocation = mem.CreateLocation(parameterName);

                this.context.Aliases.SetReferencedRoots(parLocation, [parLocation]);
                this.context.Aliases.SetBoxedRoots(parLocation, mem.Type, [UnknownLocation.Instance]);

                this.context.Types[parLocation] = mem.Type;
            }
        }

        public void RegisterLocal(string variableName, string assignValue) {
            var signature = this.context.Types[assignValue].GetSupertype();

            foreach (var mem in signature.GetMembers(this.context)) {
                var variableLocation = mem.CreateLocation(variableName);
                var assignLocation = mem.CreateLocation(assignValue);
                var aliases = this.context.Aliases.GetBoxedRoots(assignLocation, mem.Type);
                var assignType = this.context.Types[assignLocation];

                // This variable is a reference to itself
                this.context.Aliases.SetReferencedRoots(variableLocation, [variableLocation]);

                // Box anything the parameter value is boxing
                this.context.Aliases.SetBoxedRoots(variableLocation, signature, aliases);

                // Set this member's type to the actual assign expression's type
                // instead of the member's type (the assign expression can be
                // more specific)
                this.context.Types[variableLocation] = assignType;

                // Set the predicates to the assignment predicates
                this.context.Predicates[variableLocation] = this.context.Predicates[assignValue];
            }
        }

        public void RegisterLocal(string variableName, IHelixType signature) {
            foreach (var mem in signature.GetMembers(this.context)) {
                var variableLocation = mem.CreateLocation(variableName);

                this.context.Aliases.SetReferencedRoots(variableLocation, [variableLocation]);
                this.context.Aliases.SetBoxedRoots(variableLocation, mem.Type, []);

                this.context.Types[variableLocation] = mem.Type; 
            }
        }

        public void RegisterIf(string variableName, IHelixType signature, string affirmValue, string negativeValue) {
            foreach (var mem in signature.GetMembers(this.context)) {
                var variableLocation = mem.CreateLocation(variableName);

                this.context.Aliases.SetReferencedRoots(variableLocation, [variableLocation]);

                // Box anything the parameter value is boxing
                var assign1 = mem.CreateLocation(affirmValue);
                var assign2 = mem.CreateLocation(negativeValue);

                var aliases1 = this.context.Aliases.GetBoxedRoots(assign1, mem.Type);
                var aliases2 = this.context.Aliases.GetBoxedRoots(assign2, mem.Type);

                this.context.Aliases.SetBoxedRoots(variableLocation, mem.Type, aliases1.Concat(aliases2));
                this.context.Types[variableLocation] = mem.Type;
                this.context.Predicates[variableLocation] = this.context.Predicates[assign1].SelectMany(x => this.context.Predicates[assign2].Select(y => x.Or(y)));

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

                this.context.Aliases.SetReferencedRoots(variableLocation, [variableLocation]);
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
                    var boxedRoots = this.context.Aliases.GetBoxedRoots(assignValueLocation, mem.Type);
                    var targetLocation = mem.CreateLocation(baseTargetLocation);

                    this.context.Aliases.SetBoxedRoots(targetLocation, mem.Type, boxedRoots);
                    this.context.Types[targetLocation] = assignType;
                    this.context.Predicates[targetLocation] = this.context.Predicates[assignValueLocation];

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
                    .context.Aliases.GetReferencedRoots(memberLocation)
                    .Select(mem.CreateLocation)
                    .ToArray();

                var resultMemberLocation = mem.CreateLocation(resultName);

                this.context.Aliases.SetReferencedRoots(resultMemberLocation, roots);
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
                    .context.Aliases.GetBoxedRoots(memberLocation, mem.Type)
                    .Select(mem.CreateLocation)
                    .ToArray();

                var resultMemberLocation = mem.CreateLocation(resultName);

                this.context.Aliases.SetBoxedRoots(resultMemberLocation, mem.Type, boxedRoots);
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
                //    .context.Aliases.GetReferencedRoots(memberLocation)
                //    .Select(mem.CreateLocation)
                //    .ToArray();

                var resultMemberLocation = mem.CreateLocation(resultName);

                this.context.Aliases.SetReferencedRoots(resultMemberLocation, [UnknownLocation.Instance]);
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
                //    .context.Aliases.GetBoxedRoots(memberLocation, mem.Type)
                //    .Select(mem.CreateLocation)
                //    .ToArray();

                var resultMemberLocation = mem.CreateLocation(resultName);

                this.context.Aliases.SetBoxedRoots(resultMemberLocation, mem.Type, [UnknownLocation.Instance]);
            }
        }

        public void RegisterArrayLiteral(string resultName, long length, IHelixType elementType) {
            // TODO pending fixed length array types
            var loc = new NamedLocation(resultName);

            this.context.Types[loc] = new ArrayType() { InnerType = elementType };
        }

        public void RegisterDereferencedPointerReference(string resultName, IHelixType resultType, string dereferenceTarget) {
            var dereferenceType = this.context.Types[dereferenceTarget].GetSupertype();
            var derefLocation = new NamedLocation(dereferenceTarget);
            var roots = this.context.Aliases.GetBoxedRoots(derefLocation, dereferenceType);

            foreach (var mem in resultType.GetMembers(this.context)) {
                var memRoots = roots.Select(mem.CreateLocation);
                var memLocation = mem.CreateLocation(resultName);

                this.context.Aliases.SetReferencedRoots(memLocation, memRoots);
                this.context.Types[memLocation] = mem.Type;
            }
        }

        public void RegisterDereferencedPointer(string resultName, IHelixType resultType, string dereferenceTarget) {
            var dereferenceType = this.context.Types[dereferenceTarget];
            var derefLocation = new NamedLocation(dereferenceTarget);
            var roots = this.context.Aliases.GetBoxedRoots(derefLocation, dereferenceType);

            foreach (var mem in resultType.GetMembers(this.context)) {
                var memRoots = roots.Select(mem.CreateLocation);
                var memLocation = mem.CreateLocation(resultName);

                this.context.Aliases.SetBoxedRoots(memLocation, mem.Type, memRoots);
                this.context.Types[memLocation] = mem.Type;
            }
        }

        public void RegisterAddressOf(string resultName, string addressOfTarget) {
            // Note: We don't have to assign members because this will always return a pointer,
            // which doesn't have any members

            var addressOfType = new PointerType() { InnerType = this.context.Types[addressOfTarget].GetSupertype() };
            var derefTargetLocation = new NamedLocation(addressOfTarget);
            var resultLocation = new NamedLocation(resultName);
            var roots = this.context.Aliases.GetReferencedRoots(derefTargetLocation);

            this.context.Aliases.SetBoxedRoots(resultLocation, addressOfType, roots);
            this.context.Types[resultLocation] = addressOfType;
        }
    }
}
