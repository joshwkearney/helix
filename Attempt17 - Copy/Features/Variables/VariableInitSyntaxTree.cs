using Attempt17.CodeGeneration;
using Attempt17.Types;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt17.Features.Variables {
    public class VariableInitSyntaxTree : ISyntaxTree {
        public LanguageType ReturnType { get; }

        public string Name { get; }

        public ISyntaxTree Value { get; }

        public ImmutableHashSet<IdentifierPath> CapturedVariables => ImmutableHashSet<IdentifierPath>.Empty;

        public VariableInitKind Kind { get; }

        public VariableInitSyntaxTree(VariableInitKind kind, string name, ISyntaxTree value) {
            this.Kind = kind;
            this.ReturnType = VoidType.Instance;
            this.Name = name;
            this.Value = value;
        }

        public CBlock GenerateCode(CodeGenerator gen) {
            var value = this.Value.GenerateCode(gen);
            var type = this.Value.ReturnType.GenerateCType();

            var writer = new CWriter();
            writer.Lines(value.SourceLines);
            writer.VariableInit(type, this.Name, value.Value);

            return writer.ToBlock("0");
        }

        public Scope ModifyLateralScope(Scope scope) {
            LanguageType type;
            VariableSource source;

            if (this.Kind == VariableInitKind.Store) {
                type = this.Value.ReturnType;
                source = VariableSource.Local;
            }
            else if (this.Kind == VariableInitKind.Equate) {
                type = ((VariableType)this.Value.ReturnType).InnerType;
                source = VariableSource.Alias;
            }
            else {
                throw new Exception("This should never happen");
            }

            var path = scope.Path.Append(this.Name);
            var info = new VariableInfo(type, source, path);

            return scope.AppendVariable(this.Name, info);
        }
    }
}