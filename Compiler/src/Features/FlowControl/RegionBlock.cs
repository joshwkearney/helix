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
            var parent = RegionsHelper.GetClosestHeap(names.CurrentRegion);

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

            return new RegionBlockSyntaxB(this.Location, body, region, parent);
        }
    }

    public class RegionBlockSyntaxB : ISyntaxB {
        private readonly ISyntaxB body;
        private readonly IdentifierPath region;
        private readonly IdentifierPath parentHeap;

        public TokenLocation Location { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get => this.body.VariableUsage.Add(this.parentHeap, VariableUsageKind.Region).Remove(this.region);
        }

        public RegionBlockSyntaxB(TokenLocation location, ISyntaxB body, IdentifierPath region, IdentifierPath parent) {
            this.Location = location;
            this.body = body;
            this.region = region;
            this.parentHeap = parent;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            var body = this.body.CheckTypes(types);

            // Make sure that body does not return something from this region
            foreach (var lifetime in body.Lifetimes) {
                if (!lifetime.Outlives(this.region)) {
                    throw TypeCheckingErrors.LifetimeExceeded(this.Location, this.region.Pop(), lifetime);
                }
            }

            return new RegionBlockSyntaxC(body, this.region.Segments.Last(), this.parentHeap.Segments.Last());
        }
    }

    public class RegionBlockSyntaxC : ISyntaxC {
        private static int counter = 0;

        private readonly ISyntaxC body;
        private readonly string regionName;
        private readonly string parentRegionName;

        public ITrophyType ReturnType => this.body.ReturnType;

        public ImmutableHashSet<IdentifierPath> Lifetimes => this.body.Lifetimes;

        public RegionBlockSyntaxC(ISyntaxC body, string region, string parent) {
            this.body = body;
            this.regionName = region;
            this.parentRegionName = parent;
        }

        public CExpression GenerateCode(ICWriter declWriter, ICStatementWriter statWriter) {
            var regionType = CType.NamedType("Region*");
            var bufferName = "jump_buffer_" + counter++;

            // Write the region
            statWriter.WriteStatement(CStatement.Comment("Create new region"));
            statWriter.WriteStatement(CStatement.VariableDeclaration(
                regionType,
                this.regionName,
                CExpression.IntLiteral(0)));

            statWriter.WriteStatement(CStatement.VariableDeclaration(CType.NamedType("jmp_buf"), bufferName));

            var cond = CExpression.VariableLiteral(bufferName);
            cond = CExpression.Invoke(CExpression.VariableLiteral("setjmp"), new[] { cond });
            cond = CExpression.BinaryExpression(CExpression.IntLiteral(0), cond, Primitives.BinaryOperation.NotEqualTo);
            cond = CExpression.Invoke(CExpression.VariableLiteral("HEDLEY_UNLIKELY"), new[] { cond });

            var cleanup = CStatement.FromExpression(
                CExpression.Invoke(
                    CExpression.VariableLiteral("region_delete"),
                    new[] { CExpression.VariableLiteral(this.regionName) }));

            var parentPanic = CStatement.FromExpression(
                CExpression.Invoke(CExpression.VariableLiteral("region_panic"), new[] { CExpression.VariableLiteral(this.parentRegionName) }));

            var ifStatement = CStatement.If(cond, new[] { cleanup, parentPanic });

            statWriter.WriteStatement(ifStatement);
            statWriter.WriteStatement(CStatement.NewLine());

            var newRegion = CExpression.Invoke(CExpression.VariableLiteral("region_create"), new[] { 
                CExpression.AddressOf(CExpression.VariableLiteral(bufferName))
            });

            var assign = CStatement.Assignment(CExpression.VariableLiteral(this.regionName), newRegion);
            statWriter.WriteStatement(assign);
            statWriter.WriteStatement(CStatement.NewLine());

            // Write the body
            var body = this.body.GenerateCode(declWriter, statWriter);

            // Delete the region
            statWriter.WriteStatement(cleanup);

            return body;
        }
    }
}