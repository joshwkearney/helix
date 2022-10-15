using System.Collections.Generic;
using System.Collections.Immutable;
using Trophy;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing {
    public partial class Parser {
        private IParseTree WhileStatement() {
            var start = this.Advance(TokenKind.WhileKeyword);
            var cond = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            var body = this.TopExpression();
            var loc = start.Location.Span(body.Location);

            return new WhileParseStatement(loc, cond, body);
        }
    }
}

namespace Trophy.Features.FlowControl {
    public class WhileParseStatement : IParseTree {
        private readonly IParseTree cond, body;

        public TokenLocation Location { get; }

        public WhileParseStatement(TokenLocation location, IParseTree cond, IParseTree body) {
            this.Location = location;
            this.cond = cond;
            this.body = body;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            var cond = this.cond.ResolveTypes(scope, names, types);
            var body = this.body.ResolveTypes(scope, names, types);

            // Make sure the condition is a boolean
            if (cond.TryUnifyTo(PrimitiveType.Bool).TryGetValue(out var newCond)) {
                cond = newCond;
            }
            else {
                throw TypeCheckingErrors.UnexpectedType(this.cond.Location, PrimitiveType.Bool, cond.ReturnType);
            }

            return new WhileStatement(cond, body);
        }
    }

    public class WhileStatement : ISyntaxTree {
        private readonly ISyntaxTree cond, body;

        public TrophyType ReturnType => PrimitiveType.Void;

        public WhileStatement(ISyntaxTree cond, ISyntaxTree body) {
            this.cond = cond;
            this.body = body;
        }

        public CExpression GenerateCode(CWriter declWriter, CStatementWriter statWriter) {
            var loopBody = new List<CStatement>();
            var bodyWriter = new CStatementWriter(declWriter, loopBody);
            var cond = CExpression.Not(this.cond.GenerateCode(declWriter, bodyWriter));

            loopBody.Add(CStatement.If(cond, new[] { CStatement.Break() }));
            loopBody.Add(CStatement.NewLine());

            this.body.GenerateCode(declWriter, bodyWriter);

            statWriter.WriteSpacingLine();
            statWriter.WriteStatement(CStatement.Comment("While loop"));
            statWriter.WriteStatement(CStatement.While(CExpression.IntLiteral(1), loopBody));
            statWriter.WriteSpacingLine();

            return CExpression.IntLiteral(0);
        }
    }
}
