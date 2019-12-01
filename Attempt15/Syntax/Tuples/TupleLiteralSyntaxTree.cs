using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JoshuaKearney.Attempt15.Compiling;
using JoshuaKearney.Attempt15.Types;

namespace JoshuaKearney.Attempt15.Syntax.Tuples {
    public class TupleMember {
        public string Name { get; }

        public ISyntaxTree Value { get; }

        public TupleMember(string name, ISyntaxTree value) {
            this.Name = name;
            this.Value = value;
        }
    }

    public class TupleLiteralSyntaxTree : ISyntaxTree {
        public IReadOnlyList<TupleMember> Members { get; }

        public TupleType ExpressionType { get; }

        public ExternalVariablesCollection ExternalVariables { get; }

        ITrophyType ISyntaxTree.ExpressionType => this.ExpressionType;

        public TupleLiteralSyntaxTree(IEnumerable<TupleMember> members) {
            this.Members = members.ToArray();
            this.ExpressionType = new TupleType(members.Select(x => new IdentifierInfo(x.Name, x.Value.ExpressionType)));
            this.ExternalVariables = this.Members
                .Select(x => x.Value.ExternalVariables)
                .Aggregate(new ExternalVariablesCollection(), (x, y) => x.Union(y));
        }

        public string GenerateCode(CodeGenerateEventArgs args) {
            return args.TupleGenerator.GenerateTupleLiteral(this, args);
        }

        public bool DoesVariableEscape(string variableName) {
            foreach (var member in this.Members) {
                if (member.Value.DoesVariableEscape(variableName)) {
                    return true;
                }
            }

            return false;
        }
    }
}