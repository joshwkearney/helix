using System;
using System.Collections.Generic;
using System.Text;
using Trophy.Parsing;

namespace Trophy.Analysis {
    public interface ITypeUnifier {
        public void SetVariableType(string name, ITrophyType type);

        public bool TryGetVariableType(string name);
    }

    public interface IIRStatement {
        public IIRStatement CheckTypes();
    }

    public class IRVariableDeclaration : IIRStatement {
        public string Name { get; }

        public IIRExpression Value { get; }

        public IRVariableDeclaration(string name, IIRExpression value) {
            this.Name = name;
            this.Value = value;
        }

        public IIRStatement CheckTypes() {
            return new IRVariableDeclaration(this.Name, this.Value.CheckTypes());
        }
    }

    public interface IIRExpression {
        public IIRExpression CheckTypes();
    }

    public class IRVariableAccess : IIRExpression {
        public string Name { get; }

        public IRVariableAccess(string name) {
            this.Name = name;
        }
    }

    public class IRIntLiteral : IIRExpression {
        public int Value { get; }

        public IRIntLiteral(int value) {
            this.Value = value;
        }
    }

    public class IRWriter {
        private int tempCounter = 0;
        private readonly List<IIRStatement> stats = new List<IIRStatement>();

        public IReadOnlyList<IIRStatement> Statements => this.stats;

        public string GetTempName() {
            return "_trophy_temp_" + this.tempCounter++;
        }

        public void WriteVariableStatement(string name, string value) {
            this.WriteVariableDeclaration(name, new IRVariableAccess(value));
        }

        public void WriteVariableDeclaration(string name, IIRExpression value) {
            this.stats.Add(new IRVariableDeclaration(name, value));
        }
    }
}
