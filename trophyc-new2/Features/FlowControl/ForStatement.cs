using Trophy;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.FlowControl;
using Trophy.Features.Primitives;
using Trophy.Features.Variables;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing {
    public partial class Parser {
        private IParseTree ForStatement() {
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
    public class ForParseStatement : IParseTree {
        private readonly string iteratorName;
        private readonly IParseTree startIndex, endIndex, body;

        public TokenLocation Location { get; }

        public ForParseStatement(TokenLocation loc, string id, IParseTree start, IParseTree end, IParseTree body) {
            this.Location = loc;
            this.iteratorName = id;
            this.startIndex = start;
            this.endIndex = end;
            this.body = body;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            // Rewrite for syntax to use while loops
            var start = new AsParseTree(
                this.startIndex.Location, 
                this.startIndex, 
                new PrimitiveTypeTree(this.startIndex.Location, PrimitiveType.Int));

            var end = new AsParseTree(
                this.endIndex.Location,
                this.endIndex,
                new PrimitiveTypeTree(this.startIndex.Location, PrimitiveType.Int));

            var counterName = names.GetTempVariableName();
            var counterDecl = new VarParseStatement(this.Location, counterName, start, false);

            var counterAccess = new VariableAccessParseTree(this.Location, counterName);
            var iteratorDecl = new VarParseStatement(this.Location, this.iteratorName, counterAccess, false);

            var comp = new BinaryParseTree(this.Location, counterAccess, end, BinaryOperation.LessThan);

            var assign = new AssignmentParseTree(
                this.Location,
                counterAccess,
                new BinaryParseTree(
                    this.Location,
                    counterAccess,
                    new IntParseLiteral(this.Location, 1),
                    BinaryOperation.Add));

            var block = new BlockParseTree(this.Location, new IParseTree[] {
                counterDecl,
                new WhileParseStatement(
                    this.Location,
                    comp,
                    new BlockParseTree(this.Location, new IParseTree[] { 
                        iteratorDecl,
                        this.body,
                        assign
                    }))
            });

            return block.ResolveTypes(scope, names, types, context);
        }
    }
}
