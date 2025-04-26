using Helix.Analysis;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Generation.Syntax;

namespace Helix.Generation {
    public enum CVariableKind {
        Local, Allocated
    }

    public interface ICStatementWriter : ICWriter {
        public IDictionary<IdentifierPath, CVariableKind> VariableKinds { get; }

        public IDictionary<IdentifierPath, IdentifierPath> ShadowedLifetimeSources { get; }

        public ICStatementWriter WriteStatement(ICStatement stat);

        public ICStatementWriter WriteEmptyLine();
        
        // Mixins
        public ICStatementWriter WriteComment(string comment) {
            return this.WriteStatement(new CComment(comment));
        }

        public ICStatementWriter WriteStatement(ICSyntax syntax) {
            return this.WriteStatement(new CSyntaxStatement() {
                Value = syntax
            });
        }

        public ICSyntax WriteImpureExpression(ICSyntax type, ICSyntax expr) {
            var name = this.GetVariableName();

            var stat = new CVariableDeclaration() {
                Type = type,
                Name = name,
                Assignment = Option.Some(expr)
            };

            this.WriteStatement(stat);

            return new CVariableLiteral(name);
        }
    }

    public class CStatementWriter : ICStatementWriter {
        private readonly ICWriter prev;
        private readonly IList<ICStatement> stats;
        
        public IDictionary<IdentifierPath, CVariableKind> VariableKinds { get; }

        public IDictionary<IdentifierPath, IdentifierPath> ShadowedLifetimeSources { get; }

        public CStatementWriter(ICWriter prev, IList<ICStatement> stats) {
            this.prev = prev;
            this.stats = stats;

            if (prev is CStatementWriter statWriter) {
                this.VariableKinds = statWriter.VariableKinds;
                this.ShadowedLifetimeSources = statWriter.ShadowedLifetimeSources;
            }
            else {
                this.VariableKinds = new Dictionary<IdentifierPath, CVariableKind>();
                this.ShadowedLifetimeSources = new Dictionary<IdentifierPath, IdentifierPath>();
            }
        }

        public ICStatementWriter WriteStatement(ICStatement stat) {
            this.stats.Add(stat);

            return this;
        }

        public ICStatementWriter WriteEmptyLine() {
            if (this.stats.Any() && !this.stats.Last().IsEmpty) {
                this.WriteStatement(new CEmptyLine());
            }

            return this;
        }
        
        public string GetVariableName() => this.prev.GetVariableName();

        public string GetVariableName(IdentifierPath path) => this.prev.GetVariableName(path);

        public void WriteDeclaration1(ICStatement decl) => this.prev.WriteDeclaration1(decl);

        public void WriteDeclaration2(ICStatement decl) => this.prev.WriteDeclaration2(decl);

        public void WriteDeclaration3(ICStatement decl) => this.prev.WriteDeclaration3(decl);

        public void WriteDeclaration4(ICStatement decl) => this.prev.WriteDeclaration4(decl);

        public ICSyntax ConvertType(HelixType type, TypeFrame types) => this.prev.ConvertType(type, types);

        public void ResetTempNames() => this.prev.ResetTempNames();
    }
}
