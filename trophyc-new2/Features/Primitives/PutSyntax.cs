using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Generation;
using Trophy.Features.Aggregates;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax PutExpression() {
            TokenLocation start;
            bool isStackAllocated;

            //if (this.Peek(TokenKind.NewKeyword)) {
            //    start = this.Advance(TokenKind.NewKeyword).Location;
            //    isStackAllocated = false;
            //}
            //else {
                start = this.Advance(TokenKind.PutKeyword).Location;
                isStackAllocated = true;
            //}

            var targetType = this.TopExpression();
            var loc = start.Span(targetType.Location);

            if (!this.TryAdvance(TokenKind.OpenBrace)) {
                return new PutSyntax(
                    loc, 
                    targetType, 
                    Array.Empty<string>(), 
                    Array.Empty<ISyntax>());
            }

            var names = new List<string?>();
            var values = new List<ISyntax>();

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

            return new PutSyntax(loc, targetType, names, values);
        }
    }
}

namespace Trophy.Features.Primitives {
    public class PutSyntax : ISyntax {
        private readonly ISyntax type;
        private readonly IReadOnlyList<string?> names;
        private readonly IReadOnlyList<ISyntax> values;

        public TokenLocation Location { get; }

        public PutSyntax(TokenLocation loc, ISyntax type, 
            IReadOnlyList<string?> names, IReadOnlyList<ISyntax> values) {

            this.Location = loc;
            this.type = type;
            this.names = names;
            this.values = values;
        }

        public PutSyntax(TokenLocation loc, ISyntax type) {

            this.Location = loc;
            this.type = type;
            this.names = Array.Empty<string>();
            this.values = Array.Empty<ISyntax>();
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) {
            if (!this.type.TryInterpret(types).TryGetValue(out var type)) {
                throw TypeCheckingErrors.ExpectedTypeExpression(this.type.Location);
            }

            if (type is PrimitiveType) {
                if (this.names.Count > 0) {
                    throw new TypeCheckingException(
                        this.Location,
                        "Member Not Defined",
                        $"The type '{type}' does not contain the member '{this.names[0]}'");
                }
            }

            if (type == PrimitiveType.Void) {
                return new VoidLiteral(this.Location).CheckTypes(types);
            }
            else if (type == PrimitiveType.Int) {
                return new IntLiteral(this.Location, 0).CheckTypes(types);
            }
            else if (type == PrimitiveType.Bool) {
                return new IntLiteral(this.Location, 0).CheckTypes(types);
            }
            else if (type is NamedType named) {
                var target = types.TryResolveName(named.Path).GetValue();
                if (target != NameTarget.Aggregate) {
                    throw TypeCheckingErrors.ExpectedStructType(this.type.Location, type);
                }

                var sig = types.GetAggregate(named.Path);

                var result = new NewAggregateSyntax(
                    this.Location,
                    sig, 
                    this.names, 
                    this.values);

                return result.CheckTypes(types);
            }

            throw new TypeCheckingException(
                this.Location,
                "Invalid Initialization",
                $"The type '{type}' does not have a default value and cannot be initialized.");
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}