using Helix.Analysis.Types;
using Helix.Analysis;
using Helix.Features.FlowControl;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using System.Reflection;
using Helix.Features.Variables;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Collections;
using Helix.Analysis.Predicates;
using Helix.HelixMinusMinus;

namespace Helix.Parsing
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

                return new IfSyntax(loc, cond, affirm, neg);
            }
            else {
                var loc = start.Location.Span(affirm.Location);

                return new IfSyntax(loc, cond, affirm);
            }
        }
    }
}

namespace Helix.Features.FlowControl
{
    public record IfSyntax : IParseTree {
        private static int ifTempCounter = 0;

        private readonly IParseTree cond, iftrue, iffalse;
        private readonly IdentifierPath path;

        public TokenLocation Location { get; }

        public IEnumerable<IParseTree> Children => new[] { this.cond, this.iftrue, this.iffalse };

        public bool IsPure { get; }

        public bool IsStatement => true;

        public IfSyntax(
            TokenLocation location,
            IParseTree cond,
            IParseTree iftrue) {

            this.Location = location;
            this.cond = cond;

            this.iftrue = new BlockSyntax(
                iftrue, 
                new VoidLiteral(iftrue.Location)
            );

            this.iffalse = new VoidLiteral(location);
            this.IsPure = cond.IsPure && iftrue.IsPure;
            this.path = new IdentifierPath("$if" + ifTempCounter++);
        }

        public IfSyntax(TokenLocation location, IParseTree cond, IParseTree iftrue,
                        IParseTree iffalse) 
            : this(location, cond, iftrue, iffalse, new IdentifierPath("$if" + ifTempCounter++)) {}

        public IfSyntax(TokenLocation location, IParseTree cond, IParseTree iftrue,
                        IParseTree iffalse, IdentifierPath path) 
            : this(location, cond, iftrue) {

            this.Location = location;
            this.cond = cond;

            this.iftrue = iftrue;
            this.iffalse = iffalse;
            
            this.path = path;
            this.IsPure = cond.IsPure && iftrue.IsPure && iffalse.IsPure;
        }

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) {
            var ifTrueWriter = new ImperativeSyntaxWriter(writer);
            var ifFalseWriter = new ImperativeSyntaxWriter(writer);

            var variable = writer.GetTempVariable();
            var cond = this.cond.ToImperativeSyntax(writer);
            var ifTrue = this.iftrue.ToImperativeSyntax(ifTrueWriter);
            var ifFalse = this.iffalse.ToImperativeSyntax(ifFalseWriter);

            writer.AddStatement(new HmmIfStatement() {
                Location = this.Location,
                Condition = cond,
                TrueStatements = ifTrueWriter.Statements,
                FalseStatements = ifFalseWriter.Statements,
                ResultVariable = variable,
                TrueValue = ifTrue,
                FalseValue = ifFalse
            });

            return variable;
        }
    }
}