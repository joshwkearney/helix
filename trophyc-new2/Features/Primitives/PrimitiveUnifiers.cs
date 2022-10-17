using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Analysis.Unification {
    public static partial class TypeUnifier {
        private static Func<ISyntax, ISyntax>? TryUnifyToPrimitives(TrophyType from, TrophyType to) {
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
    public record IntSyntaxAdapter : ISyntax {
        private readonly ISyntax inner;

        public TokenLocation Location => this.inner.Location;

        public IntSyntaxAdapter(ISyntax inner) {
            this.inner = inner;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) {
            return Option.None;
        }

        public ISyntax CheckTypes(ITypesRecorder types) {
            return this;
        }

        public ISyntax ToRValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            return new CCast() {
                Type = writer.ConvertType(PrimitiveType.Int),
                Target = this.inner.GenerateCode(writer)
            };
        }
    }
}
