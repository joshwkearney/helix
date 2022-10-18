using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.Syntax;

namespace Trophy.Generation {
    public interface ICWriter {
        public string GetVariableName();

        public string GetVariableName(IdentifierPath path);

        public void ResetTempNames();

        public void WriteDeclaration1(ICStatement decl);

        public void WriteDeclaration2(ICStatement decl);

        public void WriteDeclaration3(ICStatement decl);

        public ICSyntax ConvertType(TrophyType type);
    }

    public class CWriter : ICWriter {
        private char tempLetterCounter = 'A';
        private int tempNumberCounter = 0;

        private readonly Dictionary<TrophyType, ICSyntax> typeNames = new();
        private readonly Dictionary<IdentifierPath, string> pathNames = new();
        private readonly Dictionary<string, int> nameCounters = new();

        private readonly StringBuilder decl1Sb = new();
        private readonly StringBuilder decl2Sb = new();
        private readonly StringBuilder decl3Sb = new();

        public CWriter() {
            decl1Sb.AppendLine("void* memset(void* str, int c, long unsigned int n);");
            decl1Sb.AppendLine();
        }

        public string GetVariableName() {
            if (this.tempLetterCounter > 'Z') {
                this.tempLetterCounter = 'A';
                this.tempNumberCounter++;
            }

            if (this.tempNumberCounter > 0) {
                return "$" + this.tempLetterCounter++ + "_" + this.tempNumberCounter;
            }
            else {
                return "$" + this.tempLetterCounter++;
            }
        }

        public void ResetTempNames() {
            this.tempLetterCounter = 'A';
            this.tempNumberCounter = 0;
        }

        public string GetVariableName(IdentifierPath path) {
            if (path.Segments.Count == 1) {
                return path.Segments.First();
            }

            var name = path.Segments.Last();

            if (!this.nameCounters.TryGetValue(name, out int counter)) {
                counter = this.nameCounters[name] = 0;
            }

            if (!this.pathNames.TryGetValue(path, out var value)) {
                value = name;

                if (counter > 0) {
                    value += "_" + counter;
                }

                this.pathNames[path] = value;
                this.nameCounters[name]++;
            }

            return value;
        }

        public void WriteDeclaration1(ICStatement decl) {
            decl.WriteToC(0, this.decl1Sb);
        }

        public void WriteDeclaration2(ICStatement decl) {
            decl.WriteToC(0, this.decl2Sb);
        }

        public void WriteDeclaration3(ICStatement decl) {
            decl.WriteToC(0, this.decl3Sb);
        }

        public ICSyntax ConvertType(TrophyType type) {
            if (this.typeNames.TryGetValue(type, out var ctype)) {
                return ctype;
            }

            if (type == PrimitiveType.Bool || type is SingularBoolType) {
                return new CNamedType("unsigned int");
            }
            else if (type == PrimitiveType.Int || type is SingularIntType) {
                return new CNamedType("unsigned int");
            }
            else if (type == PrimitiveType.Void) {
                return new CNamedType("unsigned int");
            }
            else if (type is PointerType type2) {
                return new CPointerType(ConvertType(type2.ReferencedType));
            }
            else if (type is NamedType type3) {
                return new CNamedType(string.Join("$", type3.Path.Segments));
            }
            else {
                throw new Exception();
            }
        }

        public override string ToString() {
            return new StringBuilder()
                .Append(this.decl1Sb)
                .Append(this.decl2Sb)
                .Append(this.decl3Sb)
                .ToString();
        }
    }
}