using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Syntax;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseSyntax ForStatement() {
            var startTok = this.Advance(TokenKind.ForKeyword);
            var id = this.Advance(TokenKind.Identifier);

            this.Advance(TokenKind.Assignment);
            var startIndex = this.TopExpression();

            var inclusive = true;
            if (!this.TryAdvance(TokenKind.ToKeyword)) {
                this.Advance(TokenKind.UntilKeyword);
                inclusive = false;
            }

            var endIndex = this.TopExpression();

            startIndex = new AsParseSyntax(
                startIndex.Location,
                startIndex,
                new VariableAccessParseParse(startIndex.Location, "word"));

            endIndex = new AsParseSyntax(
                endIndex.Location,
                endIndex,
                new VariableAccessParseParse(endIndex.Location, "word"));

            var counterName = id.Value;

            var counterDecl = new VarParseStatement(
                startTok.Location, 
                new[] { counterName }, 
                new Option<IParseSyntax>[] { Option.None }, 
                startIndex);

            var counterAccess = new VariableAccessParseParse(startTok.Location, counterName);

            var counterInc = new AssignmentStatement(
                startTok.Location,
                counterAccess,
                new BinaryParseSyntax(
                    startTok.Location,
                    counterAccess,
                    new WordLiteral(startTok.Location, 1),
                    BinaryOperationKind.Add));

            var totalBlock = new List<IParseSyntax> { counterDecl };
            var loopBlock = new List<IParseSyntax>();
            var loc = startTok.Location.Span(endIndex.Location);

            var test = new IfParse(
                loc,
                new BinaryParseSyntax(
                    loc,
                    counterAccess,
                    endIndex,
                    inclusive
                        ? BinaryOperationKind.GreaterThan
                        : BinaryOperationKind.GreaterThanOrEqualTo),
                new BreakContinueParse(loc, true));

            loopBlock.Add(test);

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            var body = this.TopExpression();
            loc = loc.Span(body.Location);

            loopBlock.Add(body);
            loopBlock.Add(counterInc);

            var loop = new LoopStatement(loc, BlockParse.FromMany(loc, loopBlock));
            totalBlock.Add(loop);

            return BlockParse.FromMany(loc, totalBlock);
        }
    }
}