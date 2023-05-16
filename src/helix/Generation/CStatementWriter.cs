using Helix.Analysis;
using Helix.Analysis.Flow;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Generation {
    public enum CVariableKind {
        Local, Allocated
    }

    public interface ICStatementWriter : ICWriter {
        public ICStatementWriter WriteStatement(ICStatement stat);

        public ICStatementWriter WriteEmptyLine();

        public ICSyntax WriteImpureExpression(ICSyntax type, ICSyntax expr);

        public void RegisterLifetime(Lifetime lifetime, ICSyntax value);

        public ICSyntax GetLifetime(Lifetime lifetime);

        public void RegisterVariableKind(IdentifierPath path, CVariableKind kind);

        public CVariableKind GetVariableKind(IdentifierPath path);

        public ICSyntax CalculateSmallestLifetime(TokenLocation loc, IEnumerable<Lifetime> lifetimes);

        // Mixins
        public ICStatementWriter WriteComment(string comment) {
            return this.WriteStatement(new CComment(comment));
        }

        public ICStatementWriter WriteStatement(ICSyntax syntax) {
            return this.WriteStatement(new CSyntaxStatement() { 
                Value = syntax
            });
        }

        public void RegisterLifetimes(IdentifierPath basePath, LifetimeBundle bundle, ICSyntax syntax) {
            // This registers each new lifetime and member path that results from this dereference
            foreach (var (relPath, lifetime) in bundle.Components) {
                this.RegisterMemberPath(basePath, relPath);

                foreach (var segment in relPath.Segments) {
                    syntax = new CMemberAccess() {
                        Target = syntax,
                        MemberName = segment,
                        IsPointerAccess = true
                    };
                }

                this.RegisterLifetime(lifetime, new CMemberAccess() {
                    Target = syntax,
                    MemberName = "region"
                });
            }
        }
    }

    public class CStatementWriter : ICStatementWriter {
        private readonly ICWriter prev;
        private readonly IList<ICStatement> stats;

        private readonly Dictionary<Lifetime, ICSyntax> lifetimes = new();
        private readonly Dictionary<ValueList<Lifetime>, ICSyntax> lifetimeCombinations = new();
        private readonly Dictionary<IdentifierPath, CVariableKind> variableKinds = new();

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
            this.lifetimes[lifetime] = value;
        }

        public ICSyntax GetLifetime(Lifetime lifetime) {
            if (this.prev is CStatementWriter statWriter) {
                if (statWriter.lifetimes.TryGetValue(lifetime, out var value)) {
                    return value;
                }
            }

            return this.lifetimes[lifetime];
        }

        public ICSyntax CalculateSmallestLifetime(TokenLocation loc, IEnumerable<Lifetime> lifetimes) {
            var lifetimeList = lifetimes.Where(x => x != Lifetime.Stack).ToValueList();

            if (lifetimeList.Count == 0) {
                return new CVariableLiteral("get_smallest_lifetime()");
            }
            else if (lifetimeList.Count == 1) {
                return this.GetLifetime(lifetimeList[0]);
            }
            else if (this.lifetimeCombinations.TryGetValue(lifetimeList, out var value)) {
                return value;
            }

            var values = lifetimes
                .Select(x => GetLifetime(x))
                .ToArray();

            var tempName = this.GetVariableName();
            var decl = new CVariableDeclaration() {
                Name = tempName,
                Type = new CNamedType("int"),
                Assignment = Option.Some(values.First())
            };

            this.WriteEmptyLine();
            this.WriteStatement(new CComment($"Line {loc.Line}: Region calculation"));
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

            this.lifetimeCombinations[lifetimeList] = new CVariableLiteral(tempName);
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

        public void RegisterMemberPath(IdentifierPath varPath, IdentifierPath memberPath) {
            this.prev.RegisterMemberPath(varPath, memberPath);
        }

        public void RegisterVariableKind(IdentifierPath path, CVariableKind kind) {
            this.variableKinds[path] = kind;
        }

        public CVariableKind GetVariableKind(IdentifierPath path) {
            return this.variableKinds[path];
        }
    }
}
