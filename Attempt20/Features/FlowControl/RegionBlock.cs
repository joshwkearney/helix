using System;
using System.Collections.Immutable;
using System.Linq;
using Attempt20.CodeGeneration;

namespace Attempt20.Features.FlowControl {
    public class RegionBlockParsedSyntax : IParsedSyntax {
        private static int regionCounter;
        private IdentifierPath regionPath;

        public TokenLocation Location { get; set; }

        public IOption<string> RegionName { get; set; }

        public IParsedSyntax Body { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            var name = this.RegionName.GetValueOr(() => "$anon_region_" + regionCounter++);
            this.regionPath = names.CurrentRegion.Append(name);

            // Make sure this name doesn't exist
            if (names.TryFindName(name, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, name);
            }

            // Reserve the name
            names.DeclareLocalName(names.CurrentScope.Append(name), NameTarget.Region);

            // Push this region
            names.PushRegion(this.regionPath);
            this.Body = this.Body.CheckNames(names);
            names.PopRegion();

            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            names.PushRegion(this.regionPath);
            var body = this.Body.CheckTypes(names, types);
            names.PopRegion();

            // Make sure that body does not return something from this region
            foreach (var lifetime in body.Lifetimes) {
                if (!lifetime.Outlives(this.regionPath)) {
                    throw TypeCheckingErrors.LifetimeExceeded(this.Location, this.regionPath.Pop(), lifetime);
                }
            }

            return new RegionBlockTypeCheckedSyntax() {
                Location = this.Location,
                ReturnType = body.ReturnType,
                Body = body,
                RegionName = this.regionPath.Segments.Last(),
                Lifetimes = body.Lifetimes
            };
        }
    }

    public class RegionBlockTypeCheckedSyntax : ITypeCheckedSyntax {
        public TokenLocation Location { get; set; }

        public LanguageType ReturnType { get; set; }

        public ImmutableHashSet<IdentifierPath> Lifetimes { get; set; }

        public string RegionName { get; set; }

        public ITypeCheckedSyntax Body { get; set; }

        public CExpression GenerateCode(ICDeclarationWriter declWriter, ICStatementWriter statWriter) {
            var regionType = CType.NamedType("$Region*");

            // Write the region
            statWriter.WriteStatement(CStatement.VariableDeclaration(
                regionType,
                this.RegionName,
                CExpression.Invoke(
                    CExpression.VariableLiteral("$region_create"),
                    new CExpression[0])));

            // Write the body
            var body = this.Body.GenerateCode(declWriter, statWriter);

            // Delete the region
            statWriter.WriteStatement(CStatement.FromExpression(
                CExpression.Invoke(
                    CExpression.VariableLiteral("$region_delete"),
                    new[] { CExpression.VariableLiteral(this.RegionName) })));

            return body;
        }
    }
}
