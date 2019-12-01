using JoshuaKearney.Attempt15.Syntax.Tuples;
using JoshuaKearney.Attempt15.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JoshuaKearney.Attempt15.Compiling {
    public class TupleCodeGenerator {
        private int tupleCounter;
        private readonly Dictionary<ITrophyType[], int> tupleIds = new Dictionary<ITrophyType[], int>(new TypeArrayComparer());

        public string GetTupleTypeName(TupleType type, CodeGenerateEventArgs args) {
            var key = type.Members.Select(x => x.Type).ToArray();

            if (this.tupleIds.TryGetValue(key, out int id)) {
                return "$tuple" + id;
            }
            else {
                id = this.tupleCounter++;
                this.tupleIds[key] = id;
                this.GenerateTupleStructure(id, type, args);

                return "$tuple" + id;
            }
        }

        public string GenerateTupleLiteral(TupleLiteralSyntaxTree syntax, CodeGenerateEventArgs args) {
            var tempName = args.CodeGenerator.GetTempVariableName();
            var strTupleType = syntax.ExpressionType.GenerateName(args);

            // Generate the tuple values
            var values = syntax.Members
                .Select(x => x.Value.GenerateCode(args))
                .ToArray();

            // Declare the tuple variable
            args.CodeGenerator.Declaration(strTupleType, tempName);

            // Malloc the counter if this tuple is reference counted
            if (syntax.ExpressionType.IsReferenceCounted) {
                args.CodeGenerator.Assignment(
                    tempName + ".$counter",
                    args.CodeGenerator.Malloc("int")
                );

                args.CodeGenerator.Statement($"(*{tempName}.$counter) = 1;");

                args.MemoryManager.RegisterVariable(tempName, syntax.ExpressionType);
            }

            // Assign members
            for (int i = 1; i <= values.Length; i++) {
                args.CodeGenerator.Assignment(
                    tempName + ".$item" + i,
                    values[i-1]
                );
            }

            return tempName;
        }

        private void GenerateTupleStructure(int id, TupleType tupleType, CodeGenerateEventArgs args) {
            var members = new List<CMember>();
            var strTupleType = tupleType.GenerateName(args);

            int counter = 1;
            foreach(var type in tupleType.Members.Select(x => x.Type)) {
                var typeName = type.GenerateName(args);
                var name = "$item" + counter++;

                members.Add(new CMember(name, typeName));
            }

            // Add a counter member and an incrementor/decrementor
            // if this tuple must be reference counted
            if (tupleType.IsReferenceCounted) {
                members.Add(new CMember("$counter", "int*"));                
            }

            args.CodeGenerator.TypedefStruct(
                "$tuple" + id,
                members
            );

            if (tupleType.IsReferenceCounted) {
                // Add an incrementor
                string incName = "$tuple" + id + "_incrementor";
                args.MemoryManager.ReferenceIncrementors[tupleType] = incName;

                args.CodeGenerator.Function(
                    incName,
                    new[] { new CMember("tuple", strTupleType) },
                    new[] { "(*tuple.$counter)++;" }
                );

                // Add a decrementor
                string decName = "$tuple" + id + "_decrementor";
                args.MemoryManager.ReferenceDecrementors[tupleType] = decName;

                // Generate the cleanup code
                args.CodeGenerator.CodeBlocks.Push(new List<string>());
                counter = 1;
                foreach (var mem in tupleType.Members) {
                    if (mem.Type.IsReferenceCounted) {
                        args.MemoryManager.DecrementValue(mem.Type, "tuple.$item" + counter);
                    }

                    counter++;
                }
                args.CodeGenerator.Statement("free(tuple.$counter);");
                var cleanupCode = args.CodeGenerator.CodeBlocks.Pop();

                // Generate the decrementor body
                args.CodeGenerator.CodeBlocks.Push(new List<string>());
                args.CodeGenerator.Statement("(*tuple.$counter)--;");
                args.CodeGenerator.IfStatement(
                    "(*tuple.$counter) == 0",
                    cleanupCode
                );

                // Generate the actual decrementor function
                args.CodeGenerator.Function(
                    decName,
                    new[] { new CMember("tuple", strTupleType) },
                    args.CodeGenerator.CodeBlocks.Pop()
                );
            }
        }

        private class TypeArrayComparer : IEqualityComparer<ITrophyType[]> {
            public bool Equals(ITrophyType[] x, ITrophyType[] y) {
                return x.SequenceEqual(y);
            }

            public int GetHashCode(ITrophyType[] obj) {
                return obj.Aggregate(13, (x, y) => x + 23 * y.GetHashCode());
            }
        }
    }
}
