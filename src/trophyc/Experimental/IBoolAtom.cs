using System;
using System.Collections.Generic;

namespace Trophy.Experimental {
    public interface IBoolAtom {
        public IEnumerable<IBoolAtom> TryUnionWith(IBoolAtom other);

        public IBoolAtom Negate();
    }

    public enum ComparisonKind {
        LessThan, LessThanOrEqualTo
    }

    public class ComparisonAtom : IBoolAtom
    {
        private readonly ComparisonArgument arg1, arg2;

        private readonly ComparisonKind op;

        public ComparisonAtom(ComparisonArgument arg1, ComparisonArgument arg2, ComparisonKind op) {
            if (op == ComparisonKind.LessThanOrEqualTo && arg2.AsConstant().TryGetValue(out var con1)) {
                arg2 = ComparisonArgument.Constant(con1 + 1);
                op = ComparisonKind.LessThan;
            }
            else if (op == ComparisonKind.LessThanOrEqualTo && arg1.AsConstant().TryGetValue(out var con2)) {
                arg1 = ComparisonArgument.Constant(con2 - 1);
                op = ComparisonKind.LessThan;
            }

            this.arg1 = arg1;
            this.arg2 = arg2;
            this.op = op;
        }

        public IBoolAtom Negate()
        {
            if (this.op == ComparisonKind.LessThan) {
                return new ComparisonAtom(this.arg2, this.arg1, ComparisonKind.LessThanOrEqualTo);
            }
            else {
                return new ComparisonAtom(this.arg2, this.arg1, ComparisonKind.LessThan);
            }
        }

        public IEnumerable<IBoolAtom> TryUnionWith(IBoolAtom other)
        {
            if (!(other is ComparisonAtom atom)) {
                return new[] { this, other };
            }

            if (this.Equals(atom)) {
                return new[] { this };
            }

            // If the first argument is equal and the second arguments are both constants
            if (this.arg1.Equals(atom.arg1)) {
                if (this.arg2.AsConstant().TryGetValue(out var con1) && atom.arg2.AsConstant().TryGetValue(out var con2)) {
                    return new[] {new ComparisonAtom(
                        this.arg1,
                        ComparisonArgument.Constant(Math.Min(con1, con2)),
                        ComparisonKind.LessThan) };
                }
            }

            // If the second argument is equal and the first arguments are both constants
            if (this.arg2.Equals(atom.arg2)) {
                if (this.arg1.AsConstant().TryGetValue(out var con1) && atom.arg1.AsConstant().TryGetValue(out var con2)) {
                    return new[] { new ComparisonAtom(
                        ComparisonArgument.Constant(Math.Max(con1, con2)),
                        this.arg2,
                        ComparisonKind.LessThan) };
                }
            }

            return new[] { this, other };
        }

        public override string ToString()
        {
            var result = "(" + this.arg1.ToString();

            if (this.op == ComparisonKind.LessThan) {
                result += " < ";
            }
            else {
                result += " <= ";
            }

            return result + this.arg2.ToString() + ")";
        }
    }

    public abstract class ComparisonArgument {
        public static ComparisonArgument Constant(int value) {
            return new ConstantArg(value);
        }

        public static ComparisonArgument String(string value) {
            return new StringArg(value);
        }

        private ComparisonArgument() { }

        public virtual IOption<string> AsString() { return Option.None<string>(); }

        public virtual IOption<int> AsConstant() { return Option.None<int>(); }
        
        private class ConstantArg : ComparisonArgument {
            private readonly int value;

            public ConstantArg(int value) {
                this.value = value;
            }

            public override IOption<int> AsConstant() { return Option.Some(this.value); }

            public override bool Equals(object obj)
            {
                return obj is ConstantArg arg && this.value == arg.value;
            }

            public override int GetHashCode()
            {
                return this.value.GetHashCode();
            }

            public override string ToString()
            {
                return this.value.ToString();
            }
        }

        private class StringArg : ComparisonArgument {
            private readonly string value;

            public StringArg(string value) {
                this.value = value;
            }

            public override IOption<string> AsString() { return Option.Some(this.value); }

            public override string ToString()
            {
                return this.value;
            }

            public override bool Equals(object obj)
            {
                return obj is StringArg arg && this.value == arg.value;
            }

            public override int GetHashCode()
            {
                return this.value.GetHashCode();
            }
        }
    }

    public class UnionIsAtom : IBoolAtom {
        private readonly bool isPotitive;

        public string Identifier { get; }

        public string UnionField { get; }

        public UnionIsAtom(bool isPositive, string id, string field) {
            this.isPotitive = isPositive;
            this.Identifier = id;
            this.UnionField = field;
        }

        public IBoolAtom Negate() {
            return new UnionIsAtom(!this.isPotitive, this.Identifier, this.UnionField);
        }

        public IEnumerable<IBoolAtom> TryUnionWith(IBoolAtom other) {
            if (other is UnionIsAtom atom && this.Equals(atom)) {
                return new[] { this };
            }

            return new[] { this, other };
        }

        public override bool Equals(object obj) {
            return obj is UnionIsAtom atom 
                && this.isPotitive == atom.isPotitive 
                && this.Identifier == atom.Identifier 
                && this.UnionField == atom.UnionField;
        }

        public override int GetHashCode() {
            return this.isPotitive.GetHashCode()
                + this.Identifier.GetHashCode()
                + this.UnionField.GetHashCode();
        }

        public override string ToString() {
            return "(" + this.Identifier + (this.isPotitive ? " is " : " is not ") + this.UnionField + ")";
        }
    }
}