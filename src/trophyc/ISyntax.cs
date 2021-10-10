using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy {
    public interface ISyntaxA {
        public TokenLocation Location { get; }

        public ISyntaxB CheckNames(INamesRecorder names);

        public IOption<ITrophyType> ResolveToType(INamesRecorder names) => Option.None<ITrophyType>();
    }

    public interface ISyntaxB {
        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage { get; }

        public ISyntaxC CheckTypes(ITypesRecorder types);
    }

    public interface ISyntaxC {
        public ITrophyType ReturnType { get; }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter statWriter);
    }

    public interface IDeclarationA {
        public TokenLocation Location { get; }

        public IDeclarationA DeclareNames(INamesRecorder names);

        public IDeclarationB ResolveNames(INamesRecorder names);
    }

    public interface IDeclarationB {
        public IDeclarationB DeclareTypes(ITypesRecorder types);

        public IDeclarationC ResolveTypes(ITypesRecorder types);
    }

    public interface IDeclarationC {
        public void GenerateCode(ICWriter writer);
    }

    public enum VariableUsageKind {
        Captured, CapturedAndMutated, Region
    }

    public class VariableUsage {
        public IdentifierPath VariablePath { get; }

        public VariableUsageKind Kind { get; }

        public VariableUsage(IdentifierPath path, VariableUsageKind kind) {
            this.VariablePath = path;
            this.Kind = kind;
        }

        public override bool Equals(object obj) {
            if (obj is not VariableUsage other) {
                return false;
            }

            return this.Kind == other.Kind
                && this.VariablePath == other.VariablePath;
        }

        public override int GetHashCode() {
            return this.Kind.GetHashCode() + 7 * this.VariablePath.GetHashCode();
        }
    }
}