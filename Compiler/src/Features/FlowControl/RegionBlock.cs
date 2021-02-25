using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.FlowControl {
    public class RegionBlockSyntaxA : ISyntaxA {
        private readonly IOption<string> regionName;
        private readonly ISyntaxA body;

        public TokenLocation Location { get; }

        public RegionBlockSyntaxA(TokenLocation location, ISyntaxA body) {
            this.Location = location;
            this.body = body;
            this.regionName = Option.None<string>();
        }

        public RegionBlockSyntaxA(TokenLocation location, ISyntaxA body, string region) {
            this.Location = location;
            this.body = body;
            this.regionName = Option.Some(region);
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var name = this.regionName.GetValueOr(() => "$anon_region_" + names.GetNewVariableId());
            var region = names.CurrentRegion.Append(name);

            // Make sure this name doesn't exist
            if (names.TryFindName(name, out _, out _)) {
                throw TypeCheckingErrors.IdentifierDefined(this.Location, name);
            }

            // Reserve the name
            names.DeclareLocalName(names.CurrentScope.Append(name), NameTarget.Region);

            // Push this region
            names.PushRegion(region);
            var body = this.body.CheckNames(names);
            names.PopRegion();

            return new RegionBlockSyntaxB(this.Location, body, region);
        }
    }

    public class RegionBlockSyntaxB : ISyntaxB {
        private readonly ISyntaxB body;
        private readonly IdentifierPath region;

        public RegionBlockSyntaxB(TokenLocation location, ISyntaxB body, IdentifierPath region) {
            this.Location = location;
            this.body = body;
            this.region = region;
        }

        public TokenLocation Location { get; }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var body = this.body.CheckTypes(types);

            // Make sure that body does not return something from this region
            foreach (var lifetime in body.Lifetimes) {
                if (!lifetime.Outlives(this.region)) {
                    throw TypeCheckingErrors.LifetimeExceeded(this.Location, this.region.Pop(), lifetime);
                }
            }

            return new RegionBlockSyntaxC(body, this.region.Segments.Last());
        }
    }

    public class RegionBlockSyntaxC : ISyntaxC {
        private readonly ISyntaxC body;
        private readonly string region;

        public TrophyType ReturnType => this.body.ReturnType;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.body.Lifetimes;

        public RegionBlockSyntaxC(ISyntaxC body, string region) {
            this.body = body;
            this.region = region;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var regionType = CType.NamedType("$Region*");

            // Write the region
            statWriter.WriteStatement(CStatement.VariableDeclaration(
                regionType,
                this.region,
                CExpression.Invoke(
                    CExpression.VariableLiteral("$region_create"),
                    new CExpression[0])));

            // Write the body
            var body = this.body.GenerateCode(declWriter, statWriter);

            // Delete the region
            statWriter.WriteStatement(CStatement.FromExpression(
                CExpression.Invoke(
                    CExpression.VariableLiteral("$region_delete"),
                    new[] { CExpression.VariableLiteral(this.region) })));

            return body;
        }
    }
}