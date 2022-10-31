using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Generation.Syntax;
using System.Linq.Expressions;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree ForStatement() {
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

            startIndex = new AsParseTree(
                startIndex.Location,
                startIndex,
                new VariableAccessParseSyntax(startIndex.Location, "int"));

            endIndex = new AsParseTree(
                endIndex.Location,
                endIndex,
                new VariableAccessParseSyntax(endIndex.Location, "int"));

            var counterName = id.Value;
            var counterDecl = new VarParseStatement(startTok.Location, new[] { counterName }, startIndex, true);
            var counterAccess = new VariableAccessParseSyntax(startTok.Location, counterName);

            var counterInc = new AssignmentStatement(
                startTok.Location,
                counterAccess,
                new BinarySyntax(
                    startTok.Location,
                    counterAccess,
                    new IntLiteral(startTok.Location, 1),
                    BinaryOperationKind.Add));

            var totalBlock = new List<ISyntaxTree> { counterDecl };
            var loopBlock = new List<ISyntaxTree>();
            var loc = startTok.Location.Span(endIndex.Location);

            var test = new IfParseSyntax(
                loc,
                new BinarySyntax(
                    loc,
                    counterAccess,
                    endIndex,
                    inclusive
                        ? BinaryOperationKind.GreaterThan
                        : BinaryOperationKind.GreaterThanOrEqualTo),
                new BreakContinueSyntax(loc, true));

            loopBlock.Add(test);

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            var body = this.TopExpression();
            loc = loc.Span(body.Location);

            loopBlock.Add(body);
            loopBlock.Add(counterInc);

            var loop = new LoopStatement(loc, new BlockSyntax(loc, loopBlock));
            totalBlock.Add(loop);

            return new BlockSyntax(loc, totalBlock);
        }
    }        
}