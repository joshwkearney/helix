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
        private ISyntaxTree ForStatement(BlockBuilder block) {
            var startTok = this.Advance(TokenKind.ForKeyword);
            var id = this.Advance(TokenKind.Identifier);

            this.Advance(TokenKind.Assignment);
            var startIndex = this.TopExpression(block);

            this.Advance(TokenKind.ToKeyword);
            var endIndex = this.TopExpression(block);

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

            block.Statements.Add(counterDecl);

            var newBlock = new BlockBuilder();
            var loc = startTok.Location.Span(endIndex.Location);

            //var iteratorDecl = new VarParseStatement(startTok.Location, new[] { id.Value }, counterAccess, true);

            var test = new IfParseSyntax(
                loc,
                this.scope.Append(block.GetTempName()),
                new BinarySyntax(
                    loc,
                    counterAccess,
                    endIndex,
                    BinaryOperationKind.GreaterThanOrEqualTo),
                new BreakContinueSyntax(loc, true));

            //newBlock.Statements.Add(iteratorDecl);
            newBlock.Statements.Add(test);

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            var body = this.TopExpression(newBlock);
            loc = loc.Span(body.Location);
            newBlock.Statements.Add(counterInc);

            var loop = new LoopStatement(loc, new BlockSyntax(loc, newBlock.Statements));
            block.Statements.Add(loop);

            return new VoidLiteral(loc);
        }
    }        
}