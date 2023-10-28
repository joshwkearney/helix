using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Features.Variables;

namespace Helix.Parsing
{
    public partial class Parser {
        public ISyntaxTree DereferenceExpression(ISyntaxTree first) {
            var op = this.Advance(TokenKind.Star);
            var loc = first.Location.Span(op.Location);

            return new DereferenceParseSyntax(
                loc, 
                first);
        }
    }
}

namespace Helix.Features.Variables
{
    // Dereference syntax is split into three classes: this one that does
    // some basic type checking so it's easy for the parser to spit out
    // a single class, a dereference rvalue, and a dereference lvaulue.
    // This is for clarity because dereference rvalues and lvalues have
    // very different semantics, especially when it comes to lifetimes
    public record DereferenceParseSyntax : ISyntaxTree {
        private static int derefCounter = 0;
        private readonly ISyntaxTree target;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceParseSyntax(TokenLocation loc, ISyntaxTree target) {
            this.Location = loc;
            this.target = target;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            return this.target.AsType(types)
                .Select(x => new PointerType(x))
                .Select(x => (HelixType)x);
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            // We must specialize into rvalue/lvalue, which happens later
            // For now, do nothing

            return this;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            var path = types.Scope.Append("$deref" + derefCounter++);
            var target = this.target.CheckTypes(types).ToRValue(types);

            return new DereferenceSyntax(this.Location, target, path, false).CheckTypes(types);
        }

        public ISyntaxTree ToLValue(TypeFrame types) {
            var path = types.Scope.Append("$deref" + derefCounter++);
            var target = this.target.CheckTypes(types).ToRValue(types);

            return new DereferenceSyntax(this.Location, target, path, true).CheckTypes(types);
        }
    }

    public record DereferenceSyntax : ISyntaxTree {
        private readonly bool isLValue;
        private readonly ISyntaxTree target;
        private readonly IdentifierPath tempPath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => new[] { this.target };

        public bool IsPure => this.target.IsPure;

        public DereferenceSyntax(
            TokenLocation loc, 
            ISyntaxTree target, 
            IdentifierPath tempPath,
            bool isLValue) {

            this.Location = loc;
            this.target = target;
            this.tempPath = tempPath;
            this.isLValue = isLValue;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            if (target.GetReturnType(types) is NominalType nom && nom.Kind == NominalTypeKind.Variable) {
                var sig = nom.AsVariable(types).GetValue();
                var access = new VariableAccessSyntax(target.Location, nom.Path, sig).CheckTypes(types);
                    
                if (this.isLValue) {
                    return access.ToLValue(types);
                }
                else {
                    return access.ToRValue(types);
                }
            }

            var pointerType = this.target.AssertIsPointer(types);

            HelixType returnType;
            if (this.isLValue) {
                returnType = this.target.GetReturnType(types);
            }
            else {
                returnType = pointerType.InnerType;
            }

            SyntaxTagBuilder.AtFrame(types)
                .WithChildren(this.target)
                .WithReturnType(returnType)
                .BuildFor(this);

            return this;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var target = this.target.GenerateCode(types, writer);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Pointer dereference");

            if (this.isLValue) {
                return new CMemberAccess() {
                    Target = target,
                    MemberName = "data"
                };
            }
            else {
                var pointerType = this.target.AssertIsPointer(types);
                var tempName = writer.GetVariableName(this.tempPath);
                var tempType = writer.ConvertType(pointerType.InnerType, types);

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
                writer.VariableKinds[this.tempPath] = CVariableKind.Local;

                return new CVariableLiteral(tempName);
            }
        }
    }
}
