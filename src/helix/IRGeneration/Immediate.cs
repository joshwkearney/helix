using Helix.Analysis.Types;

namespace Helix.IRGeneration;

public abstract record Immediate {
    public record Name(string Value) : Immediate;

    public record Void : Immediate {
    }

    public record Word(long Value) : Immediate {
    }

    public record Bool(bool Value) : Immediate {
    }
}

