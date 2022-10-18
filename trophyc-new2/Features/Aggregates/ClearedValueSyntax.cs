using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Parsing;
using Trophy.Generation.Syntax;
using Trophy.Generation;

namespace Trophy.Features.Aggregates {
    public class ClearedValueSyntax : ISyntax {
        private readonly TrophyType returnType;

        public TokenLocation Location { get; }

        public ClearedValueSyntax(TokenLocation loc, TrophyType type) {

            this.Location = loc;
            this.returnType = type;
        }

        public Option<TrophyType> TryInterpret(INamesRecorder names) => Option.None;

        public ISyntax CheckTypes(ITypesRecorder types) => this;

        public ISyntax ToRValue(ITypesRecorder types) => this;

        public ICSyntax GenerateCode(ICStatementWriter writer) {
            var name = writer.GetVariableName();

            var varDecl = new CVariableDeclaration() {
                Type = writer.ConvertType(this.returnType),
                Name = name
            };

            var memset = new CInvoke() {
                Target = new CVariableLiteral("memset"),
                Arguments = new ICSyntax[] {
                    new CAddressOf() {
                        Target = new CVariableLiteral(name)
                    },
                    new CIntLiteral(0),
                    new CSizeof() {
                        Target = new CVariableLiteral(name)
                    }
                }
            };

            writer.WriteEmptyLine();
            writer.WriteComment($"New struct literal for {this.returnType}");
            writer.WriteStatement(varDecl);
            writer.WriteStatement(memset);

            writer.WriteEmptyLine();

            return new CVariableLiteral(name);
        }
    }
}