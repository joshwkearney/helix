using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Aggregates;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using System.Reflection;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

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
        private readonly bool isWritable;

        public ISyntaxTree Target { get; }

        public string MemberName { get; }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.Target };

        public bool IsPure => this.Target.IsPure;

        public MemberAccessSyntax(TokenLocation location, ISyntaxTree target, 
                                  string memberName, bool isWritable = false) {

            this.Location = location;
            this.Target = target;
            this.MemberName = memberName;
            this.isWritable = isWritable;
        }

        public virtual ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var target = this.Target.CheckTypes(types).ToRValue(types);
            var targetType = types.ReturnTypes[target];

            // Handle getting the count of an array
            if (targetType is ArrayType array) {
                if (this.MemberName == "count") {
                    var result = new MemberAccessSyntax(
                        this.Location,
                        target,
                        "count",
                        false);

                    types.ReturnTypes[result] = PrimitiveType.Int;
                    return result;
                }
            }

            // If this is a named type it could be a struct or union
            if (targetType is NamedType named) {
                // If this is a struct we can access the fields
                if (types.Structs.TryGetValue(named.Path, out var sig)) {
                    var fieldOpt = sig
                        .Members
                        .Where(x => x.Name == this.MemberName)
                        .FirstOrNone();

                    // Make sure this field is present
                    if (fieldOpt.TryGetValue(out var field)) {
                        var result = new MemberAccessSyntax(
                            this.Location,
                            target,
                            this.MemberName,
                            field.IsWritable);                       

                        types.ReturnTypes[result] = field.Type;

                        return result;
                    }                    
                }               
            }

            throw TypeException.MemberUndefined(this.Location, targetType, this.MemberName);
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return this;
        }

        public ISyntaxTree ToLValue(TypeFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            if (!this.isWritable) {
                throw TypeException.LValueRequired(this.Location);
            }

            var target = this.Target.ToLValue(types);
            var result = new MemberAccessLValue(this.Location, target, this.MemberName, this.GetReturnType(types));

            return result.CheckTypes(types);
        }

        public virtual void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            this.Target.AnalyzeFlow(flow);

            var memberType = this.GetReturnType(flow);
            var targetLifetimes = this.Target.GetLifetimes(flow);
            var bundleDict = new Dictionary<IdentifierPath, Lifetime>();

            foreach (var (relPath, type) in memberType.GetMembers(flow)) {
                var memPath = new IdentifierPath(this.MemberName).Append(relPath);
                var varPath = targetLifetimes[memPath].Path;
                    
                if (type.IsValueType(flow)) {
                    bundleDict[relPath] = Lifetime.None;
                }
                else {
                    bundleDict[relPath] = flow.VariableLifetimes[varPath].RValue;
                }
            }

            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public virtual ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return new CMemberAccess() {
                Target = this.Target.GenerateCode(types, writer),
                MemberName = this.MemberName,
                IsPointerAccess = false
            };
        }
    }

    public record MemberAccessLValue : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly string memberName;
        private readonly HelixType memberType;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public MemberAccessLValue(TokenLocation location, ISyntaxTree target, 
                                  string memberName, HelixType memberType) {
            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.memberType = memberType;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            this.SetReturnType(new PointerType(this.memberType, true), types);
            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            this.target.AnalyzeFlow(flow);
            this.SetLifetimes(this.target.GetLifetimes(flow), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            // Our target will be converted to an lvalue, so we have to dereference it first
            var target = new CPointerDereference() {
                Target = this.target.GenerateCode(types, writer)
            };

            return new CAddressOf() {
                Target = new CMemberAccess() {
                    Target = target,
                    MemberName = this.memberName,
                    IsPointerAccess = false
                }
            };
        }
    }
}
