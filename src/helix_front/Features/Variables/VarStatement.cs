using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Variables;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Aggregates;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.FlowControl;
using System.Xml.Linq;
using Helix.Collections;
using Helix.HelixMinusMinus;
using Helix.Features.Primitives;

namespace Helix.Parsing {
    public partial class Parser {
        private IParseTree VarExpression() {
            var startLok = this.Advance(TokenKind.VarKeyword).Location;
            var names = new List<string>();
            var types = new List<Option<IParseTree>>();

            while (true) {
                var name = this.Advance(TokenKind.Identifier).Value;
                names.Add(name);

                if (this.TryAdvance(TokenKind.AsKeyword)) {
                    types.Add(Option.Some(this.TopExpression()));
                }
                else {
                    types.Add(Option.None);
                }

                if (this.TryAdvance(TokenKind.Assignment)) {
                    break;
                }
                else {
                    this.Advance(TokenKind.Comma);
                }
            }

            var assign = this.TopExpression();
            var loc = startLok.Span(assign.Location);

            return new VariableParseStatement(loc, names, types, assign);
        }
    }
}

namespace Helix {
    public record VariableParseStatement(
        TokenLocation Location, 
        IReadOnlyList<string> Names,                                 
        IReadOnlyList<Option<IParseTree>> Types, 
        IParseTree Assign) : IParseTree {

        public ImperativeExpression ToImperativeSyntax(ImperativeSyntaxWriter writer) {
            // TODO: Re-add destructuring at some point
            var value = this.Assign.ToImperativeSyntax(writer);

            writer.AddStatement(new VariableStatement(this.Location, this.Names[0], value));
            return ImperativeExpression.Void;
        }
    }

    public record VariableStatement(
        TokenLocation Location, 
        string VariableName, 
        ImperativeExpression Value) : IImperativeStatement {

        public void CheckTypes(TypeFrame types, ImperativeSyntaxWriter writer) {
            // If this is a compound assignment, check if we have the right
            // number of names and then recurse
            var assignType = this.Value.GetReturnType(types);

            // Make sure we're not shadowing anybody
            if (types.TryGetVariable(this.VariableName, out _)) {
                throw TypeException.IdentifierDefined(this.Location, this.VariableName);
            }

            var varSig = new PointerType(assignType);

            types.Locals = types.Locals.Add(this.VariableName, new LocalInfo(varSig));
            writer.AddStatement(new VariableStatement(this.Location, this.VariableName, this.Value));
        }

        public string[] Write() {
            return new[] { $"var {this.VariableName} = {this.Value};" };
        }
    }
}