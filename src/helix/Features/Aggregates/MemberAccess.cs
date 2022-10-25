using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Aggregates;
using Helix.Parsing;
using Helix.Generation.Syntax;

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
    public record MemberAccessSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly string memberName;
        private readonly bool isTypeChecked;
        private readonly bool isPointerAccess;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

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
                    types.Lifetimes[result] = Array.Empty<Lifetime>();
                    return result;
                }
            }

            bool isPointerAccess = false;
            if (targetType is PointerType pointer) {
                targetType = pointer.InnerType;
                isPointerAccess = true;

                // Member access through a pointer needs its own lifetime root
                throw new InvalidOperationException();
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
                        
                        // TODO: Handle this for real
                        if (field.Type.IsValueType(types)) {
                            types.Lifetimes[result] = Array.Empty<Lifetime>();
                        }
                        else {
                            // TODO: If this is a local struct (so named.Path.Append(this.memberName))
                            // exists, then only scope this lifetime to the member lifetime and not
                            // to the target lifetime
                            types.Lifetimes[result] = types.Lifetimes[target];
                        }

                        return result;
                    }
                    
                }               
            }

            throw TypeCheckingErrors.MemberUndefined(this.Location, targetType, this.memberName);
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            return this;
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            var type = types.ReturnTypes[this.target];
            if (type is PointerType point) {
                if (!point.IsWritable) {
                    throw TypeCheckingErrors.WritingToConstVariable(this.Location);
                }

                type = point.InnerType;
            }

            if (type is not NamedType named) {
                throw TypeCheckingErrors.LValueRequired(this.Location);
            }

            var mem = types.Aggregates[named.Path].Members.First(x => x.Name == this.memberName);
            if (!mem.IsWritable) {
                throw TypeCheckingErrors.WritingToConstVariable(this.Location);
            }

            var result = new LValueMemberAccessSyntax(this.Location, this.target, 
                                                      this.memberName, this.isPointerAccess);

            // 
            types.ReturnTypes[result] = new PointerType(mem.Type, true);
            types.Lifetimes[result] = types.Lifetimes[target]; ;

            return result;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            return new CMemberAccess() {
                Target = this.target.GenerateCode(types, writer),
                MemberName = this.memberName,
                IsPointerAccess = this.isPointerAccess
            };
        }
    }

    public record LValueMemberAccessSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly string member;
        private readonly bool isPointerAccess;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public LValueMemberAccessSyntax(TokenLocation loc, ISyntaxTree target, string member, bool isPointerAccess) {
            this.Location = loc;
            this.target = target;
            this.member = member;
            this.isPointerAccess = isPointerAccess;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ISyntaxTree ToLValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            return new CAddressOf() {
                Target = new CMemberAccess() {
                    Target = this.target.GenerateCode(types, writer),
                    MemberName = this.member,
                    IsPointerAccess = this.isPointerAccess
                }
            };
        }
    }
}
