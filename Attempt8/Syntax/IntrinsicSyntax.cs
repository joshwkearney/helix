using Attempt12.TypeSystem;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt12.Analyzing {
    public class IntrinsicDescriptor {
        public static IntrinsicDescriptor AddInt32 { get; } = new IntrinsicDescriptor(PrimitiveTypes.Int32Type, PrimitiveTypes.Int32Type, PrimitiveTypes.Int32Type);
        public static IntrinsicDescriptor SubtractInt32 { get; } = new IntrinsicDescriptor(PrimitiveTypes.Int32Type, PrimitiveTypes.Int32Type, PrimitiveTypes.Int32Type);
        public static IntrinsicDescriptor MultiplyInt32 { get; } = new IntrinsicDescriptor(PrimitiveTypes.Int32Type, PrimitiveTypes.Int32Type, PrimitiveTypes.Int32Type);
        public static IntrinsicDescriptor DivideInt32 { get; } = new IntrinsicDescriptor(PrimitiveTypes.Int32Type, PrimitiveTypes.Int32Type, PrimitiveTypes.Int32Type);

        public static IntrinsicDescriptor AddReal32 { get; } = new IntrinsicDescriptor(PrimitiveTypes.Float32Type, PrimitiveTypes.Float32Type, PrimitiveTypes.Float32Type);
        public static IntrinsicDescriptor SubtractReal32 { get; } = new IntrinsicDescriptor(PrimitiveTypes.Float32Type, PrimitiveTypes.Float32Type, PrimitiveTypes.Float32Type);
        public static IntrinsicDescriptor MultiplyReal32 { get; } = new IntrinsicDescriptor(PrimitiveTypes.Float32Type, PrimitiveTypes.Float32Type, PrimitiveTypes.Float32Type);
        public static IntrinsicDescriptor DivideReal32 { get; } = new IntrinsicDescriptor(PrimitiveTypes.Float32Type, PrimitiveTypes.Float32Type, PrimitiveTypes.Float32Type);

        public ImmutableList<ISymbol> ParameterTypes { get; }

        public ISymbol ReturnType { get; }

        private IntrinsicDescriptor(ISymbol returnType, params ISymbol[] paramTypes) {
            this.ReturnType = returnType;
            this.ParameterTypes = paramTypes.ToImmutableList();
        }
    }

    public class IntrinsicSyntax : ISyntax {
        public AnalyticScope Scope { get; }

        public IntrinsicDescriptor IntrinsicKind { get; }

        public ImmutableList<ISyntax> IntrinsicArguments { get; }

        public ISymbol TypeSymbol => this.IntrinsicKind.ReturnType;

        public IntrinsicSyntax(IntrinsicDescriptor kind, IEnumerable<ISyntax> args, AnalyticScope scope) {
            this.IntrinsicKind = kind;
            this.IntrinsicArguments = args.ToImmutableList();
            this.Scope = scope;

            if (this.IntrinsicKind.ParameterTypes.Count != this.IntrinsicArguments.Count) {
                throw new Exception("Intrinsic call has the incorrect number of arguments");
            }

            foreach (var pair in args.Select(x => x.TypeSymbol).Zip(kind.ParameterTypes, (x, y) => new { ArgType = x, ParamType = y })) {
                if (pair.ArgType != pair.ParamType) {
                    throw new Exception("Intrinsic call must have correctly-matching argument types");
                }
            }
        }

        public void Accept(ISyntaxVisitor visitor) {
            visitor.Visit(this);
        }
    }
}