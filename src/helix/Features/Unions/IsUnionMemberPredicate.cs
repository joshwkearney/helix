using Helix.Analysis;
using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Collections;
using Helix.Features.Unions.ParseSyntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Unions {
    public class IsUnionMemberPredicate : SyntaxPredicateLeaf {
        public required IdentifierPath TargetPath { get; init; }

        public required ValueSet<string> MemberNames { get; init; }

        public required UnionType UnionSignature { get; init; }

        public override IReadOnlyList<IParseSyntax> ApplyToTypes(TokenLocation loc, TypeFrame types) {
            if (this.MemberNames.Count != 1) {
                return Array.Empty<IParseSyntax>();
            }

            var memName = this.MemberNames.First();
            var member = this.UnionSignature.Members.First(x => x.Name == memName);

            if (!types.TryGetVariable(this.TargetPath, out var varSig)) {
                throw new Exception();
            }

            var inject = new FlowVarParseSyntax {
                Location = loc,
                UnionMember = member,
                ShadowedPath = this.TargetPath,
                ShadowedType = varSig
            };

            return [inject];
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

            result = new IsUnionMemberPredicate {
                TargetPath = this.TargetPath,
                UnionSignature = this.UnionSignature,
                MemberNames = this.MemberNames.Union(other.MemberNames)
            };

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
            
            result = new IsUnionMemberPredicate {
                TargetPath = this.TargetPath,
                UnionSignature = this.UnionSignature,
                MemberNames = overlap
            };

            return true;
        }

        public override ISyntaxPredicate Negate() {
            var result = new IsUnionMemberPredicate {
                TargetPath = this.TargetPath,
                UnionSignature = this.UnionSignature,
                MemberNames = this.UnionSignature.Members
                    .Select(x => x.Name)
                    .ToValueSet()
                    .Except(this.MemberNames)
            };

            return result;
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