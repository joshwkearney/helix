using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree VariableAccess() {
            var tok = this.Advance(TokenKind.Identifier);

            return new IdenfifierAccessParseTree(tok.Location, tok.Value);
        }
    }
}

namespace Trophy {
    public class IdenfifierAccessParseTree : ISyntaxTree {
        private readonly string name;

        public TokenLocation Location { get; }

        public IdenfifierAccessParseTree(TokenLocation location, string name) {
            this.Location = location;
            this.name = name;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) {
            return this.ResolveTypes(scope, types).ToType(scope, types);
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            // Make sure this name exists
            if (!types.TryFindPath(scope, this.name).TryGetValue(out var path)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            // Make sure this name exists
            if (!types.TryGetNameTarget(path).TryGetValue(out var target)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.name);
            }

            var result = new IdentifierAccessSyntax(this.Location, path, false);

            // If we are accessing a variable then update types accordingly
            if (target == NameTarget.Variable) {
                types.SetReturnType(result, types.GetVariableType(path));
            }

            return result;
        }

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) {
            throw new InvalidOperationException();
        }

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) {
            throw new InvalidOperationException();
        }

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter statWriter) {
            throw new InvalidOperationException();
        }
    }

    public class IdentifierAccessSyntax : ISyntaxTree {
        private readonly bool isLValue;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public IdentifierAccessSyntax(TokenLocation loc, IdentifierPath path, bool isLValue) {
            this.Location = loc;
            this.path = path;
            this.isLValue = isLValue;
        }

        public Option<TrophyType> ToType(IdentifierPath path, TypesRecorder types) {
            // Return primitive types if possible
            if (this.path == new IdentifierPath("int")) {
                return PrimitiveType.Int;
            }
            else if (this.path == new IdentifierPath("bool")) {
                return PrimitiveType.Bool;
            }
            else if (this.path == new IdentifierPath("void")) {
                return PrimitiveType.Void;
            }

            var target = types.TryGetNameTarget(this.path).GetValue();

            // If we're pointing at a struct or union return a named type
            if (target == NameTarget.Struct || target == NameTarget.Union) {
                return new NamedType(this.path);
            }

            return Option.None;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath path, TypesRecorder types) => this;

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) {
            // Make sure we haven't been converted into an lvalue
            if (this.isLValue) {
                return Option.None;
            }

            // Make sure we're pointing at a variable
            var target = types.TryGetNameTarget(this.path).GetValue();
            if (target != NameTarget.Variable) {
                return Option.None;
            }

            return this;
        }

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) {
            if (this.isLValue) {
                return this;
            }

            // If we can't be an rvalue we definitely can't be an lvalue
            if (!this.ToRValue(types).HasValue) {
                return Option.None;
            }

            // Make sure this variable is writable
            if (!types.GetVariableWritablility(this.path)) {
                return Option.None;
            }

            var result = new IdentifierAccessSyntax(this.Location, this.path, true);
            types.SetReturnType(result, new PointerType(types.GetReturnType(this)));

            return result;
        }

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter statWriter) {
            if (this.isLValue) {
                return CExpression.AddressOf(CExpression.VariableLiteral(this.path.ToCName()));
            }
            else {
                return CExpression.VariableLiteral(this.path.ToCName());
            }
        }
    }
}