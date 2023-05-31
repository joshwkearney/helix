using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Features.Aggregates;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Analysis;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree NewExpression() {
            var start = this.Advance(TokenKind.NewKeyword).Location;
            var targetType = this.TopExpression();
            var loc = start.Span(targetType.Location);

            if (!this.TryAdvance(TokenKind.OpenBrace)) {
                return new NewSyntax(loc, targetType);
            }

            var names = new List<string>();
            var values = new List<ISyntaxTree>();

            while (!this.Peek(TokenKind.CloseBrace)) {
                string name = null;

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

namespace Helix.Features.Primitives {
    public class NewSyntax : ISyntaxTree {
        private readonly ISyntaxTree type;
        private readonly IReadOnlyList<string> names;
        private readonly IReadOnlyList<ISyntaxTree> values;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.values.Prepend(this.type);

        public bool IsPure { get; }

        public NewSyntax(TokenLocation loc, ISyntaxTree type,
            IReadOnlyList<string> names, IReadOnlyList<ISyntaxTree> values) {

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

        public ISyntaxTree CheckTypes(TypeFrame types) {
            // Make sure our type is actually a type
            if (!this.type.AsType(types).TryGetValue(out var type)) {
                throw TypeException.ExpectedTypeExpression(this.type.Location);              
            }

            // Make sure we are not supplying members to a primitive type
            if (!type.AsStruct(types).HasValue && !type.AsUnion(types).HasValue) {
                if (this.names.Count > 0) {
                    throw new TypeException(
                        this.Location,
                        "Member Not Defined",
                        $"The type '{type}' does not contain the member '{this.names[0]}'");
                }
            }

            // Handle normal put syntax
            if (type == PrimitiveType.Void) {
                return new VoidLiteral(this.Location).CheckTypes(types);
            }
            else if (type == PrimitiveType.Word) {
                return new WordLiteral(this.Location, 0).CheckTypes(types);
            }
            else if (type == PrimitiveType.Bool) {
                return new WordLiteral(this.Location, 0).CheckTypes(types);
            }
            else if (type is SingularWordType singInt) {
                return new WordLiteral(this.Location, singInt.Value).CheckTypes(types);
            }
            else if (type is SingularBoolType singBool) {
                return new BoolLiteral(this.Location, singBool.Value).CheckTypes(types);
            }
            else if (type.AsStruct(types).TryGetValue(out var structSig)) {
                var result = new NewStructSyntax(
                    this.Location,
                    type,
                    structSig,
                    this.names,
                    this.values,
                    types.Scope);

                return result.CheckTypes(types);
            }
            else if (type.AsUnion(types).TryGetValue(out var unionSig)) {
                var result = new NewUnionSyntax(
                    this.Location,
                    type,
                    unionSig,
                    this.names,
                    this.values);

                return result.CheckTypes(types);
            }

            throw new TypeException(
                this.Location,
                "Invalid Initialization",
                $"The type '{type}' does not have a default value and cannot be initialized.");
        }
    }
}