using System.Text;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.Generation.CSyntax;

namespace Trophy.Generation {
    public interface ICWriter {
        public string GetVariableName();

        public string GetVariableName(IdentifierPath path);

        public void WriteDeclaration1(CDeclaration decl);

        public void WriteDeclaration2(CDeclaration decl);

        public void WriteDeclaration3(CDeclaration decl);

        public CType ConvertType(TrophyType type);
    }

    public class CWriter : ICWriter {
        private int tempCounter = 0;
        private readonly Dictionary<TrophyType, CType> typeNames = new();
        private readonly Dictionary<IdentifierPath, string> tempNames = new();

        private readonly StringBuilder decl1Sb = new StringBuilder();
        private readonly StringBuilder decl2Sb = new StringBuilder();
        private readonly StringBuilder decl3Sb = new StringBuilder();

        public CWriter() {
            this.decl1Sb.AppendLine("#include \"include/trophy.h\"");
            this.decl1Sb.AppendLine();
        }

        public string GetVariableName() {
            return "$t_" + this.tempCounter++;
        }

        public string GetVariableName(IdentifierPath path) {
            if (path.Segments.Count == 1) {
                return path.Segments.First();
            }

            if (!this.tempNames.TryGetValue(path, out var value)) {
                value = this.tempNames[path] = path.Segments.Last() + "_" + this.tempCounter++;
            }

            return value;
        }

        public void WriteDeclaration1(CDeclaration decl) {
            decl.Write(0, this.decl1Sb);
        }

        public void WriteDeclaration2(CDeclaration decl) {
            decl.Write(0, this.decl2Sb);
        }

        public void WriteDeclaration3(CDeclaration decl) {
            decl.Write(0, this.decl3Sb);
        }

        public CType ConvertType(TrophyType type) {
            if (this.typeNames.TryGetValue(type, out var ctype)) {
                return ctype;
            }

            if (type == PrimitiveType.Bool) {
                return CType.NamedType("unsigned int");
            }
            else if (type == PrimitiveType.Int) {
                return CType.NamedType("unsigned int");
            }
            else if (type == PrimitiveType.Void) {
                return CType.NamedType("unsigned int");
            }
            else if (type.AsPointerType().TryGetValue(out var type2)) {
                return CType.Pointer(ConvertType(type2.ReferencedType));
            }
            else if (type.AsNamedType().TryGetValue(out var type3)) {
                return CType.NamedType(string.Join("$", type3.FullName.Segments));
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