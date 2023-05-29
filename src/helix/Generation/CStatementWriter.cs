using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.Collections;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Parsing;
using System.Collections.Immutable;

namespace Helix.Generation {
    public enum CVariableKind {
        Local, Allocated
    }

    public interface ICStatementWriter : ICWriter {
        public IDictionary<IdentifierPath, CVariableKind> VariableKinds { get; }

        public IDictionary<IdentifierPath, IdentifierPath> ShadowedLifetimeSources { get; }

        public ICStatementWriter WriteStatement(ICStatement stat);

        public ICStatementWriter WriteEmptyLine();

        public ICSyntax GetLifetime(Lifetime lifetime, TypeFrame flow);

        public ICSyntax CalculateSmallestLifetime(TokenLocation loc, IEnumerable<Lifetime> lifetimes, TypeFrame flow);

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

        private readonly Dictionary<Lifetime, ICSyntax> lifetimes = new();
        private ImmutableDictionary<ValueSet<Lifetime>, ICSyntax> lifetimeCombinations;

        public IDictionary<IdentifierPath, CVariableKind> VariableKinds { get; }

        public IDictionary<IdentifierPath, IdentifierPath> ShadowedLifetimeSources { get; }

        public CStatementWriter(ICWriter prev, IList<ICStatement> stats) {
            this.prev = prev;
            this.stats = stats;

            if (prev is CStatementWriter statWriter) {
                this.lifetimeCombinations = statWriter.lifetimeCombinations;
                this.VariableKinds = statWriter.VariableKinds;
                this.ShadowedLifetimeSources = statWriter.ShadowedLifetimeSources;
            }
            else {
                this.lifetimeCombinations = ImmutableDictionary<ValueSet<Lifetime>, ICSyntax>.Empty;
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

        public ICSyntax GetLifetime(Lifetime lifetime, TypeFrame flow) {
            if (this.prev is CStatementWriter statWriter) {
                if (statWriter.lifetimes.TryGetValue(lifetime, out var value)) {
                    return value;
                }
            }

            if (!this.lifetimes.ContainsKey(lifetime)) {
                this.lifetimes[lifetime] = lifetime.GenerateCode(flow, this);
            }

            return this.lifetimes[lifetime];
        }

        public ICSyntax CalculateSmallestLifetime(TokenLocation loc, IEnumerable<Lifetime> lifetimes, TypeFrame flow) {
            var lifetimeList = lifetimes.ToValueSet();

            if (lifetimeList.Count == 0) {
                // TODO: Put back _region_min
                return new CVariableLiteral("_return_region");
            }
            else if (lifetimeList.Count == 1) {
                return this.GetLifetime(lifetimeList.First(), flow);
            }
            else if (this.lifetimeCombinations.TryGetValue(lifetimeList, out var value)) {
                return value;
            }

            var values = lifetimes
                .Select(x => this.GetLifetime(x, flow))
                .ToArray();

            var tempName = this.GetVariableName();
            var decl = new CVariableDeclaration() {
                Name = tempName,
                Type = new CNamedType("_Region*"),
                Assignment = Option.Some(values.First())
            };

            this.WriteEmptyLine();
            this.WriteStatement(new CComment($"Line {loc.Line}: Region calculation"));
            this.WriteStatement(decl);

            foreach (var item in values.Skip(1)) {
                this.WriteStatement(new CAssignment() {
                    Left = new CVariableLiteral(tempName),
                    Right = new CVariableLiteral($"_region_min({tempName}, {item.WriteToC()})")
                });
            }

            this.WriteEmptyLine();

            this.lifetimeCombinations = this.lifetimeCombinations.SetItem(lifetimeList, new CVariableLiteral(tempName));
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
