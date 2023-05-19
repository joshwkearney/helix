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
        Alias, Root
    }

    public abstract record Lifetime {
        public static Lifetime Heap { get; } = new ValueLifetime(
            new IdentifierPath("$heap").ToVariablePath(),
            LifetimeRole.Root,
            0);

        public static Lifetime None { get; } = new ValueLifetime(
            new IdentifierPath("$none").ToVariablePath(),
            LifetimeRole.Root,
            0);

        public abstract VariablePath Path { get; }

        public abstract LifetimeRole Kind { get; }

        public abstract int Version { get; }

        public abstract ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer);
    }

    public record StackLocationLifetime : Lifetime {
        public override LifetimeRole Kind => LifetimeRole.Root;

        public override int Version => 0;

        public override VariablePath Path { get; }

        public StackLocationLifetime(VariablePath varPath) {
            this.Path = varPath;
        }

        public override ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer) {
            return new CVariableLiteral("_region_min()");
        }
    }

    public record InferredLocationLifetime : Lifetime {
        private TokenLocation Location { get; }

        private ValueSet<Lifetime> AllowedRoots { get; }

        public override VariablePath Path { get; }

        public override LifetimeRole Kind => LifetimeRole.Alias;

        public override int Version => 0;

        public InferredLocationLifetime(TokenLocation loc, VariablePath varPath, IEnumerable<Lifetime> allowedRoots) {
            this.Location = loc;
            this.Path = varPath;
            this.AllowedRoots = allowedRoots.ToValueSet();
        }

        public override ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer) {
            var targetName = writer.GetVariableName(this.Path.Variable);
            var roots = flow.GetRoots(this).ToValueSet();

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
    }

    public record ValueLifetime : Lifetime {
        public override VariablePath Path { get; }

        public override LifetimeRole Kind { get; }

        public override int Version { get; }

        public ValueLifetime(VariablePath varPath, LifetimeRole role, int version) {
            this.Path = varPath;
            this.Kind = role;
            this.Version = version;
        }

        public override ICSyntax GenerateCode(FlowFrame flow, ICStatementWriter writer) {
            var targetName = writer.GetVariableName(this.Path.Variable);

            if (this == Heap) {
                targetName = "_return_region";
            }
            else if (this == None) {
                targetName = "_region_min()";
            }

            return new CMemberAccess() {
                IsPointerAccess = false,
                Target = new CVariableLiteral(targetName),
                MemberName = "region"
            };
        }
    }
}
