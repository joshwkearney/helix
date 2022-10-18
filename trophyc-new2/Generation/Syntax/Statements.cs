using System.Text;

namespace Trophy.Generation.Syntax {
    public interface ICStatement {
        public bool IsEmpty => false;

        public void WriteToC(int indentLevel, StringBuilder sb);
    }

    public record CEmptyLine : ICStatement {
        public bool IsEmpty => true;

        public void WriteToC(int indentLevel, StringBuilder sb) {
            sb.AppendLine();
        }
    }

    public record CSyntaxStatement : ICStatement {
        public ICSyntax? Value { get; init; } = null;

        public void WriteToC(int indentLevel, StringBuilder sb) {
            CHelper.Indent(indentLevel, sb);
            sb.Append(this.Value!.WriteToC()).AppendLine(";");
        }
    }

    public record CVariableDeclaration() : ICStatement {
        private readonly Option<ICSyntax> assign = Option.None;

        public ICSyntax? Type { get; init; } = null;

        public string? Name { get; init; } = null;

        public ICSyntax Assignment {
            init => this.assign = Option.Some(value);
        }

        public void WriteToC(int indentLevel, StringBuilder sb) {
            CHelper.Indent(indentLevel, sb);
            sb.Append(this.Type!.WriteToC()).Append(' ').Append(this.Name);

            if (this.assign.TryGetValue(out var assign)) {
                sb.Append(" = ").Append(assign.WriteToC());
            }

            sb.AppendLine(";");
        }
    }

    public record CReturn() : ICStatement {
        private readonly Option<ICSyntax> target = Option.None;

        public ICSyntax Target {
            init => this.target = Option.Some(value);
        }

        public void WriteToC(int indentLevel, StringBuilder sb) {
            CHelper.Indent(indentLevel, sb);

            if (this.target.TryGetValue(out var value)) {
                sb.Append("return ").Append(value.WriteToC()).AppendLine(";");
            }
            else {
                sb.AppendLine("return;");
            }
        }
    }

    public record CIf() : ICStatement {
        public ICSyntax? Condition { get; init; } = null;

        public IEnumerable<ICStatement> IfTrue { get; init; } = Array.Empty<ICStatement>();

        public IEnumerable<ICStatement> IfFalse { get; init; } = Array.Empty<ICStatement>();

        public void WriteToC(int indentLevel, StringBuilder sb) {
            CHelper.Indent(indentLevel, sb);
            sb.Append("if (").Append(this.Condition!.WriteToC()).Append(") { ");

            if (this.IfTrue.Any()) {
                sb.AppendLine();

                foreach (var stat in this.IfTrue) {
                    stat.WriteToC(indentLevel + 1, sb);
                }
            }

            CHelper.Indent(indentLevel, sb);
            sb.AppendLine("} ");

            if (this.IfFalse.Any()) {
                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("else {");

                foreach (var stat in this.IfFalse) {
                    stat.WriteToC(indentLevel + 1, sb);
                }

                CHelper.Indent(indentLevel, sb);
                sb.AppendLine("}");
            }
        }
    }

    public record CAssignment() : ICStatement {
        public ICSyntax? Left { get; init; } = null;

        public ICSyntax? Right { get; init; } = null;

        public void WriteToC(int indentLevel, StringBuilder sb) {
            CHelper.Indent(indentLevel, sb);

            sb.Append(this.Left!.WriteToC());
            sb.Append(" = ");
            sb.Append(this.Right!.WriteToC());
            sb.AppendLine(";");
        }
    }

    public record CWhile() : ICStatement {
        public ICSyntax? Condition { get; init; } = null;

        public IEnumerable<ICStatement> Body { get; init; } = Array.Empty<ICStatement>();

        public void WriteToC(int indentLevel, StringBuilder sb) {
            CHelper.Indent(indentLevel, sb);
            sb.Append("while (").Append(this.Condition!.WriteToC()).Append(") {");

            if (this.Body.Any()) {
                sb.AppendLine();
            }

            foreach (var stat in this.Body) {
                stat.WriteToC(indentLevel + 1, sb);
            }

            CHelper.Indent(indentLevel, sb);
            sb.AppendLine("}");
        }
    }

    public record CBreak() : ICStatement {
        public void WriteToC(int indentLevel, StringBuilder sb) {
            CHelper.Indent(indentLevel, sb);
            sb.AppendLine("break;");
        }
    }

    public record CComment(string Value) : ICStatement {
        public void WriteToC(int indentLevel, StringBuilder sb) {
            CHelper.Indent(indentLevel, sb);
            sb.Append("/* ").Append(this.Value).AppendLine(" */");
        }
    }
}
