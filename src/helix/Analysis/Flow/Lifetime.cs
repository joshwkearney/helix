using Helix.Analysis.Flow;
using Helix.Collections;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Analysis.Flow {
    public enum LifetimeRole {
        /// <summary>
        /// An alias is a lifetime that is either inferred or can be computed from
        /// previous lifetimes. Aliases do not represent new regions but rather
        /// interpolations from previously known regions. Common examples are 
        /// variable locations and temporary lifetimes
        /// </summary>
        Alias,

        /// <summary>
        /// Roots refer to lifetimes that are in a sense external to the current
        /// context and represent different regions. Copying a pointer from one
        /// root to another is not memory safe because regions are freed at
        /// different times. Examples are the return region and pointer
        /// dereference regions.
        /// </summary>
        Root
    }

    public enum LifetimeOrigin {
        LocalValue, LocalLocation, TempValue, Other
    }

    public abstract record Lifetime {
        public static Lifetime Heap { get; } = new ValueLifetime(
            new IdentifierPath("$heap").ToVariablePath(),
            LifetimeRole.Root,
            LifetimeOrigin.Other,
            0);

        public static Lifetime None { get; } = new ValueLifetime(
            new IdentifierPath("$none").ToVariablePath(),
            LifetimeRole.Root,
            LifetimeOrigin.Other,
            0);

        public abstract VariablePath Path { get; }

        public abstract LifetimeRole Role { get; }

        public abstract int Version { get; }

        public abstract LifetimeOrigin Origin { get; }

        public abstract Lifetime IncrementVersion();

        public abstract ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer);
    }

    public record StackLocationLifetime : Lifetime {
        public override LifetimeRole Role => LifetimeRole.Root;

        public override int Version { get; }

        public override VariablePath Path { get; }

        public override LifetimeOrigin Origin { get; }

        public StackLocationLifetime(VariablePath varPath, LifetimeOrigin origin, int version = 0) {
            this.Path = varPath;
            this.Version = version;
            this.Origin = origin;
        }

        public override ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer) {
            // TODO: Put back _region_min
            return new CVariableLiteral("_return_region");
        }

        public override Lifetime IncrementVersion() {
            return new StackLocationLifetime(this.Path, this.Origin, this.Version + 1);
        }
    }

    public record InferredLocationLifetime : Lifetime {
        private TokenLocation Location { get; }

        private ValueSet<Lifetime> AllowedRoots { get; }

        public override VariablePath Path { get; }

        public override LifetimeRole Role => LifetimeRole.Alias;

        public override int Version { get; }

        public override LifetimeOrigin Origin { get; }

        public InferredLocationLifetime(TokenLocation loc, VariablePath varPath, 
                                        IEnumerable<Lifetime> allowedRoots, 
                                        LifetimeOrigin origin,
                                        int version = 0) {
            this.Location = loc;
            this.Path = varPath;
            this.AllowedRoots = allowedRoots.ToValueSet();
            this.Version = version;
            this.Origin = origin;
        }

        public override ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer) {
            var targetName = writer.GetVariableName(this.Path.Variable);
            var roots = flow.GetMaximumRoots(this).ToValueSet();

            if (roots.Any() && roots.Any(x => !this.AllowedRoots.Contains(x))) {
                throw new LifetimeException(
                    this.Location,
                    "Lifetime Inference Failed",
                    "The lifetime of this new object allocation has failed because it is " +
                    "dependent on a root that does not exist at this point in the program and " +
                    "must be calculated at runtime. Please try moving the allocation " +
                    "closer to the site of its use.");
            }

            return writer.CalculateSmallestLifetime(this.Location, roots, flow);
        }

        public override Lifetime IncrementVersion() {
            return new InferredLocationLifetime(this.Location, this.Path, this.AllowedRoots, 
                                                this.Origin, this.Version + 1);
        }
    }

    public record ValueLifetime : Lifetime {
        public override VariablePath Path { get; }

        public override LifetimeRole Role { get; }

        public override int Version { get; }

        public override LifetimeOrigin Origin { get; }

        public ValueLifetime(VariablePath varPath, LifetimeRole role, LifetimeOrigin origin, int version = 0) {
            this.Path = varPath;
            this.Role = role;
            this.Version = version;
            this.Origin = origin;
        }

        public override ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer) {
            if (this == Heap) {
                return new CVariableLiteral("_return_region");
            }
            else if (this == None) {
                // TODO: Put back _region_min
                return new CVariableLiteral("_return_region");
            }

            var targetName = writer.GetVariableName(this.Path.Variable);

            return new CMemberAccess() {
                IsPointerAccess = false,
                Target = new CVariableLiteral(targetName),
                MemberName = "region"
            };
        }

        public override Lifetime IncrementVersion() {
            return new ValueLifetime(this.Path, this.Role, this.Origin, this.Version + 1);
        }
    }
}
