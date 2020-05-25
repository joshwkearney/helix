using System;
using Attempt19.CodeGeneration;
using Attempt19.TypeChecking;

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

        public Syntax ResolveTypes(TypeCache types, ITypeUnifier unifier) {
            if (!this.Operator.AsTypeResolver().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(this.Data.AsParsedData(), types, unifier);
        }

        public Syntax AnalyzeFlow(TypeCache types, FlowCache flows) {
            if (!this.Operator.AsFlowAnalyzer().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            if (!this.Data.AsTypeCheckedData().TryGetValue(out var data)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(data, types, flows);
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