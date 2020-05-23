using System.Collections.Immutable;
using Attempt18;
using Attempt18.Parsing;
using Attempt18.Types;

namespace Attempt17.NewSyntax {
    public interface IParsedData {
        public TokenLocation Location { get; }
    }

    public interface ITypeCheckedData : IParsedData {
        public LanguageType ReturnType { get; }
    }

    public interface IFlownData : ITypeCheckedData {
        public ImmutableHashSet<IdentifierPath> EscapingVariables { get; }
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

        public IOption<IParsedData> AsParsedData() {
            return Option.Some(this.op as IParsedData).Where(x => x != null);
        }

        public IOption<ITypeCheckedData> AsTypeCheckedData() {
            return Option.Some(this.op as ITypeCheckedData).Where(x => x != null);
        }

        public IOption<IFlownData> AsFlownData() {
            return Option.Some(this.op as IFlownData).Where(x => x != null);
        }
    }
}