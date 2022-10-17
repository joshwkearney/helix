using System.Collections.Immutable;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Variables {
    public enum VariableAccessKind {
        ValueAccess, LiteralAccess
    }

    public class VariableAccessSyntaxB : ISyntaxB {
        private readonly IdentifierPath path;
        private readonly VariableAccessKind kind;

        public TokenLocation Location { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => new[] { new VariableUsage(this.path, VariableUsageKind.Captured) }.ToImmutableHashSet();
        }

        public VariableAccessSyntaxB(TokenLocation loc, IdentifierPath path, VariableAccessKind kind) {
            this.Location = loc;
            this.path = path;
            this.kind = kind;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var info = types.TryGetVariable(this.path).GetValue();
            var returnType = info.Type;

            if (this.kind == VariableAccessKind.LiteralAccess) {
                // Make sure we're not literally accessing a non-variable parameter
                if (info.Kind == VariableKind.Value) {
                    throw TypeCheckingErrors.ExpectedVariableType(this.Location, info.Type);
                }
            }            
            else if (this.kind == VariableAccessKind.ValueAccess) {
                if (info.Kind != VariableKind.Value) {
                    returnType = (info.Type as VarRefType).InnerType;
                }
            }

            return new VariableAccessdSyntaxC(info, this.kind, returnType);
        }
    }

    public class VariableAccessdSyntaxC : ISyntaxC {
        private readonly VariableInfo info;
        private readonly VariableAccessKind kind;

        public ITrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage { get; }

        public VariableAccessdSyntaxC(VariableInfo info, VariableAccessKind kind, ITrophyType returnType) {
            this.info = info;
            this.kind = kind;
            this.ReturnType = returnType;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var cname = "$" + this.info.Name + this.info.UniqueId;

            if (this.info.Source == VariableSource.Local) {
                if (this.kind == VariableAccessKind.ValueAccess) {
                    return CExpression.VariableLiteral(cname);
                }
                else {
                    return CExpression.AddressOf(CExpression.VariableLiteral(cname));
                }
            }
            else {
                if (this.kind == VariableAccessKind.ValueAccess) {
                    if (this.info.Kind == VariableKind.Value) {
                        return CExpression.VariableLiteral(cname);
                    }
                    else {
                        return CExpression.Dereference(CExpression.VariableLiteral(cname));
                    }
                }
                else {
                    return CExpression.VariableLiteral(cname);
                }
            }
        }
    }
}
