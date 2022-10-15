using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Primitives;

namespace Trophy.Analysis.Unification {
    public static partial class TypeUnifier {
        public static Option<ISyntaxTree> TryUnifyPrimitives(ISyntaxTree syntax, TrophyType type) {
            // Bool to Int
            if (syntax.ReturnType == PrimitiveType.Bool && type == PrimitiveType.Int) {
                return new IntSyntaxAdapter(syntax);
            }

            // Void to Int and Bool
            if (syntax.ReturnType == PrimitiveType.Void) {
                if (type == PrimitiveType.Int) {
                    return new SyntaxAdapter(syntax, new IntLiteral(0));
                }
                else if (type == PrimitiveType.Bool) {
                    return new SyntaxAdapter(syntax, new BoolLiteral(false));
                }
            }

            return Option.None;
        }
    }
}

namespace Trophy.Features.Primitives {
    public class IntSyntaxAdapter : ISyntaxTree {
        public ISyntaxTree inner;

        public TrophyType ReturnType => PrimitiveType.Int;

        public IntSyntaxAdapter(ISyntaxTree inner) {
            this.inner = inner;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            var inner = this.inner.GenerateCode(writer, statWriter);
            var type = writer.ConvertType(this.ReturnType);

            return CExpression.Cast(type, inner);
        }
    }
}
