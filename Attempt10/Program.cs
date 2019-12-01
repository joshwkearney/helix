using JoshuaKearney.FileSystem;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Attempt12 {
    public class Program {
        public static void Main(string[] args) {
            Lexer lex = new Lexer(File.ReadAllText("program.trophy"));
            Parser parse = new Parser(lex);

            var parsed = parse.Parse();
            var tree = parsed(CreateDefaultScope());
            var result = new Interpreter().Interpret(tree);

            Console.WriteLine(result.Value);
            Console.Read();
        }

        public static FunctionLiteralSyntax GetFunctionSyntax(PrimitiveOperation op, ITrophyType retType, params ITrophyType[] argTypes) {
            return new FunctionLiteralSyntax(
                new TrophyFunctionType(
                    retType,
                    argTypes),
                new PrimitiveOperationSyntax(
                    retType,
                    op,
                    new Scope(),
                    argTypes.Select((x, i) => new VariableLiteralSyntax("x" + i, x, new Scope())).ToArray()),
                new Scope(),
                new VariableInfo[0],
                argTypes.Select((x, i) => new FunctionParameter("x" + i, x)).ToArray());
        }

        public static TypeScope GetInt64TypeScope() {           
            return new TypeScope()
                .SetFunctionMember(
                    "not",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64Not,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "negate",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64Negate,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "add", 
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64Add, 
                        PrimitiveTrophyType.Int64Type, 
                        PrimitiveTrophyType.Int64Type, 
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "subtract",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64Subtract,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "multiply",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64Multiply,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "divide",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64RealDivide,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "divide_strict",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64StrictDivide,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "and",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64And,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "or",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64Or,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "xor",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64Xor,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "greater_than",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64GreaterThan,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type))
                .SetFunctionMember(
                    "less_than",
                    GetFunctionSyntax(
                        PrimitiveOperation.Int64LessThan,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Int64Type,
                        PrimitiveTrophyType.Int64Type));
        }

        public static TypeScope GetReal64TypeScope() {
            return new TypeScope()
                .SetFunctionMember(
                    "negate",
                    GetFunctionSyntax(
                        PrimitiveOperation.Real64Negate,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type))
                .SetFunctionMember(
                    "add",
                    GetFunctionSyntax(
                        PrimitiveOperation.Real64Add,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type))
                .SetFunctionMember(
                    "subtract",
                    GetFunctionSyntax(
                        PrimitiveOperation.Real64Subtract,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type))
                .SetFunctionMember(
                    "multiply",
                    GetFunctionSyntax(
                        PrimitiveOperation.Real64Multiply,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type))
                .SetFunctionMember(
                    "divide",
                    GetFunctionSyntax(
                        PrimitiveOperation.Real64Divide,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type))
                .SetFunctionMember(
                    "greater_than",
                    GetFunctionSyntax(
                        PrimitiveOperation.Real64LessThan,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type))
                .SetFunctionMember(
                    "less_than",
                    GetFunctionSyntax(
                        PrimitiveOperation.Real64LessThan,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Real64Type,
                        PrimitiveTrophyType.Real64Type));
        }

        public static TypeScope GetBooleanTypeScope() {
            return new TypeScope()
                .SetFunctionMember(
                    "not",
                    GetFunctionSyntax(
                        PrimitiveOperation.BooleanNot,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Boolean))
                .SetFunctionMember(
                    "and",
                    GetFunctionSyntax(
                        PrimitiveOperation.BooleanAnd,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Boolean))
                .SetFunctionMember(
                    "or",
                    GetFunctionSyntax(
                        PrimitiveOperation.BooleanOr,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Boolean))
                .SetFunctionMember(
                    "xor",
                    GetFunctionSyntax(
                        PrimitiveOperation.BooleanXor,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Boolean,
                        PrimitiveTrophyType.Boolean));
        }

        public static Scope CreateDefaultScope() {
            Scope scope = new Scope();

            scope = scope.SetTypeScope(PrimitiveTrophyType.Int64Type, GetInt64TypeScope());
            scope = scope.SetTypeScope(PrimitiveTrophyType.Real64Type, GetReal64TypeScope());
            scope = scope.SetTypeScope(PrimitiveTrophyType.Boolean, GetBooleanTypeScope());

            return scope;
        }
    }
}