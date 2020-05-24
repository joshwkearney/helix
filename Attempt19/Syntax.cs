using System;
using Attempt19.CodeGeneration;

namespace Attempt19 {
    public class Syntax {
        public SyntaxData Data { get; set; }

        public SyntaxOp Operator { get; set; }

        public Syntax DeclareNames(IdentifierPath scope, NameCache names) {
            if (!this.Operator.AsNameDeclatator().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(this.Data.AsParsedData(), scope, names);
        }

        public Syntax ResolveNames(NameCache names) {
            if (!this.Operator.AsNameResolver().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(this.Data.AsParsedData(), names);
        }

        public Syntax DeclareTypes(TypeCache types) {
            if (!this.Operator.AsTypeDeclarator().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(this.Data.AsParsedData(), types);
        }

        public Syntax ResolveTypes(TypeCache types) {
            if (!this.Operator.AsTypeResolver().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(this.Data.AsParsedData(), types);
        }

        public Syntax AnalyzeFlow(FlowCache flows) {
            if (!this.Operator.AsFlowAnalyzer().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            if (!this.Data.AsTypeCheckedData().TryGetValue(out var data)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(data, flows);
        }

        public CBlock GenerateCode(ICScope scope, ICodeGenerator gen) {
            if (!this.Operator.AsCodeGenerator().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            if (!this.Data.AsFlownData().TryGetValue(out var data)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(data, scope, gen);
        }
    }
}