using Helix.Analysis;
using Helix.Analysis.Predicates;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;
using Helix.Features.Types;

namespace Helix.Features.Functions {
    public record InvokeSyntax : ISyntax {
        public required TokenLocation Location { get; init; }

        public required IdentifierPath FunctionPath { get; init; }

        public required FunctionType FunctionSignature { get; init; }
        
        public required IReadOnlyList<ISyntax> Arguments { get; init; }
        
        public HelixType ReturnType => this.FunctionSignature.ReturnType;

        public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;

        public bool IsPure => false;
        
        public ISyntax ToRValue(TypeFrame types) => this;

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

            return new CVariableLiteral(name);
        }
    }
}