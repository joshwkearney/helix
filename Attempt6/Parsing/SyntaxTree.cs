using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Attempt6.Parsing {
    public interface ISyntax {
        void Accept(ISyntaxVisitor visitor);
    }

    public interface ISyntaxVisitor {
        void Visit(Int32Literal syntax);
        void Visit(IdentifierSyntax syntax);
        void Visit(ListSyntax syntax);
    }

    public class Int32Literal : ISyntax, IAST, IEquatable<Int32Literal> {
        public int Value { get; }

        public Type ReturnType => typeof(int);

        public Int32Literal(int value) {
            this.Value = value;
        }

        public void Accept(IASTVisitor visitor) {
            visitor.Visit(this);
        }

        public bool Equals(Int32Literal other) {
            return other != null && this.Value == other.Value;
        }

        public override bool Equals(object obj) {
            if (obj is Int32Literal atom) {
                return this.Equals(atom);
            }
            else {
                return false;
            }
        }

        public override int GetHashCode() {
            return this.Value.GetHashCode();
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }

    public class IdentifierSyntax : ISyntax, IEquatable<IdentifierSyntax> {
        public string Value { get; }

        public IdentifierSyntax(string value) {
            this.Value = value;
        }

        public override bool Equals(object obj) {
            return this.Equals(obj as IdentifierSyntax);
        }

        public bool Equals(IdentifierSyntax other) {
            return other != null && this.Value == other.Value;
        }

        public override int GetHashCode() {
            return -1937169414 + EqualityComparer<string>.Default.GetHashCode(this.Value);
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }

    public class ListSyntax : ISyntax, IEquatable<ListSyntax> {
        public AssociativeList<ISyntax, ISyntax> List { get; }

        public ListSyntax(AssociativeList<ISyntax, ISyntax> values) {
            this.List = values;
        }

        public override bool Equals(object obj) {
            if (obj is ListSyntax syntax) {
                return this.Equals(syntax);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.List.Aggregate(-1937169414, (x, y) => x + y.GetHashCode());
        }

        public bool Equals(ListSyntax other) {
            if (other == null) {
                return false;
            }

            if (this.List.Count != other.List.Count) {
                return false;
            }

            foreach (var pair in this.List) {
                if (other.List.TryGetValue(pair.Key, out var otherValue)) {
                    if (!pair.Value.Equals(otherValue)) {
                        return false;
                    }
                }
                else {
                    return false;
                }
            }

            return true;
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}