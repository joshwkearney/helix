using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace Attempt17.Features.Variables {
    public class VariableLiteralSyntaxTree : ISyntaxTree {
        public LanguageType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> CapturedVariables { get; }

        public VariableLiteralKind Kind { get; }

        public VariableInfo VarInfo { get; }

        public VariableLiteralSyntaxTree(VariableInfo info, VariableLiteralKind kind, LanguageType returnType) {
            this.VarInfo = info;
            this.Kind = kind;
            this.ReturnType = returnType;

            // TODO - Make this more robust
            if (returnType == IntType.Instance || returnType == VoidType.Instance) { 
                this.CapturedVariables = ImmutableHashSet<IdentifierPath>.Empty;
            }
            else {
                this.CapturedVariables = new[] { info.Path }.ToImmutableHashSet();
            }
        }

        public CBlock GenerateCode(CodeGenerator gen) {
            var name = this.VarInfo.Path.Segments.Last();

            if (this.Kind == VariableLiteralKind.LiteralAccess) {
                if (this.VarInfo.Source == VariableSource.Alias) {
                    return new CBlock(name);
                }
                else if (this.VarInfo.Source == VariableSource.Local) {
                    return new CBlock(CWriter.AddressOf(name));
                }
            }
            else if (this.Kind == VariableLiteralKind.ValueAccess) {
                if (this.VarInfo.Source == VariableSource.Alias) {
                    return new CBlock(CWriter.Dereference(name));
                }
                else if (this.VarInfo.Source == VariableSource.Local) {
                    return new CBlock(name);
                }
            }

            throw new Exception("This should never happen");
        }

        public Scope ModifyLateralScope(Scope scope) => scope;
    }
}