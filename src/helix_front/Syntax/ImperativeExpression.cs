using Helix.Analysis;
using Helix.Analysis.Predicates;
using Helix.Analysis.TypeChecking;
using Helix.Analysis.Types;
using Helix.HelixMinusMinus;
using Helix.Parsing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Helix.Syntax {
    public record ImperativeExpression {
        private readonly object value;
        private readonly ValueType type;

        public static ImperativeExpression Void { get; } = new ImperativeExpression(PrimitiveType.Void, ValueType.VoidLiteral);
        public static ImperativeExpression Variable(string variableName) => new ImperativeExpression(variableName, ValueType.Name);
        public static ImperativeExpression Word(long value) => new ImperativeExpression(new SingularWordType(value), ValueType.WordLiteral);
        public static ImperativeExpression Bool(bool value) => new ImperativeExpression(new SingularBoolType(value), ValueType.BoolLiteral);
        public static ImperativeExpression Bool(bool value, ISyntaxPredicate pred) => new ImperativeExpression(new SingularBoolType(value, pred), ValueType.BoolLiteral);

        public bool IsVoid => type == ValueType.VoidLiteral;

        public TokenLocation Location { get; }

        private enum ValueType {
            Name,
            WordLiteral,
            BoolLiteral,
            VoidLiteral
        }

        private ImperativeExpression(object value, ValueType type) {
            this.value = value;
            this.type = type;
        }

        public Option<string> AsVariable() {
            if (type == ValueType.Name) {
                return (string)value;
            }
            else {
                return Option.None;
            }
        }

        public Option<int> AsWord() {
            if (type == ValueType.WordLiteral) {
                return (int)value;
            }
            else {
                return Option.None;
            }
        }

        public Option<bool> AsBool() {
            if (type == ValueType.BoolLiteral) {
                return (bool)value;
            }
            else {
                return Option.None;
            }
        }

        public override string ToString() {
            return type switch {
                ValueType.Name => (string)value,
                ValueType.BoolLiteral => (bool)value ? "true" : "false",
                ValueType.VoidLiteral => "void",
                ValueType.WordLiteral => value.ToString(),
                _ => throw new InvalidOperationException()
            };
        }

        public HelixType GetReturnType(TypeFrame types) {
            if (this.AsVariable().TryGetValue(out var variable)) {
                if (types.TryGetVariable(variable, out var type)) {
                    return type.InnerType;
                }
            }

            return (HelixType)this.value;
        }

        public PointerType AssertIsPointer(TypeFrame types) {
            var type = this.GetReturnType(types);

            if (!type.AsVariable(types).TryGetValue(out var pointer)) {
                throw TypeException.ExpectedVariableType(this.Location, type);
            }

            return pointer;
        }
    }
}
