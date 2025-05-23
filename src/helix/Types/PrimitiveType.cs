﻿using Helix.Parsing;
using Helix.Syntax.TypedTree;
using Helix.Syntax.TypedTree.Primitives;
using Helix.TypeChecking;

namespace Helix.Types;

public record PrimitiveType : HelixType {
    private readonly PrimitiveTypeKind kind;

    public static PrimitiveType Word { get; } = new PrimitiveType(PrimitiveTypeKind.Word);

    public static PrimitiveType Bool { get; } = new PrimitiveType(PrimitiveTypeKind.Bool);

    public static PrimitiveType Void { get; } = new PrimitiveType(PrimitiveTypeKind.Void);

    private PrimitiveType(PrimitiveTypeKind kind) {
        this.kind = kind;
    }

    public override PassingSemantics GetSemantics(TypeFrame types) {
        return PassingSemantics.ValueType;
    }

    public override bool IsWord(TypeFrame types) => this.kind == PrimitiveTypeKind.Word;
        
    public override bool IsBool(TypeFrame types) => this.kind == PrimitiveTypeKind.Bool;

    public override Option<ITypedExpression> ToSyntax(TokenLocation loc, TypeFrame types) {
        if (this == Void) {
            return new VoidLiteral {
                Location = loc
            };
        }

        return base.ToSyntax(loc, types);
    }

    public override string ToString() {
        return this.kind switch {
            PrimitiveTypeKind.Word  => "word",
            PrimitiveTypeKind.Bool  => "bool",
            PrimitiveTypeKind.Void  => "void",
            _                       => throw new Exception("Unexpected primitive type kind"),
        };
    }

    public override HelixType GetSupertype(TypeFrame types) => this;

    private enum PrimitiveTypeKind {
        Word = 11, 
        Bool = 17,
        Void = 19,
    }
}