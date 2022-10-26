using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;

namespace Helix.Generation {
    public interface ICStatementWriter : ICWriter {
        public ICStatementWriter WriteStatement(ICStatement stat);

        public ICStatementWriter WriteEmptyLine();

        public ICSyntax WriteImpureExpression(ICSyntax type, ICSyntax expr);

        public void RegisterLifetime(Lifetime lifetime, ICSyntax value);

        public ICSyntax GetLifetime(Lifetime lifetime);

        public ICSyntax GetSmallestLifetime(ValueList<Lifetime> lifetimes);

        // Mixins
        public ICStatementWriter WriteComment(string comment) {
            return this.WriteStatement(new CComment(comment));
        }

        public ICStatementWriter WriteStatement(ICSyntax syntax) {
            return this.WriteStatement(new CSyntaxStatement() { 
                Value = syntax
            });
        }
    }

    public class CStatementWriter : ICStatementWriter {
        private readonly ICWriter prev;
        private readonly IList<ICStatement> stats;

        private readonly Dictionary<Lifetime, ICSyntax> lifetimes = new();
        private readonly Dictionary<ValueList<Lifetime>, ICSyntax> lifetimeCombinations = new();

        public CStatementWriter(ICWriter prev, IList<ICStatement> stats) {
            this.prev = prev;
            this.stats = stats;
        }

        public ICStatementWriter WriteStatement(ICStatement stat) {
            stats.Add(stat);

            return this;
        }

        public ICStatementWriter WriteEmptyLine() {
            if (this.stats.Any() && !this.stats.Last().IsEmpty) {
                this.WriteStatement(new CEmptyLine());
            }

            return this;
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

        public void RegisterLifetime(Lifetime lifetime, ICSyntax value) {
            var expr = this.WriteImpureExpression(new CNamedType("int"), value);

            this.lifetimes[lifetime] = expr;
        }

        public ICSyntax GetLifetime(Lifetime lifetime) {
            if (this.prev is CStatementWriter statWriter) {
                if (statWriter.lifetimes.TryGetValue(lifetime, out var value)) {
                    return value;
                }
            }

            return this.lifetimes[lifetime];
        }

        public ICSyntax GetSmallestLifetime(ValueList<Lifetime> lifetimes) {
            if (this.lifetimeCombinations.TryGetValue(lifetimes, out var value)) {
                return value;
            }

            var heapLifetime = new Lifetime(new IdentifierPath("$heap"), 0);

            if (lifetimes.Count == 1) {
                return this.GetLifetime(lifetimes[0]);
            }

            var values = lifetimes
                .Where(x => !x.Equals(heapLifetime))
                .Select(x => GetLifetime(x))
                .ToValueList();

            var tempName = this.GetVariableName();
            var decl = new CVariableDeclaration() {
                Name = tempName,
                Type = new CNamedType("int"),
                Assignment = Option.Some(values.First())
            };

            this.WriteEmptyLine();
            this.WriteStatement(decl);

            foreach (var item in values.Skip(1)) {
                this.WriteStatement(new CAssignment() { 
                    Left = new CVariableLiteral(tempName),
                    Right = new CTernaryExpression() {
                        Condition = new CBinaryExpression() {
                            Left = new CVariableLiteral(tempName),
                            Right = item,
                            Operation = BinaryOperationKind.LessThanOrEqualTo
                        },
                        PositiveBranch = new CVariableLiteral(tempName),
                        NegativeBranch = item
                    }
                });
            }

            this.WriteEmptyLine();

            this.lifetimeCombinations[lifetimes] = new CVariableLiteral(tempName);
            return new CVariableLiteral(tempName);
        }

        public string GetVariableName() => this.prev.GetVariableName();

        public string GetVariableName(IdentifierPath path) => this.prev.GetVariableName(path);

        public void WriteDeclaration1(ICStatement decl) => this.prev.WriteDeclaration1(decl);

        public void WriteDeclaration2(ICStatement decl) => this.prev.WriteDeclaration2(decl);

        public void WriteDeclaration3(ICStatement decl) => this.prev.WriteDeclaration3(decl);

        public void WriteDeclaration4(ICStatement decl) => this.prev.WriteDeclaration4(decl);

        public ICSyntax ConvertType(HelixType type) => this.prev.ConvertType(type);

        public void ResetTempNames() => this.prev.ResetTempNames();
    }
}
