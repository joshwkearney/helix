using System.Text;
using System.Text.RegularExpressions;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Aggregates;
using Helix.Generation.CSyntax;
using Helix.Generation.Syntax;

namespace Helix.Generation {
    public interface ICWriter {
        public string GetVariableName();

        public string GetVariableName(IdentifierPath path);

        public void ResetTempNames();

        public void WriteDeclaration1(ICStatement decl);

        public void WriteDeclaration2(ICStatement decl);

        public void WriteDeclaration3(ICStatement decl);

        public void WriteDeclaration4(ICStatement decl);

        public ICSyntax ConvertType(HelixType type);
    }

    public class CWriter : ICWriter {
        private char tempLetterCounter = 'A';
        private int tempNumberCounter = 0;

        private readonly string header;
        private readonly IDictionary<HelixType, DeclarationCG> typeDeclarations;
        private readonly Dictionary<IdentifierPath, string> pathNames = new();
        private readonly Dictionary<string, int> nameCounters = new();

        private int arrayCounter = 0;
        private readonly Dictionary<ArrayType, string> arrayNames = new();

        private readonly StringBuilder decl1Sb = new();
        private readonly StringBuilder decl2Sb = new();
        private readonly StringBuilder decl3Sb = new();
        private readonly StringBuilder decl4Sb = new();

        public CWriter(string header, IDictionary<HelixType, DeclarationCG> typeDecls) {
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

        public ICSyntax ConvertType(HelixType type) {
            if (type == PrimitiveType.Bool || type is SingularBoolType) {
                return new CNamedType("int");
            }
            else if (type == PrimitiveType.Int || type is SingularIntType) {
                return new CNamedType("int");
            }
            else if (type == PrimitiveType.Void) {
                return new CNamedType("int");
            }
            else if (type is PointerType type2) {
                //return new CPointerType(ConvertType(type2.InnerType));
                return ConvertType(new ArrayType(type2.InnerType));
            }
            else if (type is NamedType named) {
                if (this.pathNames.TryGetValue(named.Path, out var cname)) {
                    return new CNamedType(cname);
                }                   

                cname = this.pathNames[named.Path] = string.Join("$", named.Path.Segments);

                // Do this after in case we have a recursive struct (through a pointer)
                if (this.typeDeclarations.TryGetValue(type, out var cg)) {
                    cg(this);
                }

                return new CNamedType(cname);
            }
            else if (type is ArrayType array) {
                if (this.arrayNames.TryGetValue(array, out var name)) {
                    return new CNamedType(name);
                }

                name = this.arrayNames[array] = this.GenerateArrayType(array);
                return new CNamedType(name);
            }
            else {
                throw new Exception();
            }
        }

        private string GenerateArrayType(ArrayType arrayType) {
            var inner = this.ConvertType(arrayType.InnerType);
            var name = inner.WriteToC();

            if (Regex.Match(name, @"[a-zA-Z0-9_$]+").Length == 0) {
                name = this.arrayCounter.ToString();
                this.arrayCounter++;
            }

            name = name + "$ptr";

            var decl = new CAggregateDeclaration() {
                Kind = AggregateKind.Struct,
                Name = name,
                Members = new[] { 
                    new CParameter() {
                        Name = "data",
                        Type = new CPointerType(inner)
                    },
                    new CParameter() {
                        Name = "count",
                        Type = this.ConvertType(PrimitiveType.Int)
                    },
                    new CParameter() {
                        Name = "pool",
                        Type = this.ConvertType(PrimitiveType.Int)
                    }
                }
            };

            var forwardDecl = new CAggregateDeclaration() {
                Kind = AggregateKind.Struct,
                Name = name
            };

            this.WriteDeclaration1(forwardDecl);
            this.WriteDeclaration3(decl);
            this.WriteDeclaration3(new CEmptyLine());

            return name;
        }

        public override string ToString() {
            var sb = new StringBuilder();

            sb.AppendLine("#if __cplusplus");
            sb.AppendLine("extern \"C\" {");
            sb.AppendLine("#endif").AppendLine();

            sb.AppendLine(this.header);

            if (this.decl1Sb.Length > 0) {
                sb.Append(this.decl1Sb).AppendLine();
            }

            if (this.decl2Sb.Length > 0) {
                sb.Append(this.decl2Sb).AppendLine();
            }

            if (this.decl3Sb.Length > 0) {
                sb.Append(this.decl3Sb);
            }

            if (this.decl4Sb.Length > 0) {
                sb.Append(this.decl4Sb);
            }

            sb.AppendLine("#if __cplusplus");
            sb.AppendLine("}");
            sb.AppendLine("#endif");

            return sb.ToString();
        }
    }
}