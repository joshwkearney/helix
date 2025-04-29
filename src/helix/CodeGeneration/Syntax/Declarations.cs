using System.Text;

namespace Helix.CodeGeneration.Syntax;

public readonly record struct CParameter() {
    public ICSyntax Type { get; init; } = null;

    public string Name { get; init; } = null;
}

public record CFunctionDeclaration() : ICStatement {
    private readonly Option<IReadOnlyList<ICStatement>> body = Option.None;

    public ICSyntax ReturnType { get; init; } = new CNamedType("void");

    public string Name { get; init; } = null;

    public bool IsStatic { get; init; } = false;

    public IReadOnlyList<CParameter> Parameters { get; init; } = Array.Empty<CParameter>();

    public IReadOnlyList<ICStatement> Body {
        init {
            this.body = Option.Some(value);
        }
    }

    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);

        if (this.IsStatic) {
            sb.Append("static ");
        }

        sb.Append(this.ReturnType.WriteToC()).Append(' ');
        sb.Append(this.Name).Append('(');

        if (this.Parameters.Any()) {
            sb.Append(this.Parameters[0].Type!.WriteToC());
            sb.Append(' ').Append(this.Parameters[0].Name);

            foreach (var par in this.Parameters.Skip(1)) {
                sb.Append(", ").Append(par.Type!.WriteToC());
                sb.Append(' ').Append(par.Name);
            }
        }

        sb.Append(')');

        if (this.body.TryGetValue(out var stats)) {
            sb.AppendLine(" {");

            foreach (var stat in stats) {
                stat.WriteToC(indentLevel + 1, sb);
            }

            CHelper.Indent(indentLevel, sb);
            sb.AppendLine("}");
        }
        else {
            sb.AppendLine(";");
        }
    }
}

public record CAggregateDeclaration() : ICStatement {
    private readonly Option<IReadOnlyList<CParameter>> members = Option.None;

    public bool IsUnion { get; init; } = false;

    public string Name { get; init; } = null;

    public IReadOnlyList<CParameter> Members {
        init {
            this.members = Option.Some(value);
        }
    }

    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);
        var type = this.IsUnion ? "union" : "struct";

        if (this.members.TryGetValue(out var mems)) {
            sb.Append(type).Append(' ').Append(this.Name).AppendLine(" {");

            foreach (var mem in mems) {
                CHelper.Indent(indentLevel + 1, sb);
                sb.Append(mem.Type!.WriteToC()).Append(' ').Append(mem.Name).AppendLine(";");
            }

            if (!mems.Any()) {
                CHelper.Indent(indentLevel + 1, sb);
                sb.AppendLine("int dummy;");
            }

            CHelper.Indent(indentLevel, sb);
            sb.AppendLine("};");
        }
        else {
            sb.Append("typedef ").Append(type).Append(' ');
            sb.Append(this.Name).Append(' ').Append(this.Name).AppendLine(";");
        }
    }
}

public record CFunctionPointerDeclaration() : ICStatement {
    public ICSyntax ReturnType { get; init; } = new CNamedType("void");

    public string Name { get; init; } = null;

    public IReadOnlyList<CParameter> Parameters { get; init; } = Array.Empty<CParameter>();

    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);

        sb.Append("typedef ").Append(this.ReturnType!.WriteToC());
        sb.Append(" (*").Append(this.Name!).Append(")(");
        sb.Append(string.Join(", ", this.Parameters.Select(x => x.Type + " " + x.Name)));
        sb.AppendLine(");");
    }
}