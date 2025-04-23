using Helix.Analysis.TypeChecking;
using Helix.Analysis;
using Helix.Generation.Syntax;
using Helix.Generation;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Functions {
    public record FunctionAccessSyntax : ISyntaxTree {
        public IdentifierPath FunctionPath { get; }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => Enumerable.Empty<ISyntaxTree>();

        public bool IsPure => true;

        public FunctionAccessSyntax(TokenLocation loc, IdentifierPath path) {
            this.Location = loc;
            this.FunctionPath = path;
        }

        public virtual ISyntaxTree CheckTypes(TypeFrame types) {
            if (this.IsTypeChecked(types)) {
                return this;
            }

            var funcType = types.Locals[this.FunctionPath].Type;

            types.SyntaxTags[this] = new SyntaxTagBuilder(types)
                .WithReturnType(funcType)
                .Build();

            return this;
        }

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            return new CVariableLiteral(writer.GetVariableName(this.FunctionPath));
        }
    }
}