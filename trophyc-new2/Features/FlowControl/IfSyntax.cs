using System.Collections.Generic;
using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing
{
    public partial class Parser {
        private IParseTree IfExpression() {
            var start = this.Advance(TokenKind.IfKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.ThenKeyword);
            var affirm = this.TopExpression();

            if (this.TryAdvance(TokenKind.ElseKeyword)) {
                var neg = this.TopExpression();
                var loc = start.Location.Span(neg.Location);

                return new IfParseTree(loc, cond, affirm, neg);
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfParseTree(loc, cond, affirm);
            }
        }
    }
}

namespace Trophy.Features.FlowControl
{
    public class IfParseTree : IParseTree {
        private readonly IParseTree cond, iftrue;
        private readonly Option<IParseTree> iffalse;

        public TokenLocation Location { get; }

        public IfParseTree(TokenLocation location, IParseTree cond, IParseTree iftrue) {
            this.Location = location;
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = Option.None;
        }

        public IfParseTree(TokenLocation location, IParseTree cond, IParseTree iftrue, IParseTree iffalse)
            : this(location, cond, iftrue) {

            this.iffalse = Option.Some(iffalse);
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            var cond = this.cond.ResolveTypes(scope, names, types);
            var iftrue = this.iftrue.ResolveTypes(scope, names, types);
            var iffalse = this.iffalse.OrElse(() => new VoidLiteral(this.Location)).ResolveTypes(scope, names, types);

            // Make sure that the condition is a boolean
            if (cond.TryUnifyTo(PrimitiveType.Bool).TryGetValue(out var newCond)) {
                cond = newCond;
            }
            else { 
                throw TypeCheckingErrors.UnexpectedType(this.cond.Location, PrimitiveType.Bool, cond.ReturnType);
            }

            if (this.iffalse.HasValue) {
                // Make sure that the branches are the same type
                if (iffalse.TryUnifyTo(iftrue.ReturnType).TryGetValue(out var newNeg)) {
                    iffalse = newNeg;
                }
                else if (iftrue.TryUnifyTo(iffalse.ReturnType).TryGetValue(out var newAffirm)) {
                    iftrue = newAffirm;
                }
                else {
                    throw TypeCheckingErrors.UnexpectedType(this.Location, iftrue.ReturnType, iffalse.ReturnType);
                }
            }

            return new IfSyntax(cond, iftrue, iffalse);
        }
    }

    public class IfSyntax : ISyntaxTree {
        private readonly ISyntaxTree cond, iftrue, iffalse;

        public TrophyType ReturnType => this.iffalse.ReturnType;

        public IfSyntax(ISyntaxTree cond, ISyntaxTree iftrue, ISyntaxTree iffalse) {
            this.cond = cond;
            this.iftrue = iftrue;
            this.iffalse = iffalse;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            var affirmList = new List<CStatement>();
            var negList = new List<CStatement>();

            var affirmWriter = new CStatementWriter(writer, affirmList);
            var negWriter = new CStatementWriter(writer, negList);

            var cond = this.cond.GenerateCode(writer, statWriter);
            var affirm = this.iftrue.GenerateCode(writer, affirmWriter);
            var neg = this.iffalse.GenerateCode(writer, negWriter);

            var tempName = writer.GetTempVariableName();
            var returnType = writer.ConvertType(this.ReturnType);

            affirmList.Add(CStatement.Assignment(CExpression.VariableLiteral(tempName), affirm));
            negList.Add(CStatement.Assignment(CExpression.VariableLiteral(tempName), neg));

            statWriter.WriteSpacingLine();
            statWriter.WriteStatement(CStatement.Comment("If statement"));
            statWriter.WriteStatement(CStatement.VariableDeclaration(returnType, tempName));
            statWriter.WriteStatement(CStatement.If(cond, affirmList, negList));
            statWriter.WriteSpacingLine();

            return CExpression.VariableLiteral(tempName);
        }
    }
}