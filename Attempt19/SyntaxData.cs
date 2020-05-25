using System.Collections.Immutable;
using Attempt19.Parsing;
using Attempt19.Types;

namespace Attempt19 {
    public interface IParsedData {
        public TokenLocation Location { get; }
    }

    public interface ITypeCheckedData : IParsedData {
        public LanguageType ReturnType { get; }

        public ImmutableHashSet<VariableCapture> EscapingVariables { get; }
    }

    public interface IFlownData : ITypeCheckedData {
    }

    public class SyntaxData {
        public static SyntaxData From(IParsedData data) {
            return new SyntaxData(data);
        }

        public static SyntaxData From(ITypeCheckedData data) {
            return new SyntaxData(data);
        }

        public static SyntaxData From(IFlownData data) {
            return new SyntaxData(data);
        }

        private readonly object op;

        private SyntaxData(object op) {
            this.op = op;
        }

        public IParsedData AsParsedData() {
            return Option.Some(this.op as IParsedData).Where(x => x != null).GetValue();
        }

        public IOption<ITypeCheckedData> AsTypeCheckedData() {
            return Option.Some(this.op as ITypeCheckedData).Where(x => x != null);
        }

        public IOption<IFlownData> AsFlownData() {
            return Option.Some(this.op as IFlownData).Where(x => x != null);
        }
    }
}