using Helix.FlowAnalysis;

namespace Helix.Syntax.IR;

public record StoreArrayOp : IOp {
    public required Immediate Value { get; init; }

    public required Immediate Array { get; init; }
    
    public required Immediate Index { get; init; }
    
    public override string ToString() {
        return IOp.FormatOp("array_store", $"{this.Value} -> [ {this.Array}[{this.Index}] ]");
    }
}