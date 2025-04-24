using Helix.Analysis;
using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using Helix.Syntax;

namespace Helix.Features.Unions;

public class FlowVarSyntax : ISyntax {
    public required StructMember UnionMember { get; init; }
        
    public required IdentifierPath ShadowedPath { get; init; }

    public required IdentifierPath Path { get; init; }
        
    public required TokenLocation Location { get; init; }

    public HelixType ReturnType => this.UnionMember.Type;

    public ISyntaxPredicate Predicate => ISyntaxPredicate.Empty;

    public ISyntax ToRValue(TypeFrame types) => this;

    public ICSyntax GenerateCode(TypeFrame types, ICStatementWriter writer) {
        ICSyntax assign = new CAddressOf() {
            Target = new CMemberAccess() {
                Target = new CMemberAccess() {
                    Target = new CVariableLiteral(writer.GetVariableName(this.ShadowedPath)),
                    MemberName = "data"
                },
                MemberName = this.UnionMember.Name
            }
        };

        var name = writer.GetVariableName(this.Path);
        var cReturnType = new CPointerType(writer.ConvertType(this.UnionMember.Type, types));

        var stat = new CVariableDeclaration() {
            Type = cReturnType,
            Name = name,
            Assignment = Option.Some(assign)
        };

        writer.WriteComment($"Line {this.Location.Line}: Union downcast flowtyping");
        writer.WriteStatement(stat);
        writer.WriteEmptyLine();
        writer.VariableKinds[this.Path] = CVariableKind.Allocated;

        writer.ShadowedLifetimeSources[this.ShadowedPath] = this.Path;

        return new CIntLiteral(0);
    }
}