using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Analysis.Unification {
    public static partial class TypeUnifier {
        private static Func<ISyntaxTree, ISyntaxTree> TryUnifyToPrimitives(TrophyType from, TrophyType to) {
            // Bool to Int
            if (from == PrimitiveType.Bool && to == PrimitiveType.Int) {
                return syntax => new IntSyntaxAdapter(syntax);
            }

            // Void to Int and Bool
            if (from == PrimitiveType.Void) {
                if (to == PrimitiveType.Int) {
                    return syntax => new SyntaxAdapter(syntax, new IntLiteral(syntax.Location, 0));
                }
                else if (to == PrimitiveType.Bool) {
                    return syntax => new SyntaxAdapter(syntax, new BoolLiteral(syntax.Location, false));
                }
            }

            return null;
        }
    }
}

namespace Trophy.Features.Primitives {
    public class IntSyntaxAdapter : ISyntaxTree {
        private readonly ISyntaxTree inner;

        public TokenLocation Location => this.inner.Location;

        public IntSyntaxAdapter(ISyntaxTree inner) {
            this.inner = inner;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) {
            return Option.None;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            return this;
        }

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) => Option.None;

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter writer) {
            var inner = this.inner.GenerateCode(types, writer);
            var type = writer.ConvertType(PrimitiveType.Int);

            return CExpression.Cast(type, inner);
        }
    }
}
