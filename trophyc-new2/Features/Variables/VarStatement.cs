using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.SyntaxTree;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;
using Trophy.Parsing.ParseTree;

namespace Trophy.Parsing
{
    public partial class Parser {
        private IParseTree VarExpression() {
            var tok = this.Advance(TokenKind.VarKeyword);
            var name = this.Advance(TokenKind.Identifier).Value;

            this.Advance(TokenKind.Assignment);

            var assign = this.TopExpression();
            var loc = tok.Location.Span(assign.Location);

            return new VarParseStatement(loc, name, assign, true);
        }
    }
}

namespace Trophy {
    public class VarParseStatement : IParseTree {
        private readonly string name;
        private readonly IParseTree assign;
        private readonly bool isWritable;

        public TokenLocation Location { get; }

        public VarParseStatement(TokenLocation loc, string name, IParseTree assign, bool isWritable) {
            this.Location = loc;
            this.name = name;
            this.assign = assign;
            this.isWritable = isWritable;
        }

        public ISyntaxTree ResolveTypes(IdentifierPath scope, NamesRecorder names, TypesRecorder types, TypeContext context) {
            // Type check the assignment value
            var assign = this.assign.ResolveTypes(scope, names, types);

            // Declare this variable and make sure we're not shadowing another variable
            if (!names.TrySetName(scope, this.name, NameTarget.Variable)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.name);
            }

            types.SetVariable(scope.Append(this.name), assign.ReturnType, this.isWritable);

            return new VarStatement(scope.Append(this.name), assign, new PointerType(assign.ReturnType));
        }
    }

    public class VarStatement : ISyntaxTree {
        private readonly IdentifierPath path;
        private readonly ISyntaxTree assign;

        public TrophyType ReturnType { get; }

        public VarStatement(IdentifierPath path, ISyntaxTree assign, TrophyType retType) {
            this.path = path;
            this.assign = assign;
            this.ReturnType = retType;
        }

        public CExpression GenerateCode(CWriter writer, CStatementWriter statWriter) {
            var type = writer.ConvertType(this.assign.ReturnType);
            var value = this.assign.GenerateCode(writer, statWriter);
            var assign = CStatement.VariableDeclaration(type, this.path.ToCName(), value);

            statWriter.WriteSpacingLine();
            statWriter.WriteStatement(CStatement.Comment("Variable declaration statement"));
            statWriter.WriteStatement(assign);
            statWriter.WriteSpacingLine();

            return CExpression.AddressOf(CExpression.VariableLiteral(this.path.ToCName()));
        }
    }
}