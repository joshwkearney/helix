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
        private ISyntaxTree NewExpression() {
            var start = this.Advance(TokenKind.NewKeyword).Location;
            var targetType = this.TopExpression();
            var loc = start.Span(targetType.Location);

            if (!this.TryAdvance(TokenKind.OpenBrace)) {
                return new NewSyntax(loc, targetType);
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

            return new NewSyntax(loc, targetType, names, values);
        }
    }
}

namespace Helix.Features.Memory {
    public class NewSyntax : ISyntaxTree {
        private static int tempCounter = 0;

        private readonly ISyntaxTree type;
        private readonly IReadOnlyList<string?> names;
        private readonly IReadOnlyList<ISyntaxTree> values;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.values.Prepend(type);

        public bool IsPure { get; }

        public NewSyntax(TokenLocation loc, ISyntaxTree type,
            IReadOnlyList<string?> names, IReadOnlyList<ISyntaxTree> values) {

            this.Location = loc;
            this.type = type;
            this.names = names;
            this.values = values;

            this.IsPure = type.IsPure && values.All(x => x.IsPure);
        }

        public NewSyntax(TokenLocation loc, ISyntaxTree type) {
            this.Location = loc;
            this.type = type;
            this.names = Array.Empty<string>();
            this.values = Array.Empty<ISyntaxTree>();

            this.IsPure = type.IsPure;
        }

        public ISyntaxTree CheckTypes(EvalFrame types) {
            // If the supplied type isn't a type, then try to check this as a new value expression
            if (!this.type.AsType(types).TryGetValue(out var type)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.type.Location);              
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
                if (!types.Structs.TryGetValue(named.Path, out var sig)) {
                    throw TypeCheckingErrors.ExpectedStructType(this.type.Location, type);
                }

                var result = new NewStructSyntax(
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
    }
}