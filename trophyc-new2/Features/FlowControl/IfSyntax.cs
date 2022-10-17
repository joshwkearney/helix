using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Analysis.Unification;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax IfExpression() {
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
    public record IfParseSyntax : ISyntax {
        private readonly ISyntax cond, iftrue, iffalse;

        public TokenLocation Location { get; }

        public IfParseSyntax(TokenLocation location, ISyntax cond, ISyntax iftrue) {
            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockSyntax(iftrue.Location, new ISyntax[] {
                iftrue, new VoidLiteral(iftrue.Location)
            });

            this.iffalse = new VoidLiteral(location);
        }

        public IfParseSyntax(TokenLocation location, ISyntax cond, ISyntax iftrue, ISyntax iffalse)
            : this(location, cond, iftrue) {

            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
        }

        public Option<TrophyType> ToType(INamesRecorder names) {
            return Option.None;
        }

        public ISyntax CheckTypes(ITypesRecorder types) {
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

        public Option<ISyntax> ToRValue(ITypesRecorder types) => Option.None;

        public Option<ISyntax> ToLValue(ITypesRecorder types) => Option.None;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }

    public record IfSyntax : ISyntax {
        private readonly ISyntax cond, iftrue, iffalse;
        private readonly TrophyType returnType;

        public TokenLocation Location { get; }

        public IfSyntax(TokenLocation loc, ISyntax cond,
                         ISyntax iftrue,
                         ISyntax iffalse, TrophyType returnType) {

            this.Location = loc;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
            this.returnType = returnType;
        }

        public Option<TrophyType> ToType(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) => this;

        public Option<ISyntax> ToLValue(ITypesRecorder types) => Option.None;

        public Option<ISyntax> ToRValue(ITypesRecorder types) => this;

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