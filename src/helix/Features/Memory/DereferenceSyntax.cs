using System;
using helix.FlowAnalysis;
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
    public record DereferenceSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceSyntax(TokenLocation loc, ISyntaxTree target, 
            IdentifierPath tempPath) {

            this.Location = loc;
            this.target = target;
            this.tempPath = tempPath;
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
            var result = new DereferenceSyntax(this.Location, target, this.tempPath);

            types.ReturnTypes[result] = pointerType.InnerType;
            return result;
        }

        public ISyntaxTree ToRValue(EvalFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return this;
        }

        public ISyntaxTree ToLValue(EvalFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return new DereferenceLValue(this.Location, this.target).CheckTypes(types);
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            this.target.AnalyzeFlow(flow);

            var pointerType = this.target.AssertIsPointer(flow);
            var pointerLifetime = this.target.GetLifetimes(flow).Components[new IdentifierPath()];
            var bundleDict = new Dictionary<IdentifierPath, Lifetime>();

            foreach (var (compPath, type) in pointerType.InnerType.GetMembers(flow)) {
                if (type.IsValueType(flow)) {
                    bundleDict[compPath] = Lifetime.None;
                }
                else { 
                    var lifetime = new Lifetime(
                        this.tempPath.Append(compPath),
                        0,
                        LifetimeKind.Root);

                    bundleDict[compPath] = lifetime;

                    // The lifetime that is stored in the pointer must outlive the pointer itself
                    flow.LifetimeGraph.RequireOutlives(lifetime, pointerLifetime);
                }
            }

            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
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

                var lifetime = new Lifetime(this.tempPath.Append(relPath), 0, LifetimeKind.Root);

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

    public record DereferenceLValue : ISyntaxTree {
        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceLValue(TokenLocation loc, ISyntaxTree target) {
            this.Location = loc;
            this.target = target;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            this.SetReturnType(this.target.GetReturnType(types), types);
            return this;
        }

        public ISyntaxTree ToLValue(EvalFrame types) {
            if (!this.IsTypeChecked(types)) {
                throw new InvalidOperationException();
            }

            return this;
        }

        public void AnalyzeFlow(FlowFrame flow) {
            if (this.IsFlowAnalyzed(flow)) {
                return;
            }

            this.target.AnalyzeFlow(flow);

            var pointerType = this.target.AssertIsPointer(flow);
            var pointerLifetime = this.target.GetLifetimes(flow).Components[new IdentifierPath()];
            var bundleDict = new Dictionary<IdentifierPath, Lifetime>();

            foreach (var (compPath, _) in pointerType.InnerType.GetMembers(flow)) {
                var lifetime = new Lifetime(
                    pointerLifetime.Path.Append(compPath),
                    0,
                    LifetimeKind.Root);

                bundleDict[compPath] = lifetime;

                // The lifetime that is stored in the pointer must outlive the pointer itself
                flow.LifetimeGraph.RequireOutlives(lifetime, pointerLifetime);
            }

            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);
            var result = new CMemberAccess() {
                Target = target,
                MemberName = "data"
            };

            return result;
        }
    }
}
