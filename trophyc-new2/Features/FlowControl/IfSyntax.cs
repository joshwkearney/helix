using Trophy.Analysis;
using Trophy.Analysis.Unification;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree IfExpression() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.TopExpression();

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Location);

                return new IfSyntax(loc, cond, affirm, neg);
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfSyntax(loc, cond, affirm);
            }
        }
    }
}

namespace Trophy.Features.FlowControl {
    public class IfSyntax : ISyntaxTree {
        private readonly ISyntaxTree cond, iftrue, iffalse;

        public TokenLocation Location { get; }

        public IfSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue) {
            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockSyntax(iftrue.Location, new ISyntaxTree[] {
                iftrue, new VoidLiteral(iftrue.Location)
            });

            this.iffalse = new VoidLiteral(location);
        }

        public IfSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue, ISyntaxTree iffalse)
            : this(location, cond, iftrue) {

            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) {
            return Option.None;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            if (!this.cond.ResolveTypes(scope, types).ToRValue(types).TryGetValue(out var cond)) {
                throw TypeCheckingErrors.RValueRequired(this.cond.Location);
            }

            if (!this.iftrue.ResolveTypes(scope, types).ToRValue(types).TryGetValue(out var iftrue)) {
                throw TypeCheckingErrors.RValueRequired(this.iftrue.Location);
            }

            if (!this.iffalse.ResolveTypes(scope, types).ToRValue(types).TryGetValue(out var iffalse)) {
                throw TypeCheckingErrors.RValueRequired(this.iffalse.Location);
            }

            var condType = types.GetReturnType(cond);
            var ifTrueType = types.GetReturnType(iftrue);
            var ifFalseType = types.GetReturnType(iffalse);

            // Make sure that the condition is a boolean
            if (!TypeUnifier.TryUnifyTo(cond, condType, PrimitiveType.Bool).TryGetValue(out cond)) {
                throw TypeCheckingErrors.UnexpectedType(this.cond.Location, PrimitiveType.Bool, condType);
            }

            // Make sure that the branches are the same type
            if (!TypeUnifier.TryUnifyFrom(ifTrueType, ifFalseType).TryGetValue(out var unified)) {
                throw TypeCheckingErrors.UnexpectedType(this.Location, ifTrueType, ifFalseType);
            }

            iftrue = TypeUnifier.TryUnifyTo(iftrue, ifTrueType, unified).GetValue();
            iffalse = TypeUnifier.TryUnifyTo(iffalse, ifFalseType, unified).GetValue();

            var result = new IfSyntax(this.Location, cond, iftrue, iffalse);
            types.SetReturnType(result, unified);

            return result;
        }

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) => Option.None;

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter writer) {
            var affirmList = new List<CStatement>();
            var negList = new List<CStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var cond = this.cond.GenerateCode(types, writer);
            var affirm = this.iftrue.GenerateCode(types, affirmWriter);
            var neg = this.iffalse.GenerateCode(types, negWriter);

            var tempName = writer.GetVariableName();
            var returnType = writer.ConvertType(types.GetReturnType(this));

            affirmList.Add(CStatement.Assignment(CExpression.VariableLiteral(tempName), affirm));
            negList.Add(CStatement.Assignment(CExpression.VariableLiteral(tempName), neg));

            writer.WriteSpacingLine();
            writer.WriteStatement(CStatement.Comment("If statement"));
            writer.WriteStatement(CStatement.VariableDeclaration(returnType, tempName));
            writer.WriteStatement(CStatement.If(cond, affirmList, negList));
            writer.WriteSpacingLine();

            return CExpression.VariableLiteral(tempName);
        }
    }
}