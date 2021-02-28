using System.Collections.Generic;
using System.Linq;

namespace Trophy.Experimental {
    public abstract class BoolSyntax {
        public static BoolSyntax And(params BoolSyntax[] terms) {
            if (terms.Length == 1) {
                return terms[0];
            }

            return new BoolSyntaxTerm(terms.ToArray());
        }

        public static BoolSyntax Or(params BoolSyntax[] terms) {
            if (terms.Length == 1) {
                return terms[0];
            }

            return new BoolSyntaxPoly(terms.ToArray());
        }

        public static BoolSyntax Not(BoolSyntax arg) {
            return new BoolSyntaxNot(arg);
        }

        public static BoolSyntax Atom(IBoolAtom atom) {
            return new BoolAtomSyntax(atom);
        }

        private BoolSyntax() { }

        public abstract BoolSyntax Negate();

        public abstract bool IsCNF();

        public abstract BoolSyntax ToCNF();

        public abstract BoolSyntax Simplify();

        protected virtual BoolSyntaxPoly ToPoly() { return new BoolSyntaxPoly(new[] { this }); } 

        protected virtual BoolSyntaxTerm ToTerm() { return new BoolSyntaxTerm(new[] { this }); } 

        public class BoolSyntaxPoly : BoolSyntax {
            public HashSet<BoolSyntax> Arguments { get; }

            public BoolSyntaxPoly(params BoolSyntax[] args) {
                this.Arguments = args.ToHashSet();
            }

            public override BoolSyntax Negate() {
                return new BoolSyntaxTerm(this.Arguments.Select(x => x.Negate()).ToArray());
            }

            public override BoolSyntax ToCNF() {
                var args = this.Arguments
                    .Select(x => x.ToCNF())
                    .Select(x => x.ToPoly())
                    .SelectMany(x => x.Arguments)
                    .ToArray();

                return Or(args);
            }

            protected override BoolSyntaxPoly ToPoly() {
                return this;
            }

            public override string ToString() {
                return "(" + string.Join(" or ", this.Arguments) + ")";
            }

            public override bool IsCNF() {
                return this.Arguments.All(x => x is not BoolSyntaxPoly)
                    && this.Arguments.All(x => x.IsCNF());
            }

            public override bool Equals(object obj) {
                return obj is BoolSyntaxPoly poly && poly.Arguments.SetEquals(this.Arguments);
            }
            
            public override int GetHashCode() {
                return this.Arguments.Aggregate(7, (x, y) => x + y.GetHashCode());
            }

            public override BoolSyntax Simplify() {
                var args = this.Arguments
                    .Select(x => x.Simplify())
                    .Select(x => x.ToTerm())
                    .Where(x => x.Arguments.Count > 0)
                    .Select(x => And(x.Arguments.ToArray()))
                    .ToArray();

                return Or(args);
            }
        }

        public class BoolSyntaxTerm : BoolSyntax {
            public HashSet<BoolSyntax> Arguments { get; }

            public BoolSyntaxTerm(params BoolSyntax[] args) {
                this.Arguments = args.ToHashSet();
            }

            public override bool IsCNF() {
                return this.Arguments.All(x => x is not BoolSyntaxPoly) 
                    && this.Arguments.All(x => x is not BoolSyntaxTerm)
                    && this.Arguments.All(x => x.IsCNF());
            }

            public override BoolSyntax Negate() {
                return Or(this.Arguments.Select(x => x.Negate()).ToArray());
            }

            protected override BoolSyntaxTerm ToTerm() {
                return this;
            }

            public override BoolSyntax ToCNF() {
                if (this.IsCNF()) {
                    return this;
                }

                if (this.Arguments.Count == 1) {
                    return this.Arguments.First().ToCNF();
                }

                // First unbox all inner terms
                var args = this.Arguments
                    .Select(x => x.ToTerm())
                    .SelectMany(x => x.Arguments)
                    .ToArray();

                var first = args.First().ToCNF().ToPoly();
                var tail = args.Skip(1).ToArray();

                // Distribute the fail onto each argument in first
                var result = first.Arguments
                    .Select(x => x.ToTerm())
                    .Select(x => And(tail.Concat(x.Arguments).ToArray()))
                    .ToArray();

                return Or(result).ToCNF();
            }

            public override string ToString() {
                return "(" + string.Join(" and ", this.Arguments) + ")";
            }

            public override bool Equals(object obj) {
                return obj is BoolSyntaxTerm term && term.Arguments.SetEquals(this.Arguments);
            }
            
            // override object.GetHashCode
            public override int GetHashCode() {
                return this.Arguments.Aggregate(7, (x, y) => x + y.GetHashCode());
            }

            public override BoolSyntax Simplify() {
                var args = this.Arguments.ToHashSet();

                // Attempt to intersect any atoms that match each other
                foreach (var arg1 in this.Arguments) {
                    foreach (var arg2 in args.ToArray()) {
                        if (arg1 is BoolAtomSyntax s1 && arg2 is BoolAtomSyntax s2) {
                            args.Remove(arg1);
                            args.Remove(arg2);
                            args.UnionWith(s1.atom.TryUnionWith(s2.atom).Select(x => Atom(x)));
                        }
                    }
                }

                // Make sure there aren't contradictory terms
                foreach (var arg in args) {
                    if (args.Contains(arg.Negate())) {
                        return new BoolSyntaxTerm(new BoolSyntax[0]);
                    }
                }

                return And(args.ToArray());
            }
        }

        public class BoolSyntaxNot : BoolSyntax {        
            private readonly BoolSyntax target;

            public BoolSyntaxNot(BoolSyntax target) {
                this.target = target;
            }

            public override bool IsCNF() {
                return false;
            }

            public override BoolSyntax Negate() {
                return this.target;
            }

            public override BoolSyntax ToCNF()
            {
                return target.Negate().ToCNF();
            }

            public override string ToString() {
                return "!" + this.target;
            }

            public override bool Equals(object obj) {
                return obj is BoolSyntaxNot not && this.target.Equals(not.target);
            }

            public override int GetHashCode() {
                return 11 * this.target.GetHashCode();
            }

            public override BoolSyntax Simplify()
            {
                return this.target.Negate();
            }
        }

        public class BoolAtomSyntax : BoolSyntax {
            public IBoolAtom atom;

            public BoolAtomSyntax(IBoolAtom atom) {
                this.atom = atom;
            }

            public override bool IsCNF() {
                return true;
            }

            public override BoolSyntax Negate() {
                return new BoolAtomSyntax(this.atom.Negate());
            }

            public override BoolSyntax ToCNF() {
                return this;
            }

            public override string ToString() {
                return this.atom.ToString();
            }

            public override bool Equals(object obj) {
                return obj is BoolAtomSyntax other && this.atom.Equals(other.atom);
            }

            public override int GetHashCode() {
                return this.atom.GetHashCode();
            }

            public override BoolSyntax Simplify()
            {
                return this;
            }
        }
    }
}