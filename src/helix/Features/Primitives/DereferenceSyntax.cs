using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.Primitives {
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

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            if (this.isTypeChecked) {
                return this;
            }

            var target = this.target.CheckTypes(types).ToRValue(types);
            var pointerType = target.AssertIsPointer(types);

            if (pointerType.InnerType is PointerType || pointerType.InnerType is ArrayType) {
                // If we dereference a pointer and get another reference type, then we have no
                // idea where this new pointer came from because it could be aliased with something
                // else, so we need to emit a new root lifetime.
                var lifetime = new Lifetime(this.tempPath, 0, true);
                var result = new DereferenceSyntax(this.Location, target, this.tempPath, true);

                types.AvailibleLifetimes.Add(lifetime);
                types.ReturnTypes[result] = pointerType.InnerType;
                types.Lifetimes[result] = new[] { lifetime };

                return result;
            }
            else {
                var result = new DereferenceSyntax(this.Location, target, this.tempPath, true);

                types.ReturnTypes[result] = pointerType.InnerType;
                types.Lifetimes[result] = Array.Empty<Lifetime>();

                return result;
            }
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
            types.Lifetimes[result] = types.Lifetimes[this.target];

            // Since we're going to be an lvalue, we actually need to declare a 
            // variable signature so that mutations can keep track of which version
            // of the lifetime another variable references
            //var sig = new VariableSignature(this.tempPath, types.ReturnTypes[this], true, 0, false);

            //types.Variables[this.tempPath] = sig;
            //types.SyntaxValues[this.tempPath] = this;

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
            var lifetime = new Lifetime(this.tempPath, 0, true);

            writer.WriteEmptyLine();
            writer.WriteComment($"Line {this.Location.Line}: Pointer dereference");

            writer.WriteStatement(new CVariableDeclaration() { 
                Name = tempName,
                Type = tempType,
                Assignment = result
            });

            writer.RegisterLifetime(lifetime, new CMemberAccess() {
                Target = target,
                MemberName = "pool"
            });

            writer.WriteEmptyLine();

            return new CVariableLiteral(tempName);
        }        
    }
}
