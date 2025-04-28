using Helix.CodeGeneration;
using Helix.CodeGeneration.Syntax;
using Helix.Parsing;
using Helix.TypeChecking;
using Helix.Types;

namespace Helix.Syntax.TypedTree.Functions {
    public record InvokeTypedTree : ITypedTree {
        public required TokenLocation Location { get; init; }

        public required IdentifierPath FunctionPath { get; init; }

        public required FunctionType FunctionSignature { get; init; }
        
        public required IReadOnlyList<ITypedTree> Arguments { get; init; }
        
        public required bool AlwaysJumps { get; init; }
        
        public HelixType ReturnType => this.FunctionSignature.ReturnType;

        public bool IsPure => false;

        public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
            var args = this.Arguments
                .Select(x => x.GenerateCode(types, writer))
                .ToArray();

            var result = new CInvoke() {
                Target = new CVariableLiteral(writer.GetVariableName(this.FunctionPath)),
                Arguments = args
            };

            var name = writer.GetVariableName();

            var stat = new CVariableDeclaration() {
                Name = name,
                Type = writer.ConvertType(this.FunctionSignature.ReturnType, types),
                Assignment = result
            };

            writer.WriteComment($"Line {this.Location.Line}: Function call");
            writer.WriteStatement(stat);
            writer.WriteEmptyLine();

            return new CVariableLiteral(name);
        }
    }
}