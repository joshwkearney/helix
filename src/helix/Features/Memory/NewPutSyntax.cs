using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Features.Aggregates;
using Helix.Features.Memory;
using Helix.Analysis.Lifetimes;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree NewPutExpression() {
            TokenLocation start;
            bool isNew;

            if (this.Peek(TokenKind.NewKeyword)) {
                start = this.Advance(TokenKind.NewKeyword).Location;
                isNew = true;
            }
            else {
                start = this.Advance(TokenKind.PutKeyword).Location;
                isNew = false;
            }

            var targetType = this.TopExpression();
            var loc = start.Span(targetType.Location);

            if (!this.TryAdvance(TokenKind.OpenBrace)) {
                return new NewPutSyntax(
                    loc, 
                    targetType,
                    isNew,
                    Array.Empty<string>(), 
                    Array.Empty<ISyntaxTree>());
            }

            var names = new List<string?>();
            var values = new List<ISyntaxTree>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                string? name = null;

                if (this.Peek(TokenKind.Identifier)) {
                    name = this.Advance(TokenKind.Identifier).Value;
                    this.Advance(TokenKind.Assignment);
                }

                var value = this.TopExpression();

                names.Add(name);
                values.Add(value);

                if (!this.Peek(TokenKind.CloseBrace)) {
                    this.Advance(TokenKind.Comma);
                }
            }

            var end = this.Advance(TokenKind.CloseBrace);
            loc = start.Span(end.Location);

            return new NewPutSyntax(loc, targetType, isNew, names, values);
        }
    }
}

namespace Helix.Features.Memory {
    public class NewPutSyntax : ISyntaxTree {
        private static int tempCounter = 0;

        private readonly ISyntaxTree type;
        private readonly IReadOnlyList<string?> names;
        private readonly IReadOnlyList<ISyntaxTree> values;
        private readonly bool isNew;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.values.Prepend(type);

        public bool IsPure { get; }

        public NewPutSyntax(TokenLocation loc, ISyntaxTree type, bool isNew,
            IReadOnlyList<string?> names, IReadOnlyList<ISyntaxTree> values) {

            this.Location = loc;
            this.type = type;
            this.names = names;
            this.values = values;
            this.isNew = isNew;

            this.IsPure = type.IsPure && values.All(x => x.IsPure);
        }

        public NewPutSyntax(TokenLocation loc, ISyntaxTree type, bool isNew) {
            this.Location = loc;
            this.type = type;
            this.isNew = isNew;
            this.names = Array.Empty<string>();
            this.values = Array.Empty<ISyntaxTree>();

            this.IsPure = type.IsPure;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            // If the supplied type isn't a type, then try to check this as a new value expression
            if (!this.type.AsType(types).TryGetValue(out var type)) {
                // Put expression cannot take values
                if (!this.isNew) {
                    throw TypeCheckingErrors.ExpectedTypeExpression(this.type.Location);
                }
                
                // Make sure we are not supplying members to a value new expression
                if (this.names.Any()) {
                    throw new TypeCheckingException(
                        this.Location,
                        "Invalid Members",
                        $"You may not supply explicit members to a new expression when providing an existing value.'");
                }

                var lifetime = new Lifetime(this.Location.Scope.Append("$new_temp_" + tempCounter++), 0);

                var result = new NewSyntax(
                    this.Location, 
                    this.type.CheckTypes(types), 
                    lifetime, 
                    types.LifetimeGraph.AllLifetimes);

                return result.CheckTypes(types);
            }

            // Make sure we are not supplying members to a primitive type
            if (type is not NamedType) {
                if (this.names.Count > 0) {
                    throw new TypeCheckingException(
                        this.Location,
                        "Member Not Defined",
                        $"The type '{type}' does not contain the member '{this.names[0]}'");
                }
            }

            // Rewrite a new type expression to a new value expression and a put expression
            if (this.isNew) {
                var putSyntax = new NewPutSyntax(this.Location, this.type, false, this.names, this.values);
                var newSyntax = new NewPutSyntax(this.Location, putSyntax, true);

                return newSyntax.CheckTypes(types);
            }

            // Handle normal put syntax
            if (type == PrimitiveType.Void) {
                return new VoidLiteral(this.Location).CheckTypes(types);
            }
            else if (type == PrimitiveType.Int) {
                return new IntLiteral(this.Location, 0).CheckTypes(types);
            }
            else if (type == PrimitiveType.Bool) {
                return new IntLiteral(this.Location, 0).CheckTypes(types);
            }
            else if (type is SingularIntType singInt) {
                return new IntLiteral(this.Location, singInt.Value).CheckTypes(types);
            }
            else if (type is SingularBoolType singBool) {
                return new BoolLiteral(this.Location, singBool.Value).CheckTypes(types);
            }
            else if (type is NamedType named) {
                if (!types.Aggregates.TryGetValue(named.Path, out var sig)) {
                    throw TypeCheckingErrors.ExpectedStructType(this.type.Location, type);
                }

                var result = new PutStructSyntax(
                    this.Location,
                    sig,
                    this.names,
                    this.values);

                return result.CheckTypes(types);
            }
            else {
                throw new TypeCheckingException(
                    this.Location,
                    "Invalid Initialization",
                    $"The type '{type}' does not have a default value and cannot be initialized.");
            }
        }

        public ICSyntax GenerateCode(EvalFrame types, ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}