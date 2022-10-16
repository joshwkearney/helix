using Trophy.Analysis;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Features.Variables;
using Trophy.Parsing;

namespace Trophy.Parsing {
    public partial class Parser {
        private ISyntaxTree ForStatement() {
            var start = this.Advance(TokenKind.ForKeyword);
            var id = this.Advance(TokenKind.Identifier).Value;

            this.Advance(TokenKind.Assignment);
            var startIndex = this.TopExpression();

            this.Advance(TokenKind.ToKeyword);
            var endIndex = this.TopExpression();

            this.Advance(TokenKind.DoKeyword);
            var body = this.TopExpression();
            var loc = start.Location.Span(body.Location);

            return new ForParseStatement(loc, id, startIndex, endIndex, body);
        }

    }
}

namespace Trophy.Features.FlowControl {
    public class ForParseStatement : ISyntaxTree {
        private readonly string iteratorName;
        private readonly ISyntaxTree startIndex, endIndex, body;

        public TokenLocation Location { get; }

        public ForParseStatement(TokenLocation loc, string id, ISyntaxTree start, ISyntaxTree end, ISyntaxTree body) {
            this.Location = loc;
            this.iteratorName = id;
            this.startIndex = start;
            this.endIndex = end;
            this.body = body;
        }

        public Option<TrophyType> ToType(IdentifierPath scope, TypesRecorder types) => Option.None;

        public ISyntaxTree ResolveTypes(IdentifierPath scope, TypesRecorder types) {
            // Rewrite for syntax to use while loops
            var start = new AsParseTree(
                this.startIndex.Location, 
                this.startIndex, 
                new IdenfifierAccessParseTree(this.startIndex.Location, "int"));

            var end = new AsParseTree(
                this.endIndex.Location,
                this.endIndex,
                new IdenfifierAccessParseTree(this.startIndex.Location, "int"));

            var counterName = types.GetTempVariableName();
            var counterDecl = new VarParseStatement(this.Location, counterName, start, true);

            var counterAccess = new IdenfifierAccessParseTree(this.Location, counterName);
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

            var block = new BlockSyntax(this.Location, new ISyntaxTree[] {
                counterDecl,
                new WhileStatement(
                    this.Location,
                    comp,
                    new BlockSyntax(this.Location, new ISyntaxTree[] { 
                        iteratorDecl,
                        this.body,
                        assign
                    }))
            });

            return block.ResolveTypes(scope, types);
        }

        public Option<ISyntaxTree> ToRValue(TypesRecorder types) {
            throw new InvalidOperationException();
        }

        public Option<ISyntaxTree> ToLValue(TypesRecorder types) {
            throw new InvalidOperationException();
        }

        public CExpression GenerateCode(TypesRecorder types, CStatementWriter statWriter) {
            throw new InvalidOperationException();
        }
    }
}
