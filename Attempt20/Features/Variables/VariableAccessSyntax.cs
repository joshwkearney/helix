using System.Collections.Immutable;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Features.Functions;
using Attempt20.Parsing;

namespace Attempt20.Features.Variables {
    public enum VariableAccessKind {
        ValueAccess, LiteralAccess
    }

    public class VariableAccessParseSyntax : IParsedSyntax {
        public string VariableName { get; set; }

        public IdentifierPath VariablePath { get; private set; }

        public TokenLocation Location { get; set; }

        public VariableAccessKind AccessKind { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            // Make sure this name exists
            if (!names.TryFindName(this.VariableName, out var target, out var path)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.VariableName);
            }

            // If the name is a function return a different syntax tree
            if (target == NameTarget.Function) {
                return new FunctionAccessParsedSyntax() {
                    Location = this.Location,
                    FunctionPath = path
                };
            }

            // Make sure this name is a variable
            if (target != NameTarget.Variable) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.VariableName);
            }

            // Store the variable path for later
            this.VariablePath = path;

            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            if (!types.TryGetVariable(this.VariablePath).TryGetValue(out var info)) {
                throw TypeCheckingErrors.VariableUndefined(this.Location, this.VariableName);
            }

            var returnType = info.Type;
            var lifetimes = ImmutableHashSet.Create<IdentifierPath>();

            if (this.AccessKind == VariableAccessKind.ValueAccess) {
                lifetimes = info.ValueLifetimes;

                // If we're accessing a parameter, automatically dereference it
                if (info.DefinitionKind == VariableDefinitionKind.Parameter && info.Type is VariableType varType) {
                    returnType = varType.InnerType;
                }

                // If the variable type is copiable, don't propagate any lifetimes
                if (returnType.GetCopiability(types) == TypeCopiability.Unconditional) {
                    lifetimes = ImmutableHashSet.Create<IdentifierPath>();
                }
            }
            else {
                lifetimes = info.VariableLifetimes;

                // Make sure we're not literally accessing a non-variable parameter
                if (info.DefinitionKind == VariableDefinitionKind.Parameter && info.Type is not VariableType) {
                    throw TypeCheckingErrors.ExpectedVariableType(this.Location, info.Type);
                }

                // For non-parameter access, return a variable type of the accessed variable
                if (info.DefinitionKind == VariableDefinitionKind.Local || info.DefinitionKind == VariableDefinitionKind.LocalAllocated) {
                    returnType = new VariableType(returnType);
                }
            }

            return new VariableAccessTypeCheckedSyntax() {
                Location = this.Location,
                VariableName = this.VariableName,
                VariableInfo = info,
                ReturnType = returnType,
                Lifetimes = lifetimes,
                AccessKind = this.AccessKind
            };
        }
    };

    public class VariableAccessTypeCheckedSyntax : ISyntax {
        public string VariableName { get; set; }

        public VariableInfo VariableInfo { get; set; }

        public TokenLocation Location { get; set; }

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public VariableAccessKind AccessKind { get; set; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            if (this.AccessKind == VariableAccessKind.ValueAccess) {
                if (this.VariableInfo.Type is VariableType && this.VariableInfo.DefinitionKind == VariableDefinitionKind.Parameter) {
                    return CExpression.Dereference(CExpression.VariableLiteral(this.VariableName));
                }
                else if (this.VariableInfo.DefinitionKind == VariableDefinitionKind.LocalAllocated) {
                    return CExpression.Dereference(CExpression.VariableLiteral(this.VariableName));
                }
                else {
                    return CExpression.VariableLiteral(this.VariableName);
                }
            }
            else {
                if (this.VariableInfo.DefinitionKind == VariableDefinitionKind.Parameter) {
                    return CExpression.VariableLiteral(this.VariableName);
                }
                else if (this.VariableInfo.DefinitionKind == VariableDefinitionKind.LocalAllocated) {
                    return CExpression.VariableLiteral(this.VariableName);
                }
                else {
                    return CExpression.AddressOf(CExpression.VariableLiteral(this.VariableName));
                }
            }
        }
    }
}
