using Helix.Common;
using Helix.Common.Collections;
using Helix.Common.Hmm;
using Helix.Common.Types;
using System.Collections.Immutable;

namespace Helix.MiddleEnd.Interpreting {
    internal class AliasStore {
        // For use with AliasTracker

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

        private readonly AnalysisContext context;

        public AliasStore(AnalysisContext context) {
            this.context = context;
        }

        private AliasStore(
            AnalysisContext context,
            ImmutableDictionary<IValueLocation, ValueSet<IValueLocation>> referencedRoots,
            ImmutableDictionary<IValueLocation, ValueSet<IValueLocation>> boxedRoots) {

            this.context = context;
            this.referencedRoots = referencedRoots;
            this.boxedRoots = boxedRoots;
        }

        public AliasStore CreateScope() {
            return new AliasStore(this.context, this.referencedRoots, this.boxedRoots);
        }

        public AliasStore MergeWith(AliasStore other) {
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

            return new AliasStore(this.context, newReferencedRoots, newBoxedKeys);
        }

        public bool WasModifiedBy(AliasStore other) {
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

        public void SetReferencedRoots(IValueLocation referenceName, IEnumerable<IValueLocation> lvalues) {
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

        public void SetBoxedRoots(IValueLocation rValueName, IHelixType rValueType, IEnumerable<IValueLocation> lvalues) {
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
