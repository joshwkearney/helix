using System.Text;

namespace Helix.CodeGeneration.Syntax;

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
    public ICSyntax Value { get; init; } = null;

    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);
        sb.Append(this.Value!.WriteToC()).AppendLine(";");
    }
}

public record CLabel : ICStatement {
    public string Value { get; init; } = null;

    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);
        sb.Append(this.Value).AppendLine(": ;");
    }
}

public record CGoto : ICStatement {
    public string Value { get; init; } = null;

    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);
        sb.Append("goto ").Append(this.Value).AppendLine(";");
    }
}

public record CVariableDeclaration() : ICStatement {
    public Option<ICSyntax> Assignment { get; init; } = Option.None;

    public ICSyntax Type { get; init; } = null;

    public string Name { get; init; } = null;

    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);
        sb.Append(this.Type!.WriteToC()).Append(' ').Append(this.Name);

        if (this.Assignment.TryGetValue(out var assign)) {
            sb.Append(" = ").Append(assign.WriteToC());
        }

        sb.AppendLine(";");
    }
}

/// <summary>
/// This is required in addition to CVariableDeclaration because
/// array declarations require a very particular syntax to work in
/// both C and C++
/// </summary>
public record CArrayDeclaration() : ICStatement {
    public IReadOnlyList<ICSyntax> Elements { get; init; }

    public ICSyntax ElementType { get; init; } = null;

    public string Name { get; init; } = null;

    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);
        sb.Append(this.ElementType.WriteToC()).Append(' ').Append(this.Name).Append("[]");
        sb.Append(" = { ").AppendJoin(", ", this.Elements.Select(x => x.WriteToC())).AppendLine(" };");
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
    public ICSyntax Condition { get; init; } = null;

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
    public ICSyntax Left { get; init; } = null;

    public ICSyntax Right { get; init; } = null;

    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);

        sb.Append(this.Left!.WriteToC());
        sb.Append(" = ");
        sb.Append(this.Right!.WriteToC());
        sb.AppendLine(";");
    }
}

public record CWhile() : ICStatement {
    public ICSyntax Condition { get; init; } = null;

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

public record CContinue() : ICStatement {
    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);
        sb.AppendLine("continue;");
    }
}

public record CComment(string Value) : ICStatement {
    public void WriteToC(int indentLevel, StringBuilder sb) {
        CHelper.Indent(indentLevel, sb);
        sb.Append("/* ").Append(this.Value).AppendLine(" */");
    }
}