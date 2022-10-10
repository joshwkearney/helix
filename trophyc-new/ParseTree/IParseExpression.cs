using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Trophy.Analysis;
using Trophy.Parsing;

namespace Trophy.Parsing.ParseTree {
    public interface IParseDeclaration {
        public TokenLocation Location { get; }

        public string Path { get; }
    }

    public interface IParseExpression {
        public TokenLocation Location { get; }

        public string Analyze(IRWriter writer, NameTable table);
    }

    public enum CompositeKind {
        Struct, Union
    }

    public class ErrorDeclaration : IParseDeclaration {
        public TokenLocation Location => default;

        public string Path => string.Empty;
    }

    public class CompositeDeclaration : IParseDeclaration {
        public CompositeKind Kind { get; }

        public CompositeSignature Signature { get; }

        public IReadOnlyList<IParseDeclaration> Children { get; }

        public string Path { get; }

        public TokenLocation Location { get; }

        public CompositeDeclaration(TokenLocation loc, string path, CompositeKind kind, 
            CompositeSignature signature, IReadOnlyList<IParseDeclaration> children) {

            this.Location = loc;
            this.Path = path;
            this.Kind = kind;
            this.Signature = signature;
            this.Children = children;
        }
    }

    public class FunctionDeclaration : IParseDeclaration {
        public TokenLocation Location { get; }

        public string Path { get; }

        public FunctionSignature Signature { get; }

        public IParseExpression Body { get; }

        public FunctionDeclaration(TokenLocation location, string path, FunctionSignature signature, 
            IParseExpression body) {

            this.Location = location;
            this.Path = path;
            this.Signature = signature;
            this.Body = body;
        }
    }

    public class IntLiteral : IParseExpression {
        public TokenLocation Location { get; }

        public int Value { get; }

        public IntLiteral(TokenLocation location, int value) {
            this.Location = location;
            this.Value = value;
        }

        public string Analyze(IRWriter writer, NameTable table) {
            var name = writer.GetTempName();
            writer.WriteVariableDeclaration(name, new IRIntLiteral(this.Value));

            return name;
        }
    }

    public class BlockExpression : IParseExpression {
        public TokenLocation Location { get; }

        public IReadOnlyList<IParseExpression> Statements { get; }

        public BlockExpression(TokenLocation location, IReadOnlyList<IParseExpression> statements) {
            this.Location = location;
            this.Statements = statements;
        }

        public string Analyze(IRWriter writer, NameTable table) {
            if (this.Statements.Any()) {
                foreach (var stat in this.Statements.Take(this.Statements.Count-1)) {
                    stat.Analyze(writer, table);
                }

                return this.Statements.Last().Analyze(writer, table);
            }
            else { 
                return new IntLiteral(this.Location, 0).Analyze(writer, table);
            }
        }
    }

    public class VariableStatement : IParseExpression {
        public TokenLocation Location { get; }

        public string Path { get; }

        public string Name { get; }

        public IParseExpression AssignValue { get; }

        public VariableStatement(TokenLocation location, string name, string path, 
            IParseExpression assignValue) {

            this.Location = location;
            this.Name = name;
            this.Path = path;
            this.AssignValue = assignValue;
        }

        public string Analyze(IRWriter writer, NameTable table) {
            // Make sure this name isn't taken
            if (table.DoesPathShadow(this.Path)) {
                throw new Exception();
            }

            var result = this.AssignValue.Analyze(writer, table);
            writer.WriteVariableStatement(this.Path, result);

            return new IntLiteral(this.Location, 0).Analyze(writer, table);
        }
    }

    public class VariableAccess : IParseExpression {
        public string VariableName { get; }

        public string Scope { get; }

        public TokenLocation Location { get; }

        public VariableAccess(TokenLocation location, string variableName) {
            this.Location = location;
            this.VariableName = variableName;
        }

        public string Analyze(IRWriter writer, NameTable table) {
            if (!table.TryFindPath(this.Scope, this.VariableName, out var path, out var target)) {
                throw new Exception();
            }

            if (target != NameTarget.Variable) {
                throw new Exception();
            }

            return path;
        }
    }

    public class ErrorExpression : IParseExpression {
        public TokenLocation Location => default;

        public string Analyze(IRWriter writer, NameTable table) {
            throw new NotImplementedException();
        }
    }
}
