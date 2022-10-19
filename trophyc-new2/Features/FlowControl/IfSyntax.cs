using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

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

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            var cond = this.cond.CheckTypes(types).ToRValue(types).UnifyTo(PrimitiveType.Bool, types);
            var iftrue = this.iftrue.CheckTypes(types).ToRValue(types);
            var iffalse = this.iffalse.CheckTypes(types).ToRValue(types);

            iftrue = iftrue.UnifyFrom(iffalse, types);
            iffalse = iffalse.UnifyFrom(iftrue, types);

            var resultType = types.ReturnTypes[iftrue];
            var result = new IfSyntax(this.Location, cond, iftrue, iffalse, resultType);

            types.ReturnTypes[result] = resultType;
            return result;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ISyntaxTree ToLValue(SyntaxFrame types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
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

        public ISyntaxTree CheckTypes(SyntaxFrame types) => this;

        public ISyntaxTree ToRValue(SyntaxFrame types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            var affirmList = new List<ICStatement>();
            var negList = new List<ICStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var affirm = this.iftrue.GenerateCode(affirmWriter);
            var neg = this.iffalse.GenerateCode(negWriter);

            var tempName = writer.GetVariableName();

            affirmWriter.WriteStatement(new CAssignment() { 
                Left = new CVariableLiteral(tempName),
                Right = affirm
            });

            negWriter.WriteStatement(new CAssignment() {
                Left = new CVariableLiteral(tempName),
                Right = neg
            });

            var stat = new CVariableDeclaration() {
                Type = writer.ConvertType(this.returnType),
                Name = tempName
            };

            var expr = new CIf() {
                Condition = this.cond.GenerateCode(writer),
                IfTrue = affirmList,
                IfFalse = negList
            };

            writer.WriteEmptyLine();
            writer.WriteComment("If statement");
            writer.WriteStatement(stat);
            writer.WriteStatement(expr);
            writer.WriteEmptyLine();

            return new CVariableLiteral(tempName);
        }
    }
}