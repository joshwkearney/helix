using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree VariableAccess() {
            var tok = this.Advance(TokenKind.Identifier);

            return new VariableAccessParseSyntax(tok.Location, tok.Value);
        }
    }
}

namespace Trophy {
    public record VariableAccessParseSyntax : ISyntaxTree {
        private readonly string name;

        public TokenLocation Location { get; }

        public VariableAccessParseSyntax(TokenLocation location, string name) {
            this.Location = location;
            this.name = name;
        }

        public Option<TrophyType> ToType(INamesRecorder names) {
            // Make sure this name exists
            if (!names.TryFindPath(this.name).TryGetValue(out var path)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            // Make sure this name exists
            if (!names.TryResolveName(path).TryGetValue(out var target)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            // Return primitive types if possible
            if (path == new IdentifierPath("int")) {
                return PrimitiveType.Int;
            }
            else if (path == new IdentifierPath("bool")) {
                return PrimitiveType.Bool;
            }
            else if (path == new IdentifierPath("void")) {
                return PrimitiveType.Void;
            }

            // If we're pointing at a struct or union return a named type
            if (target == NameTarget.Aggregate) {
                return new NamedType(path);
            }

            return Option.None;
        }

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            // Make sure this name exists
            if (!types.TryFindPath(this.name).TryGetValue(out var path)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            // Make sure this name exists
            if (!types.TryResolveName(path).TryGetValue(out var target)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            // Make sure we are accessing a variable
            if (target != NameTarget.Variable) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            var result = new VariableAccessSyntax(this.Location, path);
            types.SetReturnType(result, types.GetVariable(path).Type);

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => Option.None;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record VariableAccessSyntax : ISyntaxTree {
        private readonly IdentifierPath variablePath;

        public TokenLocation Location { get; }

        public VariableAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.variablePath = path;
        }

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) {
            // If we can't be an rvalue we definitely can't be an lvalue
            if (!this.ToRValue(types).HasValue) {
                return Option.None;
            }

            // Make sure this variable is writable
            if (!types.GetVariable(this.variablePath).IsWritable) {
                return Option.None;
            }

            var result = new LValueVariableAccessSyntax(this.Location, this.variablePath);
            types.SetReturnType(result, new PointerType(types.GetReturnType(this), true));

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => this;

        public CExpression GenerateCode(CStatementWriter writer) {
            var name = writer.GetVariableName(this.variablePath);

            return CExpression.VariableLiteral(name);
        }
    }

    public record LValueVariableAccessSyntax : ISyntaxTree {
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public LValueVariableAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.path = path;
        }

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => Option.None;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => this;

        public CExpression GenerateCode(CStatementWriter writer) {
            var name = writer.GetVariableName(this.path);

            return CExpression.AddressOf(CExpression.VariableLiteral(name));
        }
    }
}