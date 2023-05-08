using System;
using Helix.Analysis;
using Helix.Analysis.Lifetimes;
using Helix.Analysis.Types;
using Helix.Features.Memory;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Parsing {
    public partial class Parser {
        private int dereferenceCounter = 0;

        public ISyntaxTree DereferenceExpression(ISyntaxTree first) {
            var op = this.Advance(TokenKind.Star);
            var loc = first.Location.Span(op.Location);

            return new DereferenceSyntax(loc, first, this.scope.Append("$deref_" + dereferenceCounter++));
        }
    }
}

namespace Helix.Features.Memory {
    public record DereferenceSyntax : ISyntaxTree, ILValue {
        private readonly ISyntaxTree target;
        private readonly IdentifierPath tempPath;
        private readonly bool isLValue;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public bool IsLocalVariable => false;

        public DereferenceSyntax(TokenLocation loc, ISyntaxTree target, 
            IdentifierPath tempPath, bool islvalue = false) {

            this.Location = loc;
            this.target = target;
            this.tempPath = tempPath;
            this.isLValue = islvalue;
        }

        public Option<HelixType> AsType(EvalFrame types) {
            return this.target.AsType(types)
                .Select(x => new PointerType(x, true))
                .Select(x => (HelixType)x);
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var target = this.target.CheckTypes(types).ToRValue(types);
            var pointerType = target.AssertIsPointer(types);
            var result = new DereferenceSyntax(this.Location, target, this.tempPath, true);

            types.ReturnTypes[result] = pointerType.InnerType;
            return result;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            var pointerType = this.target.AssertIsPointer(flow);
            var bundleDict = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>();

            this.target.AnalyzeFlow(flow);

            foreach (var (compPath, type) in VariablesHelper.GetMemberPaths(pointerType.InnerType, flow)) {
                if (type.IsValueType(flow)) {
                    bundleDict[compPath] = Array.Empty<Lifetime>();
                }
                else { 
                    var lifetime = new Lifetime(this.tempPath.Append(compPath), 0);

                    bundleDict[compPath] = new[] { lifetime };
                    flow.LifetimeGraph.AddRoot(lifetime);
                }
            }

            flow.Lifetimes[this.target] = new LifetimeBundle(bundleDict);
        }

        public ILValue ToLValue(EvalFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            if (this.isLValue) {
                return this;
            }
            else {
                var result = new DereferenceSyntax(
                    this.Location,
                    this.target,
                    this.tempPath,
                    true);

                types.ReturnTypes[result] = types.ReturnTypes[this];

                return result;
            }
        }

        public ISyntaxTree ToRValue(EvalFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return this;
        }

        public ICSyntax GenerateCode(EvalFrame types, ICStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);
            var result = new CPointerDereference() {
                Target = new CMemberAccess() {
                    Target = target,
                    MemberName = "data"
                }
            };

            var returnType = types.ReturnTypes[this];

            // Register our member paths with the code generator
            foreach (var (relPath, _) in VariablesHelper.GetMemberPaths(returnType, types)) {
                writer.SetMemberPath(this.tempPath, relPath);
            }

            writer.WriteEmptyLine();


            foreach (var (relPath, _) in VariablesHelper.GetMemberPaths(returnType, types)) {
                writer.SetMemberPath(this.tempPath, relPath);

                var lifetime = new Lifetime(this.tempPath.Append(relPath), 0);

                writer.WriteComment($"Line {this.Location.Line}: Saving lifetime '{lifetime.Path}'");

                var hack = new CMemberAccess() {
                    Target = target,
                    MemberName = "data"
                };

                foreach (var segment in relPath.Segments) {
                    hack = new CMemberAccess() {
                        Target = hack,
                        MemberName = segment,
                        IsPointerAccess = true
                    };
                }

                writer.RegisterLifetime(lifetime, new CMemberAccess() {
                    Target = hack,
                    MemberName = "pool"
                });

                writer.WriteEmptyLine();
            }

            if (this.isLValue) {
                return result;
            }

            var pointerType = (PointerType)types.ReturnTypes[this.target];
            if (pointerType.InnerType is not PointerType && pointerType.InnerType is not ArrayType) {
                return result;
            }

            // If we are dereferencing a pointer or array, we need to put it in a 
            // temp variable and write out the new lifetime.

            var tempName = writer.GetVariableName(this.tempPath);
            var tempType = writer.ConvertType(pointerType.InnerType);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Pointer dereference");

            writer.WriteStatement(new CVariableDeclaration() { 
                Name = tempName,
                Type = tempType,
                Assignment = result
            });

            writer.WriteEmptyLine();

            return new CVariableLiteral(tempName);
        }        
    }
}
