using Attempt16.Syntax;

namespace Attempt16.Generation {
    public class IntLiteralGCBehavior : IExpressionCodeGenerator {
        public CCode Generate(IntLiteral syntax) {
            return new CCode(syntax.Value.ToString());
        }

        public CCode Generate(ISyntax syntax) => this.Generate((IntLiteral)syntax);
    }
}
