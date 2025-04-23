using Helix.Analysis;
using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Collections;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Unions {
    public class IsUnionMemberPredicate : SyntaxPredicateLeaf {
        public IdentifierPath TargetPath { get; }

        public ValueSet<string> MemberNames { get; }

        public UnionType UnionSignature { get; }

        public IsUnionMemberPredicate(IdentifierPath targetPath, ValueSet<string> memberNames, 
                                      UnionType unionSig) {
            this.TargetPath = targetPath;
            this.MemberNames = memberNames;
            this.UnionSignature = unionSig;
        }

        public override IReadOnlyList<ISyntaxTree> ApplyToTypes(TokenLocation loc, TypeFrame types) {
            if (this.MemberNames.Count != 1) {
                return Array.Empty<ISyntaxTree>();
            }

            var memName = this.MemberNames.First();
            var member = this.UnionSignature.Members.First(x => x.Name == memName);

            if (!types.TryGetVariable(this.TargetPath, out var varSig)) {
                throw new Exception();
            }

            var inject = new FlowVarStatement(loc, member, this.TargetPath, varSig);
            return new[] { inject };
        }

        public override bool TryOrWith(ISyntaxPredicate pred, out ISyntaxPredicate result) {
            if (pred is not IsUnionMemberPredicate other) {
                result = null;
                return false;
            }

            if (other.TargetPath != this.TargetPath || other.UnionSignature != this.UnionSignature) {
                result = null;
                return false;
            }

            result = new IsUnionMemberPredicate(
                this.TargetPath,
                this.MemberNames.Union(other.MemberNames),
                this.UnionSignature);

            return true;
        }

        public override bool TryAndWith(ISyntaxPredicate pred, out ISyntaxPredicate result) {
            if (pred is not IsUnionMemberPredicate other) {
                result = null;
                return false;
            }

            if (other.TargetPath != this.TargetPath || other.UnionSignature != this.UnionSignature) {
                result = null;
                return false;
            }

            var overlap = this.MemberNames.Intersect(other.MemberNames);

            if (overlap.Count == 0) {
                result = ISyntaxPredicate.Empty;
                return true;
            }

            result = new IsUnionMemberPredicate(
                this.TargetPath,
                overlap,
                this.UnionSignature);

            return true;
        }

        public override ISyntaxPredicate Negate() {
            return new IsUnionMemberPredicate(
                this.TargetPath,
                this.UnionSignature.Members
                    .Select(x => x.Name)
                    .ToValueSet()
                    .Except(this.MemberNames),
                this.UnionSignature);
        }

        public override bool Equals(object other) {
            if (other is ISyntaxPredicate pred) {
                return this.Equals(pred);
            }

            return false;
        }

        public override int GetHashCode() {
            return this.TargetPath.GetHashCode()
                + 11 * this.UnionSignature.GetHashCode()
                + 13 * this.MemberNames.GetHashCode();
        }

        public override bool Equals(ISyntaxPredicate other) {
            if (other is not IsUnionMemberPredicate pred) {
                return false;
            }

            return this.MemberNames == pred.MemberNames
                && this.TargetPath == pred.TargetPath
                && this.UnionSignature == pred.UnionSignature;
        }

        public override string ToString() {
            if (this.MemberNames.Count == 1) {
                return this.TargetPath.Segments.Last() + $" is {this.MemberNames.First()}";
            }
            else {
                return this.TargetPath.Segments.Last() + " is { " + string.Join("; ", this.MemberNames) + " }";
            }
        }
    }
}