using Trophy.Analysis;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Features.Aggregates;
using Trophy.Features.Functions;
using Trophy.Parsing;

namespace Trophy.CodeGeneration {
    public class CStatementWriter : ITypesObserver, INamesObserver, ISyntaxNavigator {
        private readonly IList<CStatement> stats;
        private readonly CWriter writer;

        public IdentifierPath CurrentScope { get; }

        public CStatementWriter(IList<CStatement> stats, CWriter writer, string blockName) {
            this.writer = writer;
            this.stats = stats;
            this.CurrentScope = writer.CurrentScope.Append(blockName);
        }

        public CStatementWriter(IList<CStatement> stats, CWriter writer) {
            this.writer = writer;
            this.stats = stats;
            this.CurrentScope = writer.CurrentScope;
        }

        public CType ConvertType(TrophyType type) {
            return this.writer.ConvertType(type);
        }

        public string GetVariableName() {
            return this.writer.GetVariableName();
        }

        public string GetVariableName(IdentifierPath path) {
            return this.writer.GetVariableName(path);
        }

        public CStatementWriter WriteStatement(CStatement stat) {
            stats.Add(stat);

            return this;
        }

        public CStatementWriter WriteSpacingLine() {
            if (this.stats.Any() && !this.stats.Last().IsEmpty) {
                this.WriteStatement(CStatement.NewLine());
            }

            return this;
        }

        public CExpression WriteImpureExpression(CType type, CExpression expr) {
            var name = this.writer.GetVariableName();
            var stat = CStatement.VariableDeclaration(type, name, expr);

            this.WriteStatement(stat);

            return CExpression.VariableLiteral(name);
        }

        // Interface implementations
        public FunctionSignature GetFunction(IdentifierPath path) {
            return this.writer.GetFunction(path);
        }

        public VariableSignature GetVariable(IdentifierPath path) {
            return this.writer.GetVariable(path);
        }

        public AggregateSignature GetAggregate(IdentifierPath path) {
            return this.writer.GetAggregate(path);
        }

        public bool IsReserved(IdentifierPath path) {
            return this.writer.IsReserved(path);
        }

        public TrophyType GetReturnType(ISyntaxTree tree) {
            return this.writer.GetReturnType(tree);
        }

        public Option<NameTarget> TryResolveName(IdentifierPath path) {
            return writer.TryResolveName(path);
        }

        public void PushScope(IdentifierPath scope) {
            throw new InvalidOperationException();
        }

        public void PopScope() {
            throw new InvalidOperationException();
        }
    }
}
