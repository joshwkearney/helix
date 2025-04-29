using Helix.FlowAnalysis;

namespace Helix.Syntax.IR;

public record StoreReferenceOp : IOp {
    public required Immediate Value { get; init; }

    public required Immediate Reference { get; init; }
    
    public override string ToString() {
        return IOp.FormatOp("ref_store", $"{this.Value} -> [ {this.Reference} ]");
    }
}