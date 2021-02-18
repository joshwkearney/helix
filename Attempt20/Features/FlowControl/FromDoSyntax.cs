using System;
using System.Linq;

namespace Attempt20.Features.FlowControl {
    public class FromDoParsedSyntax : IParsedSyntax {
        private IdentifierPath region;

        public TokenLocation Location { get; set; }

        public string RegionName { get; set; }

        public IParsedSyntax Target { get; set; }

        public IParsedSyntax CheckNames(INameRecorder names) {
            // Make sure that our region exists
            if (this.RegionName == "stack") {
                this.region = IdentifierPath.StackPath;
            }
            else {
                var segments = names.CurrentRegion.Segments;
                if (!segments.Contains(this.RegionName)) {
                    throw TypeCheckingErrors.RegionUndefined(this.Location, this.RegionName);
                }

                this.region = new IdentifierPath(segments.TakeWhile(x => x != this.RegionName)).Append(this.RegionName);
            }

            // Push our region
            names.PushRegion(this.region);

            this.Target = this.Target.CheckNames(names);

            names.PopRegion();

            return this;
        }

        public ITypeCheckedSyntax CheckTypes(INameRecorder names, ITypeRecorder types) {
            names.PushRegion(this.region);

            var target = this.Target.CheckTypes(names, types);

            names.PopRegion();

            return target;
        }
    }
}
