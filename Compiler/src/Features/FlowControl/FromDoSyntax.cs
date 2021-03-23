using Trophy.Analysis;
using Trophy.Parsing;
using System.Linq;
using System.Collections.Immutable;

namespace Trophy.Features.FlowControl {
    public class FromSyntaxA : ISyntaxA {
        private readonly string region;
        private readonly ISyntaxA arg;

        public TokenLocation Location { get; }

        public FromSyntaxA(TokenLocation location, string region, ISyntaxA arg) {
            this.Location = location;
            this.region = region;
            this.arg = arg;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            IdentifierPath region;

            // Make sure that our region exists
            if (this.region == "stack") {
                region = RegionsHelper.GetClosestStack(names.Context.Region);
            }
            else {
                var segments = names.Context.Region.Segments.ToList();
                if (!segments.Contains(this.region)) {
                    throw TypeCheckingErrors.RegionUndefined(this.Location, this.region);
                }

                var index = segments.LastIndexOf(this.region);

                region = new IdentifierPath(segments.Take(index)).Append(this.region);
            }

            // Push our region
            var context = names.Context.WithRegion(_ => region);
            var target = names.WithContext(context, names => this.arg.CheckNames(names));

            return new FromSyntaxB(this.Location, target, region);
        }

    }

    public class FromSyntaxB : ISyntaxB {
        private readonly ISyntaxB arg;
        private readonly IdentifierPath region;

        public TokenLocation Location { get; }

        public FromSyntaxB(TokenLocation location, ISyntaxB arg, IdentifierPath region) {
            this.Location = location;
            this.arg = arg;
            this.region = region;
        }

        public IImmutableSet<VariableUsage> VariableUsage {
            get => this.arg.VariableUsage
                .Add(new VariableUsage(this.region, VariableUsageKind.Region));
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            return this.arg.CheckTypes(types);
        }
    }
}