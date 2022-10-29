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
    public record MemberAccessSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly string memberName;
        private readonly bool isTypeChecked;
        private readonly bool isPointerAccess;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public bool IsLocal => true;

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
                    types.Lifetimes[result] = new ScalarLifetimeBundle();
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
                        
                        var relPath = new IdentifierPath(this.memberName);
                        var targetLifetimes = types.Lifetimes[target].ComponentLifetimes;
                        var bundleDict = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>();

                        foreach (var (memPath, type) in VariablesHelper.GetMemberPaths(field.Type, types)) {
                            bundleDict[memPath] = targetLifetimes[relPath.Append(memPath)];
                        }

                        types.ReturnTypes[result] = field.Type;
                        types.Lifetimes[result] = new StructLifetimeBundle(bundleDict);

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

        public ILValue ToLValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            // Make sure this is a named type
            var target = this.target.ToLValue(types);
            var named = (NamedType)types.ReturnTypes[this.target];
            var mem = types.Aggregates[named.Path].Members.First(x => x.Name == this.memberName);

            if (!mem.IsWritable) {
                throw TypeCheckingErrors.WritingToConstVariable(this.Location);
            }

            var relPath = new IdentifierPath(this.memberName);
            var targetLifetimes = types.Lifetimes[target].ComponentLifetimes;
            var bundleDict = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>();

            foreach (var (memPath, type) in VariablesHelper.GetMemberPaths(mem.Type, types)) {
                bundleDict[memPath] = targetLifetimes[relPath.Append(memPath)];
            }

            var result = new LValueMemberAccessSyntax(
                this.Location, 
                target, 
                this.memberName, 
                this.isPointerAccess);

            types.ReturnTypes[result] = mem.Type;
            types.Lifetimes[result] = new StructLifetimeBundle(bundleDict);

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

    public record LValueMemberAccessSyntax : ISyntaxTree, ILValue {
        private readonly ILValue target;
        private readonly string member;
        private readonly bool isPointerAccess;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public bool IsLocal => this.target.IsLocal;

        public LValueMemberAccessSyntax(TokenLocation loc, ILValue target, string member, bool isPointerAccess) {
            this.Location = loc;
            this.target = target;
            this.member = member;
            this.isPointerAccess = isPointerAccess;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ISyntaxTree ToLValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            return new CMemberAccess() {
                Target = this.target.GenerateCode(types, writer),
                MemberName = this.member,
                IsPointerAccess = this.isPointerAccess
            };
        }
    }
}
