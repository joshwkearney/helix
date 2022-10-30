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
        private readonly bool isTypeChecked;
        private readonly bool isLValue;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public bool IsLocal => false;

        public DereferenceSyntax(TokenLocation loc, ISyntaxTree target, 
            IdentifierPath tempPath, bool isTypeChecked = false, bool islvalue = false) {

            this.Location = loc;
            this.target = target;
            this.tempPath = tempPath;
            this.isTypeChecked = isTypeChecked;
            this.isLValue = islvalue;
        }

        public Option<HelixType> AsType(SyntaxFrame types) {
            return this.target.AsType(types)
                .Select(x => new PointerType(x, true))
                .Select(x => (HelixType)x);
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            if (this.isTypeChecked) {
                return this;
            }

            var target = this.target.CheckTypes(types).ToRValue(types);
            var pointerType = target.AssertIsPointer(types);
            var result = new DereferenceSyntax(this.Location, target, this.tempPath, true);

            types.ReturnTypes[result] = pointerType.InnerType;
            types.Lifetimes[result] = this.CalculateLifetimes(pointerType, types);

            return result;
        }

        public LifetimeBundle CalculateLifetimes(PointerType pointerType, SyntaxFrame types) {
            var bundleDict = new Dictionary<IdentifierPath, IReadOnlyList<Lifetime>>();

            foreach (var (compPath, type) in VariablesHelper.GetMemberPaths(pointerType.InnerType, types)) {
                if (type is PointerType || type is ArrayType) {
                    var lifetime = new Lifetime(this.tempPath.Append(compPath), 0, true);

                    bundleDict[compPath] = new[] { lifetime };
                    types.LifetimeGraph.AddRoot(lifetime);
                }
                else {
                    bundleDict[compPath] = Array.Empty<Lifetime>();
                }
            }

            return new LifetimeBundle(bundleDict);
        }

        public ILValue ToLValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            if (this.isLValue) {
                return this;
            }

            var result = new DereferenceSyntax(
                this.Location, 
                this.target, 
                this.tempPath,
                true, 
                true);

            types.ReturnTypes[result] = types.ReturnTypes[this];
            types.Lifetimes[result] = types.Lifetimes[this];

            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            return this;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);
            var result = new CPointerDereference() {
                Target = new CMemberAccess() {
                    Target = target,
                    MemberName = "data"
                }
            };

            var returnType = types.ReturnTypes[this];

            writer.WriteEmptyLine();

            foreach (var (relPath, type) in VariablesHelper.GetMemberPaths(returnType, types)) {
                if (type is not PointerType && type is not ArrayType) {
                    continue;
                }

                var lifetime = new Lifetime(this.tempPath.Append(relPath), 0, true);

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
