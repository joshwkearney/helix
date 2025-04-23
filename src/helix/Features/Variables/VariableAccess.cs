using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Features.Functions;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree VariableAccess() {
            var tok = this.Advance(TokenKind.Identifier);

            return new VariableAccessParseSyntax(tok.Location, tok.Value);
        }
    }
}

namespace Helix.Features.Variables {
    public record VariableAccessParseSyntax : ISyntaxTree {
        public string Name { get; }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public VariableAccessParseSyntax(TokenLocation location, string name) {
            this.Location = location;
            this.Name = name;
        }

        public Option<HelixType> AsType(TypeFrame types) {
            // If we're pointing at a type then return it
            if (types.TryResolveName(types.Scope, this.Name, out var type)) {
                return type;
            }

            return Option.None;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            // Make sure this name exists
            if (!types.TryResolvePath(types.Scope, this.Name, out var path)) {
                throw TypeException.VariableUndefined(this.Location, this.Name);
            }

            if (path == new IdentifierPath("void")) {
                return new VoidLiteral(this.Location).CheckTypes(types);
            }

            // See if we are accessing a variable
            if (types.TryGetVariable(path, out var type)) {
                return new VariableAccessSyntax(this.Location, path, type).CheckTypes(types);
            }

            // See if we are accessing a function
            if (types.TryGetFunction(path, out var _)) {
                return new FunctionAccessSyntax(this.Location, path).CheckTypes(types);
            }

            throw TypeException.VariableUndefined(this.Location, this.Name);
        }
    }

    public record VariableAccessSyntax : ISyntaxTree {
        private readonly bool isLValue;

        public PointerType VariableSignature { get; }

        public IdentifierPath VariablePath { get; }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public VariableAccessSyntax(TokenLocation loc, IdentifierPath path, PointerType sig, bool isLValue = false) {
            this.Location = loc;
            this.VariableSignature = sig;
            this.VariablePath = path;
            this.isLValue = isLValue;
        }

        public virtual ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var cap = new VariableCapture(
                this.VariablePath,
                VariableCaptureKind.ValueCapture,
                this.VariableSignature);

            types.SyntaxTags[this] = new SyntaxTagBuilder(types)
                .WithReturnType(this.VariableSignature.InnerType)
                .Build();

            return this;
        }

        public virtual ISyntaxTree ToLValue(TypeFrame types) {
            ISyntaxTree result = new VariableAccessSyntax(
                this.Location,
                this.VariablePath,
                this.VariableSignature,
                true);
            
            result = result.CheckTypes(types);

            var captured = new VariableCapture(
                this.VariablePath,
                VariableCaptureKind.LocationCapture,
                this.VariableSignature);

            types.SyntaxTags[result] = new SyntaxTagBuilder(types)
                .WithReturnType(new NominalType(this.VariablePath, NominalTypeKind.Variable))
                .Build();

            return result;
        }

        public ISyntaxTree ToRValue(TypeFrame types) {
            var hasSingularValue = types.Locals[this.VariablePath].Type
                .AsVariable(types)
                .SelectMany(x => x.InnerType.ToSyntax(this.Location, types))
                .TryGetValue(out var singularSyntax);

            if (hasSingularValue) {
                return singularSyntax.CheckTypes(types);
            }

            return this;
        }

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            ICSyntax result = new CVariableLiteral(writer.GetVariableName(this.VariablePath));

            if (writer.VariableKinds[this.VariablePath] == CVariableKind.Allocated) {
                result = new CPointerDereference() {
                    Target = result
                };
            }

            if (this.isLValue) {
                result = new CAddressOf() {
                    Target = result
                };
            }

            return result;
        }
    }
}