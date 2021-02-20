using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.Analysis;
using Attempt20.Analysis.Types;
using Attempt20.CodeGeneration.CSyntax;
using Attempt20.Parsing;

namespace Attempt20.Features.Variables {
    public class LetParseSyntax : IParsedSyntax {
        private IdentifierPath region;

        public TokenLocation Location { get; set; }

        public string VariableName { get; set; }

        public IParsedSyntax AssignExpression { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            this.AssignExpression = this.AssignExpression.CheckNames(names);

            // Make sure we're not shadowing another variable
            if (names.TryFindName(this.VariableName, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, this.VariableName);
            }

            // Save the region
            this.region = names.CurrentRegion;

            // Declare this variable
            names.DeclareLocalName(names.CurrentScope.Append(this.VariableName), NameTarget.Variable);

            return this;
        }

        public ISyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            var assign = this.AssignExpression.CheckTypes(names, types);

            VariableInfo info;
            if (this.region == IdentifierPath.StackPath) {
                info = new VariableInfo(
                    assign.ReturnType, 
                    VariableDefinitionKind.Local, 
                    names.GetNewVariableId(),
                    assign.Lifetimes, 
                    new[] { this.region }.ToImmutableHashSet());
            }
            else {
                info = new VariableInfo(
                    assign.ReturnType, 
                    VariableDefinitionKind.LocalAllocated, 
                    names.GetNewVariableId(),
                    assign.Lifetimes, 
                    new[] { this.region }.ToImmutableHashSet());
            }

            // Declare this variable
            types.DeclareVariable(names.CurrentScope.Append(this.VariableName), info);

            return new LetTypeCheckedSyntax() {
                Location = this.Location,
                VariableName = this.VariableName,
                VariableInfo = info,
                AssignExpression = assign,
                ReturnType = TrophyType.Void,
                Lifetimes = ImmutableHashSet.Create<IdentifierPath>(),
                Region = this.region
            };
        }
    }

    public class LetTypeCheckedSyntax : ISyntax {
        public string VariableName { get; set; }

        public VariableInfo VariableInfo { get; set; }

        public ISyntax AssignExpression { get; set; }

        public TokenLocation Location { get; set; }

        public TrophyType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public IdentifierPath Region { get; set; }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var assign = this.AssignExpression.GenerateCode(declWriter, statWriter);
            var typeName = declWriter.ConvertType(this.AssignExpression.ReturnType);

            if (this.VariableInfo.DefinitionKind == VariableDefinitionKind.Local) {
                var stat = CStatement.VariableDeclaration(
                    typeName,
                    this.VariableName + this.VariableInfo.UniqueId,
                    assign);

                statWriter.WriteStatement(stat);
                statWriter.WriteStatement(CStatement.NewLine());
            }
            else if (this.VariableInfo.DefinitionKind == VariableDefinitionKind.LocalAllocated) {
                declWriter.RequireRegions();

                var regionName = this.Region.Segments.Last();
                var stat = CStatement.VariableDeclaration(
                    CType.Pointer(typeName),
                    this.VariableName + this.VariableInfo.UniqueId,
                    CExpression.Invoke(CExpression.VariableLiteral("$region_alloc"), new[] {
                        CExpression.VariableLiteral(regionName), CExpression.Sizeof(typeName)
                    }));
                var stat2 = CStatement.Assignment(
                    CExpression.Dereference(CExpression.VariableLiteral(this.VariableName)),
                    assign);

                statWriter.WriteStatement(stat);
                statWriter.WriteStatement(stat2);
                statWriter.WriteStatement(CStatement.NewLine());
            }
            else {
                throw new Exception("This should never happen");
            }

            return CExpression.IntLiteral(0);
        }
    }
}
