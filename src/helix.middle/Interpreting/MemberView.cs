using Helix.Common.Types;

namespace Helix.MiddleEnd.Interpreting {
    internal class MemberView {
        public IHelixType Type { get; }

        public IReadOnlyList<string> MemberChain { get; }

        public MemberView(IHelixType type, IReadOnlyList<string> mems) {
            this.MemberChain = mems;
            this.Type = type;
        }

        public IValueLocation CreateLocation(IValueLocation targetLocation) {
            foreach (var mem in this.MemberChain) {
                targetLocation = new MemberAccessLocation() {
                    Parent = targetLocation,
                    Member = mem
                };
            }

            return targetLocation;
        }

        public IValueLocation CreateLocation(string targetName) => this.CreateLocation(new NamedLocation(targetName));
    }
}
