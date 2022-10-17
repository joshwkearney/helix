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
            
            // Pointer to readonly pointer
            //if (from is PointerType p1 && to is PointerType p2) {
            //    if (p1.ReferencedType == p2.ReferencedType && p1.IsWritable) {
            //        return syntax => 
            //    }
            //}

            return null;
        }
    }
}

namespace Trophy.Features.Primitives {
    public record IntSyntaxAdapter : ISyntaxTree {
        private readonly ISyntaxTree inner;

        public TokenLocation Location => this.inner.Location;

        public IntSyntaxAdapter(ISyntaxTree inner) {
            this.inner = inner;
        }

        public Option<TrophyType> ToType(INamesRecorder names) {
            return Option.None;
        }

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            return this;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(CStatementWriter writer) {
            var inner = this.inner.GenerateCode(writer);
            var type = writer.ConvertType(PrimitiveType.Int);

            return CExpression.Cast(type, inner);
        }
    }
}
