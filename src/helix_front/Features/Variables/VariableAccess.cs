using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Syntax;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Features.Variables;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Features.Functions;
using System.IO;
using Helix.HelixMinusMinus;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseTree VariableAccess() {
            var tok = this.Advance(TokenKind.Identifier);

            return new VariableAccessParseSyntax(tok.Location, tok.Value);
        }
    }
}

namespace Helix.Features.Variables {
    public record VariableAccessParseSyntax(TokenLocation Location, string VariableName) : IParseTree {
        public Option<HelixType> AsType(TypeFrame types) {
            // If we're pointing at a type then return it
            if (types.Locals.TryGetValue(this.VariableName, out var info)) {
                return info.Type;
            }

            return Option.None;
        }

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) {
            var stat = new VariableAccessStatement(this.Location, this.VariableName);

            writer.AddStatement(stat);
            return ImperativeExpression.Variable(this.VariableName);
        }
    }

    public record VariableAccessStatement(TokenLocation Location, string VariableName) : IImperativeStatement {
        public void CheckTypes(TypeFrame types, ImperativeSyntaxWriter writer) {
            if (!types.TryGetVariable(this.VariableName, out _)) {
                throw TypeException.VariableUndefined(this.Location, this.VariableName);
            }
        }

        public string[] Write() { return Array.Empty<string>(); }
    }
}