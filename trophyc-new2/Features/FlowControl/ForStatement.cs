using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation;
using Trophy.Generation.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Features.Variables;
using Trophy.Parsing;
using Trophy.Generation.Syntax;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntax ForStatement() {
            var start = this.Advance(TokenKind.ForKeyword);
            var id = this.Advance(TokenKind.Identifier).Value;

            this.Advance(TokenKind.Assignment);
            var startIndex = this.TopExpression();

            this.Advance(TokenKind.ToKeyword);
            var endIndex = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            var body = this.TopExpression();
            var loc = start.Location.Span(body.Location);

            return new ForStatement(loc, id, startIndex, endIndex, body);
        }

    }
}

namespace Trophy.Features.FlowControl {
    public record ForStatement : ISyntax {
        private readonly string iteratorName;
        private readonly ISyntax startIndex, endIndex, body;

        public TokenLocation Location { get; }

        public ForStatement(TokenLocation loc, string id, ISyntax start, ISyntax end, ISyntax body) {
            this.Location = loc;
            this.iteratorName = id;
            this.startIndex = start;
            this.endIndex = end;
            this.body = body;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) {
            // Rewrite for syntax to use while loops
            var start = new AsParseTree(
                this.startIndex.Location, 
                this.startIndex, 
                new VariableAccessParseSyntax(this.startIndex.Location, "int"));

            var end = new AsParseTree(
                this.endIndex.Location,
                this.endIndex,
                new VariableAccessParseSyntax(this.startIndex.Location, "int"));

            var counterName = types.GetVariableName();
            var counterDecl = new VarParseStatement(this.Location, counterName, start, true);

            var counterAccess = new VariableAccessParseSyntax(this.Location, counterName);
            var iteratorDecl = new VarParseStatement(this.Location, this.iteratorName, counterAccess, false);

            var comp = new BinarySyntax(this.Location, counterAccess, end, BinaryOperationKind.LessThan);

            var assign = new AssignmentStatement(
                this.Location,
                counterAccess,
                new BinarySyntax(
                    this.Location,
                    counterAccess,
                    new IntLiteral(this.Location, 1),
                    BinaryOperationKind.Add));

            var block = new BlockSyntax(this.Location, new ISyntax[] {
                counterDecl,
                new WhileStatement(
                    this.Location,
                    comp,
                    new BlockSyntax(this.Location, new ISyntax[] { 
                        iteratorDecl,
                        this.body,
                        assign
                    }))
            });

            return block.CheckTypes(types);
        }

        public ISyntax ToRValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public ISyntax ToLValue(ITypesRecorder types) {
            throw new InvalidOperationException();
        }

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            throw new InvalidOperationException();
        }
    }
}
