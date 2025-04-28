using Helix.Analysis.Types;
using Helix.IRGeneration;

namespace Helix.Parsing.IR;

public record AllocateArrayOp : IOp {
    public required Immediate ReturnValue { get; init; }
    
    public required HelixType InnerType { get; init; }
    
    public required Immediate Length { get; init; }

    public override string ToString() {
        return IOp.FormatOp("array_malloc", $"let {this.ReturnValue} = malloc({this.Length} * &{this.InnerType})");
    }
}