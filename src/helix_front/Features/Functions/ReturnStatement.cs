using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Features.Functions;
using Helix.Features.Types;
using Helix.Features.Primitives;
using Helix.HelixMinusMinus;

namespace Helix.Parsing {
    public partial class Parser {
        public IParseTree ReturnStatement() {
            var start = this.Advance(TokenKind.ReturnKeyword);

            if (this.Peek(TokenKind.Semicolon)) {
                return new ReturnParseStatement(
                    start.Location,
                    new VoidLiteral(start.Location));
            }
            else {
                return new ReturnParseStatement(
                    start.Location,
                    this.TopExpression());
            }
        }
    }
}

namespace Helix.Features.Functions {
    public record ReturnParseStatement(TokenLocation Location, IParseTree Argument) : IParseTree {
        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) {
            var arg = this.Argument.ToImperativeSyntax(writer);

            writer.AddStatement(new ReturnStatement(this.Location, arg));
            return ImperativeExpression.Void;
        }
    }

    public record ReturnStatement(TokenLocation Location, ImperativeExpression Argument) : IImperativeStatement {
        public void CheckTypes(TypeFrame types, ImperativeSyntaxWriter writer) {
            if (types.CurrentFunction == null) {
                throw new InvalidOperationException();
            }

            var newArg = this.Argument.UnifyTo(types.CurrentFunction.ReturnType, types, writer);
            var result = new ReturnStatement(this.Location, newArg);

            writer.AddStatement(result);
        }

        public string[] Write() {
            return new[] { $"return {this.Argument};" };
        }
    }
}
