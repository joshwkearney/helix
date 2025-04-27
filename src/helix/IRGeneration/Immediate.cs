namespace Helix.IRGeneration;

public abstract record Immediate {
    public record Name(string Value) : Immediate {
        public override string ToString() {
            return this.Value;
        }
    }

    public record Void : Immediate {
        public override string ToString() {
            return "void";
        }
    }

    public record Word(long Value) : Immediate {
        public override string ToString() {
            return this.Value.ToString();
        }
    }

    public record Bool(bool Value) : Immediate {
        public override string ToString() {
            return this.Value ? "true" : "false";
        }
    }
}

