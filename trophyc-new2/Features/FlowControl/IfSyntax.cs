using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Analysis.Unification;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
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

                return new IfParseSyntax(loc, cond, affirm, neg);
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfParseSyntax(loc, cond, affirm);
            }
        }
    }
}

namespace Trophy.Features.FlowControl {
    public record IfParseSyntax : ISyntaxTree {
        private readonly ISyntaxTree cond, iftrue, iffalse;

        public TokenLocation Location { get; }

        public IfParseSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue) {
            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockSyntax(iftrue.Location, new ISyntaxTree[] {
                iftrue, new VoidLiteral(iftrue.Location)
            });

            this.iffalse = new VoidLiteral(location);
        }

        public IfParseSyntax(TokenLocation location, ISyntaxTree cond, ISyntaxTree iftrue, ISyntaxTree iffalse)
            : this(location, cond, iftrue) {

            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
        }

        public Option<TrophyType> ToType(INamesRecorder names) {
            return Option.None;
        }

        public ISyntaxTree CheckTypes(ITypesRecorder types) {
            if (!this.cond.CheckTypes(types).ToRValue(types).TryGetValue(out var cond)) {
                throw TypeCheckingErrors.RValueRequired(this.cond.Location);
            }

            if (!this.iftrue.CheckTypes(types).ToRValue(types).TryGetValue(out var iftrue)) {
                throw TypeCheckingErrors.RValueRequired(this.iftrue.Location);
            }

            if (!this.iffalse.CheckTypes(types).ToRValue(types).TryGetValue(out var iffalse)) {
                throw TypeCheckingErrors.RValueRequired(this.iffalse.Location);
            }

            var condType = types.GetReturnType(cond);
            var ifTrueType = types.GetReturnType(iftrue);
            var ifFalseType = types.GetReturnType(iffalse);

            // Make sure that the condition is a boolean
            if (!types.TryUnifyTo(cond, condType, PrimitiveType.Bool).TryGetValue(out cond)) {
                throw TypeCheckingErrors.UnexpectedType(this.cond.Location, PrimitiveType.Bool, condType);
            }

            // Make sure that the branches are the same type
            if (!types.TryUnifyFrom(ifTrueType, ifFalseType).TryGetValue(out var unified)) {
                throw TypeCheckingErrors.UnexpectedType(this.Location, ifTrueType, ifFalseType);
            }

            iftrue = types.TryUnifyTo(iftrue, ifTrueType, unified).GetValue();
            iffalse = types.TryUnifyTo(iffalse, ifFalseType, unified).GetValue();

            var result = new IfSyntax(this.Location, cond, iftrue, iffalse, unified);
            types.SetReturnType(result, unified);

            return result;
        }

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => Option.None;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public CExpression GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record IfSyntax : ISyntaxTree {
        private readonly ISyntaxTree cond, iftrue, iffalse;
        private readonly TrophyType returnType;

        public TokenLocation Location { get; }

        public IfSyntax(TokenLocation loc, ISyntaxTree cond,
                         ISyntaxTree iftrue,
                         ISyntaxTree iffalse, TrophyType returnType) {

            this.Location = loc;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.returnType = returnType;
        }

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntaxTree CheckTypes(ITypesRecorder types) => this;

        public Option<ISyntaxTree> ToLValue(ITypesRecorder types) => Option.None;

        public Option<ISyntaxTree> ToRValue(ITypesRecorder types) => this;

        public CExpression GenerateCode(ICStatementWriter writer) {
            var affirmList = new List<CStatement>();
            var negList = new List<CStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var cond = this.cond.GenerateCode(writer);
            var affirm = this.iftrue.GenerateCode(writer);
            var neg = this.iffalse.GenerateCode(writer);

            var tempName = writer.GetVariableName();
            var returnType = writer.ConvertType(this.returnType);

            affirmList.Add(CStatement.Assignment(CExpression.VariableLiteral(tempName), affirm));
            negList.Add(CStatement.Assignment(CExpression.VariableLiteral(tempName), neg));

            writer.WriteEmptyLine();
            writer.WriteStatement(CStatement.Comment("If statement"));
            writer.WriteStatement(CStatement.VariableDeclaration(returnType, tempName));
            writer.WriteStatement(CStatement.If(cond, affirmList, negList));
            writer.WriteEmptyLine();

            return CExpression.VariableLiteral(tempName);
        }
    }
}