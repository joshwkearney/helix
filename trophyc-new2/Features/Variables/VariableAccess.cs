using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.Syntax;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax VariableAccess() {
            var tok = this.Advance(TokenKind.Identifier);

            return new VariableAccessParseSyntax(tok.Location, tok.Value);
        }
    }
}

namespace Trophy {
    public record VariableAccessParseSyntax : ISyntax {
        private readonly string name;

        public TokenLocation Location { get; }

        public VariableAccessParseSyntax(TokenLocation location, string name) {
            this.Location = location;
            this.name = name;
        }

        public Option<TrophyType> AsType(ITypesRecorder types) {
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
            if (types.TryResolveValue(this.name).TryGetValue(out var syntax)) {
                if (syntax.AsType(types).TryGetValue(out var type)) {
                    return type;
                }
            }

            return Option.None;
        }

        public ISyntax CheckTypes(ITypesRecorder types) {
            // Make sure this name exists
            if (!types.TryResolvePath(this.name).TryGetValue(out var path)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            // Make sure we are accessing a variable
            if (!types.TryGetVariable(path).HasValue) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            var result = new VariableAccessSyntax(this.Location, path);
            types.SetReturnType(result, types.GetVariable(path).Type);

            return result;
        }

        public ISyntax ToRValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public ISyntax ToLValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record VariableAccessSyntax : ISyntax {
        private readonly IdentifierPath variablePath;

        public TokenLocation Location { get; }

        public VariableAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.variablePath = path;
        }

        public ISyntax CheckTypes(ITypesRecorder types) => this;

        public ISyntax ToLValue(ITypesRecorder types) {
            // Make sure this variable is writable
            if (!types.GetVariable(this.variablePath).IsWritable) {
                throw TypeCheckingErrors.WritingToConstVariable(this.Location);
            }

            var result = new LValueVariableAccessSyntax(this.Location, this.variablePath);
            types.SetReturnType(result, new PointerType(types.GetReturnType(this), true));

            return result;
        }

        public ISyntax ToRValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            var name = writer.GetVariableName(this.variablePath);

            return new CVariableLiteral(name);
        }
    }

    public record LValueVariableAccessSyntax : ISyntax {
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public LValueVariableAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.path = path;
        }

        public ISyntax CheckTypes(ITypesRecorder types) => this;

        public ISyntax ToLValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            var name = writer.GetVariableName(this.path);

            return new CAddressOf() {
                Target = new CVariableLiteral(name)
            };
        }
    }
}