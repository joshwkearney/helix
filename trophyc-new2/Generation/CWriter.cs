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

        public void WriteDeclaration4(ICStatement decl);

        public ICSyntax ConvertType(TrophyType type);
    }

    public class CWriter : ICWriter {
        private char tempLetterCounter = 'A';
        private int tempNumberCounter = 0;

        private readonly string header;
        private readonly IDictionary<TrophyType, DeclarationCG> typeDeclarations;
        private readonly Dictionary<IdentifierPath, string> pathNames = new();
        private readonly Dictionary<string, int> nameCounters = new();

        private readonly StringBuilder decl1Sb = new();
        private readonly StringBuilder decl2Sb = new();
        private readonly StringBuilder decl3Sb = new();
        private readonly StringBuilder decl4Sb = new();

        public CWriter(string header, IDictionary<TrophyType, DeclarationCG> typeDecls) {
            this.header = header;
            this.typeDeclarations = typeDecls;
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

        public void WriteDeclaration4(ICStatement decl) {
            decl.WriteToC(0, this.decl4Sb);
        }

        public ICSyntax ConvertType(TrophyType type) {
            if (type == PrimitiveType.Bool || type is SingularBoolType) {
                return new CNamedType("_trophy_bool");
            }
            else if (type == PrimitiveType.Int || type is SingularIntType) {
                return new CNamedType("_trophy_int");
            }
            else if (type == PrimitiveType.Void) {
                return new CNamedType("_trophy_void");
            }
            else if (type is PointerType type2) {
                return new CPointerType(ConvertType(type2.ReferencedType));
            }
            else if (type is NamedType named) {
                if (this.pathNames.TryGetValue(named.Path, out var cname)) {
                    return new CNamedType(cname);
                }
                    
                if (this.typeDeclarations.TryGetValue(type, out var cg)) {
                    cg(this);
                }

                this.pathNames[named.Path] = string.Join("$", named.Path.Segments);

                return new CNamedType(this.pathNames[named.Path]);
            }
            else {
                throw new Exception();
            }
        }

        public override string ToString() {
            var sb = new StringBuilder();

            sb.AppendLine(this.header);

            if (this.decl1Sb.Length > 0) {
                sb.Append(this.decl1Sb).AppendLine();
            }

            if (this.decl2Sb.Length > 0) {
                sb.Append(this.decl2Sb).AppendLine();
            }

            if (this.decl3Sb.Length > 0) {
                sb.Append(this.decl3Sb).AppendLine();
            }

            if (this.decl4Sb.Length > 0) {
                sb.Append(this.decl4Sb).AppendLine();
            }

            return sb.ToString();
        }
    }
}