using Trophy.Analysis;
using Trophy.Parsing;
using System.Linq;

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

        public ISyntaxB CheckNames(INameRecorder names) {
            var region = IdentifierPath.StackPath;

            // Make sure that our region exists
            if (this.region != "stack") {
                var segments = names.CurrentRegion.Segments;
                if (!segments.Contains(this.region)) {
                    throw TypeCheckingErrors.RegionUndefined(this.Location, this.region);
                }

                region = new IdentifierPath(segments.TakeWhile(x => x != this.region)).Append(this.region);
            }

            // Push our region
            names.PushRegion(region);
            var target = this.arg.CheckNames(names);
            names.PopRegion();

            return target;
        }
    }
}