using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree VariableAccess() {
            var tok = this.Advance(TokenKind.Identifier);

            return new VariableAccessParseSyntax(tok.Location, tok.Value);
        }
    }
}

namespace Helix {
    public record VariableAccessParseSyntax : ISyntaxTree {
        private readonly string name;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public VariableAccessParseSyntax(TokenLocation location, string name) {
            this.Location = location;
            this.name = name;
        }

        public Option<HelixType> AsType(SyntaxFrame types) {
            // Make sure this name exists
            //if (!types.TryResolvePath(this.name).TryGetValue(out var path)) {
            //    throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            //}

            //// Return primitive types if possible
            //if (path == new IdentifierPath("int")) {
            //    return PrimitiveType.Int;
            //}
            //else if (path == new IdentifierPath("bool")) {
            //    return PrimitiveType.Bool;
            //}
            //else if (path == new IdentifierPath("void")) {
            //    return PrimitiveType.Void;
            //}

            // If we're pointing at a type then return it
            if (types.TryResolveName(this.Location.Scope, this.name, out var syntax)) {
                if (syntax.AsType(types).TryGetValue(out var type)) {
                    return type;
                }
            }

            return Option.None;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            // Make sure this name exists
            if (!types.TryResolvePath(this.Location.Scope, this.name, out var path)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            if (path == new IdentifierPath("void")) {
                return new VoidLiteral(this.Location).CheckTypes(types);
            }

            // Make sure we are accessing a variable
            if (types.Variables.TryGetValue(path, out var varSig)) {
                var result = new VariableAccessSyntax(this.Location, path);

                types.ReturnTypes[result] = varSig.Type;
                types.CapturedVariables[result] = varSig.CapturedVariables;

                return result;
            }

            if (types.Functions.ContainsKey(path)) {
                var result = new VariableAccessSyntax(this.Location, path);

                types.ReturnTypes[result] = new NamedType(path);
                types.CapturedVariables[result] = Array.Empty<IdentifierPath>();

                return result;
            }

            throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record VariableAccessSyntax : ISyntaxTree {
        private readonly IdentifierPath variablePath;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public VariableAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.variablePath = path;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            // Make sure this variable is writable
            if (!types.Variables[this.variablePath].IsWritable) {
                throw TypeCheckingErrors.WritingToConstVariable(this.Location);
            }

            var result = new LValueVariableAccessSyntax(this.Location, this.variablePath);
            types.ReturnTypes[result] = new PointerType(types.ReturnTypes[this], true);
            types.CapturedVariables[result] = types.CapturedVariables[this];

            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.variablePath);

            return new CVariableLiteral(name);
        }
    }

    public record LValueVariableAccessSyntax : ISyntaxTree {
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public LValueVariableAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.path = path;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToLValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            var name = writer.GetVariableName(this.path);

            return new CAddressOf() {
                Target = new CVariableLiteral(name)
            };
        }
    }
}