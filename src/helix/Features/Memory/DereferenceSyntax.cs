using System;
using helix.FlowAnalysis;
using helix.Syntax;
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

            foreach (var (relPath, type) in pointerType.InnerType.GetMembers(types)) {
                // Add new roots to the current root set
                if (!type.IsValueType(types)) {
                    var lifetime = new Lifetime(this.tempPath.Append(relPath), 0);

                    types.LifetimeRoots[lifetime.Path] = lifetime;
                }
            }

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

            foreach (var (relPath, type) in pointerType.InnerType.GetMembers(flow)) {
                if (type.IsValueType(flow)) {
                    bundleDict[relPath] = Lifetime.None;
                }
                else { 
                    // This value's lifetime actually isn't the pointer's lifetime, but some
                    // other lifetime that outlives the pointer. It's important to represent
                    // this value like this because we can't store things into it that just
                    // outlive the pointer
                    var lifetime = new Lifetime(this.tempPath.Append(relPath), 0);

                    bundleDict[relPath] = lifetime;

                    // The lifetime that is stored in the pointer must outlive the pointer itself
                    flow.LifetimeGraph.RequireOutlives(lifetime, pointerLifetime);
                }
            }

            this.SetLifetimes(new LifetimeBundle(bundleDict), flow);
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);
            var pointerType = (PointerType)types.ReturnTypes[this.target];
            var tempName = writer.GetVariableName(this.tempPath);
            var tempType = writer.ConvertType(pointerType.InnerType);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Pointer dereference");
            writer.WriteStatement(new CVariableDeclaration() {
                Name = tempName,
                Type = tempType,
                Assignment = new CPointerDereference() {
                    Target = new CMemberAccess() {
                        Target = target,
                        MemberName = "data",
                        IsPointerAccess = false
                    }
                }
            });

            writer.WriteEmptyLine();

            var result = new CVariableLiteral(writer.GetVariableName(this.tempPath));
            writer.RegisterLifetimes(this.tempPath, this.GetLifetimes(types), result);

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

            foreach (var (relPath, _) in pointerType.InnerType.GetMembers(flow)) {
                // We are returning lifetimes that represent the minimum region
                // required to store something in this pointer
                var lifetime = new Lifetime(pointerLifetime.Path.Append(relPath), 0);

                bundleDict[relPath] = lifetime;

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
