using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Attempt18;
using Attempt18.CodeGeneration;
using Attempt18.Parsing;
using Attempt18.Types;

namespace Attempt17.NewSyntax {
    public class Syntax {
        public SyntaxData Data { get; set; }

        public SyntaxOp Operator { get; set; }

        public Syntax DeclareNames(IdentifierPath scope, NameCache names) {
            if (!this.Operator.AsNameDeclatator().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            if (!this.Data.AsParsedData().TryGetValue(out var data)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(data, scope, names);
        }

        public Syntax ResolveNames(NameCache names) {
            if (!this.Operator.AsNameResolver().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            if (!this.Data.AsParsedData().TryGetValue(out var data)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(data, names);
        }

        public Syntax DeclareTypes(TypeCache types) {
            if (!this.Operator.AsTypeResolver().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            if (!this.Data.AsParsedData().TryGetValue(out var data)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(data, types);
        }

        public Syntax ResolveTypes(TypeCache types) {
            if (!this.Operator.AsTypeResolver().TryGetValue(out var op)) {
                throw new InvalidOperationException();
            }

            if (!this.Data.AsParsedData().TryGetValue(out var data)) {
                throw new InvalidOperationException();
            }

            return op.Invoke(data, types);
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