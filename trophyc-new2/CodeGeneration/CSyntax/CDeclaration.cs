using System.Text;

namespace Trophy.CodeGeneration.CSyntax {
    public class CParameter {
        public string Name { get; }

        public CType Type { get; }

        public CParameter(CType type, string name) {
            this.Type = type;
            this.Name = name;
        }
    }

    public abstract class CDeclaration {
        public static CDeclaration Function(CType returnType, string name, bool isStatic, IReadOnlyList<CParameter> pars, IReadOnlyList<CStatement> stats) {
            return new CFunctionDeclaration(returnType, name, isStatic, pars, Option.Some(stats));
        }

        public static CDeclaration Function(string name, bool isStatic, IReadOnlyList<CParameter> pars, IReadOnlyList<CStatement> stats) {
            return new CFunctionDeclaration(name, isStatic, pars, Option.Some(stats));
        }

        public static CDeclaration FunctionPrototype(CType returnType, string name, bool isStatic, IReadOnlyList<CParameter> pars) {
            return new CFunctionDeclaration(returnType, name, isStatic, pars, Option.None);
        }

        public static CDeclaration FunctionPrototype(string name, bool isStatic, IReadOnlyList<CParameter> pars) {
            return new CFunctionDeclaration(name, isStatic, pars, Option.None);
        }

        public static CDeclaration Struct(string name, IReadOnlyList<CParameter> members) {
            return new CAggregateDeclaration(false, name, Option.Some(members));
        }

        public static CDeclaration StructPrototype(string name) {
            return new CAggregateDeclaration(false, name, Option.None);
        }

        public static CDeclaration Union(string name, IReadOnlyList<CParameter> members) {
            return new CAggregateDeclaration(true, name, Option.Some(members));
        }

        public static CDeclaration UnionPrototype(string name) {
            return new CAggregateDeclaration(true, name, Option.None);
        }

        public static CDeclaration FunctionPointer(string name, CType returnType, IReadOnlyList<CParameter> pars) {
            return new CFunctionPointerDeclaration(name, returnType, pars);
        }

        public static CDeclaration FunctionPointer(string name, IReadOnlyList<CParameter> pars) {
            return new CFunctionPointerDeclaration(name, CType.NamedType("void"), pars);
        }

        public static CDeclaration EmptyLine() {
            return new CEmptyLine();
        }

        private CDeclaration() { }

        public abstract void Write(int indentLevel, StringBuilder sb);

        private class CEmptyLine : CDeclaration {
            public override void Write(int indentLevel, StringBuilder sb) {
                sb.AppendLine();
            }
        }

        private class CFunctionPointerDeclaration : CDeclaration {
            private readonly IReadOnlyList<CParameter> pars;
            private readonly CType returnType;
            private readonly string name;

            public CFunctionPointerDeclaration(string name, CType returnType, IReadOnlyList<CParameter> pars) {
                this.name = name;
                this.returnType = returnType;
                this.pars = pars;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);

                sb.Append("typedef ").Append(this.returnType).Append(" (*").Append(this.name).Append(")(");
                sb.Append(string.Join(", ", this.pars.Select(x => x.Type + " " + x.Name)));
                sb.AppendLine(");");
            }
        }

        private class CAggregateDeclaration : CDeclaration {
            private readonly string Name;
            private readonly Option<IReadOnlyList<CParameter>> members;
            private readonly bool isUnion;

            public CAggregateDeclaration(bool isUnion, string name, Option<IReadOnlyList<CParameter>> members) {
                this.Name = name;
                this.members = members;
                this.isUnion = isUnion;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);
                var type = this.isUnion ? "union" : "struct";

                if (this.members.TryGetValue(out var mems)) {
                    sb.Append(type).Append(" ").Append(this.Name).AppendLine(" {");

                    foreach (var mem in mems) {
                        CHelper.Indent(indentLevel + 1, sb);
                        sb.Append(mem.Type.ToString()).Append(" ").Append(mem.Name).AppendLine(";");
                    }

                    if (!mems.Any()) {
                        CHelper.Indent(indentLevel + 1, sb);
                        sb.AppendLine("int dummy;");
                    }

                    CHelper.Indent(indentLevel, sb);
                    sb.AppendLine("};");
                }
                else {
                    sb.Append("typedef ").Append(type).Append(" ").Append(this.Name).Append(" ").Append(this.Name).AppendLine(";");
                }
            }
        }

        private class CFunctionDeclaration : CDeclaration {
            private readonly Option<CType> ReturnType;
            private readonly string name;
            private readonly IReadOnlyList<CParameter> pars;
            private readonly Option<IReadOnlyList<CStatement>> stats;
            private readonly bool isStatic;

            public CFunctionDeclaration(CType returnType, string name, bool isStatic, IReadOnlyList<CParameter> pars, Option<IReadOnlyList<CStatement>> stats) {
                this.ReturnType = Option.Some(returnType);
                this.name = name;
                this.pars = pars;
                this.stats = stats;
                this.isStatic = isStatic;
            }

            public CFunctionDeclaration(string name, bool isStatic, IReadOnlyList<CParameter> pars, Option<IReadOnlyList<CStatement>> stats) {
                this.ReturnType = Option.None;
                this.name = name;
                this.pars = pars;
                this.stats = stats;
                this.isStatic = isStatic;
            }

            public override void Write(int indentLevel, StringBuilder sb) {
                CHelper.Indent(indentLevel, sb);

                if (this.isStatic) {
                    sb.Append("static ");
                }

                if (this.ReturnType.TryGetValue(out var type)) {
                    sb.Append(type);
                }
                else {
                    sb.Append("void");
                }

                sb.Append(' ').Append(this.name).Append('(');

                if (this.pars.Any()) {
                    sb.Append(this.pars[0].Type).Append(' ').Append(this.pars[0].Name);

                    foreach (var par in this.pars.Skip(1)) {
                        sb.Append(", ").Append(par.Type).Append(' ').Append(par.Name);
                    }
                }

                sb.Append(')');

                if (this.stats.TryGetValue(out var stats)) {
                    if (stats.Any()) {
                        sb.AppendLine(" {");
                    }

                    foreach (var stat in stats) {
                        stat.Write(indentLevel + 1, sb);
                    }

                    if (stats.Any()) {
                        CHelper.Indent(indentLevel, sb);
                    }

                    sb.AppendLine("}");
                }
                else {
                    sb.AppendLine(";");
                }                
            }
        }
    }
}
