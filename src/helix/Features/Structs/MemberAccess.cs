using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Aggregates;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Lifetimes;
using Helix.Features.Variables;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree MemberAccess(ISyntaxTree first) {
            this.Advance(TokenKind.Dot);

            var tok = this.Advance(TokenKind.Identifier);
            var loc = first.Location.Span(tok.Location);

            return new MemberAccessSyntax(loc, first, tok.Value);
        }
    }
}

namespace Helix.Features.Aggregates {
    public record MemberAccessSyntax : ISyntaxTree, ILValue {
        private readonly ISyntaxTree target;
        private readonly string memberName;
        private readonly bool isTypeChecked;
        private readonly bool isPointerAccess;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public bool IsLocal {
            get {
                if (this.target is ILValue lvalue) {
                    return lvalue.IsLocal;
                }

                return false;
            }
        }

        public MemberAccessSyntax(TokenLocation location, ISyntaxTree target, 
                                  string memberName, bool isPointerAccess = false,
                                  bool isTypeChecked = false) {

            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.isTypeChecked = isTypeChecked;
            this.isPointerAccess = isPointerAccess;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var target = this.target.CheckTypes(types).ToRValue(types);
            var targetType = types.ReturnTypes[target];

            // Handle getting the count of an array
            if (targetType is ArrayType array) {
                if (this.memberName == "count") {
                    var result = new MemberAccessSyntax(
                        this.Location,
                        target,
                        "count",
                        true);

                    types.ReturnTypes[result] = PrimitiveType.Int;
                    types.Lifetimes[result] = new LifetimeBundle();
                    return result;
                }
            }

            // If this is a named type it could be a struct or union
            if (targetType is NamedType named) {
                // If this is a struct or union we can access the fields
                if (types.Aggregates.TryGetValue(named.Path, out var sig)) {
                    var fieldOpt = sig
                        .Members
                        .Where(x => x.Name == this.memberName)
                        .FirstOrNone();

                    // Make sure this field is present
                    if (fieldOpt.TryGetValue(out var field)) {
                        var result = new MemberAccessSyntax(
                            this.Location,
                            target,
                            this.memberName,
                            isPointerAccess,
                            true);                       

                        types.ReturnTypes[result] = field.Type;
                        types.Lifetimes[result] = this.CalculateLifetimes(target, field.Type, types);

                        return result;
                    }                    
                }               
            }

            throw TypeCheckingErrors.MemberUndefined(this.Location, targetType, this.memberName);
        }

        private LifetimeBundle CalculateLifetimes(ISyntaxTree target, HelixType memberType, SyntaxFrame types) {
            var relPath = new IdentifierPath(this.memberName);
            var targetLifetimes = types.Lifetimes[target].ComponentLifetimes;
            var bundleDict = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>();

            foreach (var (memPath, type) in VariablesHelper.GetMemberPaths(memberType, types)) {
                bundleDict[memPath] = targetLifetimes[relPath.Append(memPath)];
            }

            return new LifetimeBundle(bundleDict);
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            return this;
        }

        public ILValue ToLValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            // Make sure this is a named type
            var target = this.target.ToLValue(types);

            var result = new MemberAccessSyntax(
                this.Location, 
                target, 
                this.memberName, 
                this.isPointerAccess,
                true);

            types.ReturnTypes[result] = types.ReturnTypes[this];
            types.Lifetimes[result] = types.Lifetimes[this];

            return result;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            return new CMemberAccess() {
                Target = this.target.GenerateCode(types, writer),
                MemberName = this.memberName,
                IsPointerAccess = this.isPointerAccess
            };
        }
    }    
}
