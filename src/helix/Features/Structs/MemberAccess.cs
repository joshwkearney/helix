using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Aggregates;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Lifetimes;
using Helix.Features.Variables;
using System;
using System.Reflection;

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
        private readonly bool isPointerAccess;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public bool IsLocalVariable {
            get {
                if (this.target is ILValue lvalue) {
                    return lvalue.IsLocalVariable;
                }

                return false;
            }
        }

        public MemberAccessSyntax(TokenLocation location, ISyntaxTree target, 
                                  string memberName, bool isPointerAccess = false) {

            this.Location = location;
            this.target = target;
            this.memberName = memberName;
            this.isPointerAccess = isPointerAccess;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var target = this.target.CheckTypes(types).ToRValue(types);
            var targetType = types.ReturnTypes[target];

            // Handle getting the count of an array
            if (targetType is ArrayType array) {
                if (this.memberName == "count") {
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
                // If this is a struct or union we can access the fields
                if (types.Structs.TryGetValue(named.Path, out var sig)) {
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
                            isPointerAccess);                       

                        types.ReturnTypes[result] = field.Type;

                        return result;
                    }                    
                }               
            }

            throw TypeCheckingErrors.MemberUndefined(this.Location, targetType, this.memberName);
        }

        public ISyntaxTree ToRValue(EvalFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return this;
        }

        public ILValue ToLValue(EvalFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            // Make sure this is a named type
            var target = this.target.ToLValue(types);

            var result = new MemberAccessSyntax(
                this.Location, 
                target, 
                this.memberName, 
                this.isPointerAccess);

            types.ReturnTypes[result] = types.ReturnTypes[this];

            return result;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (!this.IsFlowAnalyzed(flow)) {
                return;
            }

            this.target.AnalyzeFlow(flow);

            var memberType = this.GetReturnType(flow);
            var relPath = new IdentifierPath(this.memberName);
            var targetLifetimes = this.target.GetLifetimes(flow).ComponentLifetimes;
            var bundleDict = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>();

            foreach (var (memPath, type) in VariablesHelper.GetMemberPaths(memberType, flow)) {
                //if (type.IsValueType(flow)) {
                //    bundleDict[memPath] = new Lifetime[0];
                //}
                //else {
                    bundleDict[memPath] = targetLifetimes[relPath.Append(memPath)];
                //}
            }

            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            if (!this.IsTypeChecked(types)) {
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
