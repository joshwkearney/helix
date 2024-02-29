using Helix.Common;
using Helix.Common.Hmm;
using Helix.Common.Types;
using Helix.MiddleEnd.Unification;
using System.Text;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeChecker : IHmmVisitor<string> {
        private static readonly Dictionary<BinaryOperationKind, IHelixType> intOperations = new() {
            { BinaryOperationKind.Add,                  WordType.Instance },
            { BinaryOperationKind.Subtract,             WordType.Instance },
            { BinaryOperationKind.Multiply,             WordType.Instance },
            { BinaryOperationKind.Modulo,               WordType.Instance },
            { BinaryOperationKind.FloorDivide,          WordType.Instance },
            { BinaryOperationKind.And,                  WordType.Instance },
            { BinaryOperationKind.Or,                   WordType.Instance },
            { BinaryOperationKind.Xor,                  WordType.Instance },
            { BinaryOperationKind.EqualTo,              BoolType.Instance },
            { BinaryOperationKind.NotEqualTo,           BoolType.Instance },
            { BinaryOperationKind.GreaterThan,          BoolType.Instance },
            { BinaryOperationKind.LessThan,             BoolType.Instance },
            { BinaryOperationKind.GreaterThanOrEqualTo, BoolType.Instance },
            { BinaryOperationKind.LessThanOrEqualTo,    BoolType.Instance },
        };

        private static readonly Dictionary<BinaryOperationKind, IHelixType> boolOperations = new() {
            { BinaryOperationKind.And,                  BoolType.Instance },
            { BinaryOperationKind.Or,                   BoolType.Instance },
            { BinaryOperationKind.Xor,                  BoolType.Instance },
            { BinaryOperationKind.EqualTo,              BoolType.Instance },
            { BinaryOperationKind.NotEqualTo,           BoolType.Instance },
        };

        private readonly TypeCheckingContext context;
        private readonly Stack<IHelixType> functionReturnTypes = [];
        private readonly Stack<bool> isInLoops = [];

        private bool IsInLoop => this.isInLoops.Peek();

        private HmmWriter Writer => this.context.Writer;

        private TypeStore Types => this.context.Types;

        private TypeUnifier Unifier => this.context.Unifier;

        public TypeChecker(TypeCheckingContext context) {
            this.context = context;
            this.isInLoops.Push(false);
        }

        public string VisitArrayLiteral(HmmArrayLiteral syntax) {
            if (syntax.Args.Count == 0) {
                return "void";
            }

            var totalType = this.Types[syntax.Args[0]];

            for (int i = 1; i < syntax.Args.Count; i++) {
                var argType = this.Types[syntax.Args[i]];

                if (!this.Unifier.TryUnifyWithConvert(argType, totalType).TryGetValue(out totalType)) {
                    throw new NotImplementedException();
                    //throw TypeException.UnexpectedType(args[i].Location, totalType, argType);
                }
            }

            var args = syntax.Args.Select(x => this.Unifier.Convert(x, totalType, syntax.Location)).ToArray();

            this.Writer.AddLine(new HmmArrayLiteral() {
                Location = syntax.Location,
                Args = args,
                Result = syntax.Result
            });

            this.Types[syntax.Result] = new ArrayType() { InnerType = totalType };
            return syntax.Result;
        }

        public string VisitAssignment(HmmAssignment syntax) {
            throw new NotImplementedException();
        }

        public string VisitAsSyntax(HmmAsSyntax syntax) {
            var result = this.Unifier.Convert(syntax.Operand, syntax.Type, syntax.Location);

            this.Writer.AddLine(new HmmVariableStatement() {
                Location = syntax.Location,
                IsMutable = false,
                Value = result,
                Variable = syntax.Result
            });

            this.Types[syntax.Result] = syntax.Type;
            return syntax.Result;
        }

        public string VisitBinarySyntax(HmmBinaryOperator syntax) {
            Assert.IsFalse(syntax.Operator == BinaryOperationKind.BranchingAnd);
            Assert.IsFalse(syntax.Operator == BinaryOperationKind.BranchingOr);

            if (syntax.Operator == BinaryOperationKind.Index) {
                throw new NotImplementedException();
            }

            var type1 = this.Types[syntax.Left];
            var type2 = this.Types[syntax.Right];

            var left = syntax.Left;
            var right = syntax.Right;

            IHelixType? returnType;

            if (this.Unifier.CanConvert(type1, BoolType.Instance) && this.Unifier.CanConvert(type2, BoolType.Instance)) {
                if (!boolOperations.TryGetValue(syntax.Operator, out returnType)) {
                    throw TypeCheckException.InvalidBinaryOperator(syntax.Location, type1, type2, syntax.Operator);
                }

                left = this.Unifier.Convert(left, BoolType.Instance, syntax.Location);
                right = this.Unifier.Convert(right, BoolType.Instance, syntax.Location);

                this.Types[syntax.Result] = boolOperations[syntax.Operator];
            }
            else if (this.Unifier.CanConvert(type1, WordType.Instance) && this.Unifier.CanConvert(type2, WordType.Instance)) {
                if (!intOperations.TryGetValue(syntax.Operator, out returnType)) {
                    throw TypeCheckException.InvalidBinaryOperator(syntax.Location, type1, type2, syntax.Operator);
                }

                left = this.Unifier.Convert(left, WordType.Instance, syntax.Location);
                right = this.Unifier.Convert(right, WordType.Instance, syntax.Location);
            }
            else {
                throw TypeCheckException.InvalidBinaryOperator(syntax.Location, type1, type2, syntax.Operator);
            }

            this.Writer.AddLine(new HmmBinaryOperator() {
                Location = syntax.Location,
                Left = left,
                Right = right,
                Operator = syntax.Operator,
                Result = syntax.Result
            });

            this.Types[syntax.Result] = returnType;
            return syntax.Result;
        }

        public string VisitBreak(HmmBreakSyntax syntax) {
            if (!this.IsInLoop) {
                throw TypeCheckException.NotInLoop(syntax.Location);
            }

            this.Writer.AddLine(new HmmBreakSyntax() {
                Location = syntax.Location
            });

            return "void";
        }

        public string VisitContinue(HmmContinueSyntax syntax) {
            if (!this.IsInLoop) {
                throw TypeCheckException.NotInLoop(syntax.Location);
            }

            this.Writer.AddLine(new HmmContinueSyntax() {
                Location = syntax.Location
            });

            return "void";
        }

        public string VisitFunctionDeclaration(HmmFunctionDeclaration syntax) {
            this.functionReturnTypes.Push(syntax.Signature.ReturnType);
            this.Writer.PushBlock();

            foreach (var par in syntax.Signature.Parameters) {
                this.Types[par.Name] = par.Type;
            }

            foreach (var stat in syntax.Body) {
                stat.Accept(this);
            }

            var body = this.Writer.PopBlock();
            this.functionReturnTypes.Pop();

            // TODO: Make sure function always returns

            this.Writer.AddLine(new HmmFunctionDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Signature = syntax.Signature,
                Body = body
            });

            return "void";
        }

        public string VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax) {
            this.Types[syntax.Name] = syntax.Signature;
            this.Writer.AddFowardDeclaration(syntax);

            return "void";
        }

        public string VisitIfExpression(HmmIfExpression syntax) {
            var cond = this.Unifier.Convert(syntax.Condition, BoolType.Instance, syntax.Location);

            // Write affirmative block
            this.Writer.PushBlock();
            foreach (var line in syntax.AffirmativeBody) {
                line.Accept(this);
            }
            var affirmBody = this.Writer.PopBlock();

            // Write negative block
            this.Writer.PushBlock();
            foreach (var line in syntax.NegativeBody) {
                line.Accept(this);
            }
            var negBody = this.Writer.PopBlock();

            // Unify types
            var affirmType = this.Types[syntax.Affirmative];
            var negType = this.Types[syntax.Negative];
            var totalType = this.Unifier.UnifyWithConvert(affirmType, negType, syntax.Location); 

            // Unify the true branch in its scope
            this.Writer.PushBlock(affirmBody);
            var affirm = this.Unifier.Convert(syntax.Affirmative, totalType, syntax.Location);
            this.Writer.PopBlock();

            // unify the false branch in its scope
            this.Writer.PushBlock(negBody);
            var neg = this.Unifier.Convert(syntax.Negative, totalType, syntax.Location);
            this.Writer.PopBlock();

            this.Writer.AddLine(new HmmIfExpression() {
                Location = syntax.Location,
                Affirmative = affirm,
                Negative = neg,
                AffirmativeBody = affirmBody,
                NegativeBody = negBody,
                Condition = cond,
                Result = syntax.Result
            });

            this.Types[syntax.Result] = totalType;
            return syntax.Result;
        }

        public string VisitInvoke(HmmInvokeSyntax syntax) {
            if (!this.Types[syntax.Target].TryGetFunctionSignature(this.context).TryGetValue(out var sig)) {
                throw TypeCheckException.ExpectedFunctionType(syntax.Location, this.Types[syntax.Target]);
            }

            if (syntax.Arguments.Count != sig.Parameters.Count) {
                throw TypeCheckException.ParameterCountMismatch(syntax.Location, sig.Parameters.Count, syntax.Arguments.Count);
            }

            var newArgs = new List<string>();
            foreach (var (sigPar, arg) in sig.Parameters.Zip(syntax.Arguments)) {
                var newArg = this.Unifier.Convert(arg, sigPar.Type, syntax.Location);

                newArgs.Add(newArg);
            }

            this.Writer.AddLine(new HmmInvokeSyntax() {
                Location = syntax.Location,
                Target = syntax.Target,
                Arguments = newArgs,
                Result = syntax.Result
            });

            this.Types[syntax.Result] = sig.ReturnType;
            return syntax.Result;
        }

        public string VisitIs(HmmIsSyntax syntax) {
            var type = this.Types[syntax.Operand];

            if (!type.GetUnionSignature(this.context).TryGetValue(out var unionType)) {
                throw TypeCheckException.ExpectedUnionType(syntax.Location);
            }

            if (!unionType.Members.Any(x => x.Name == syntax.Field)) {
                throw TypeCheckException.MemberUndefined(syntax.Location, type, syntax.Field);
            }

            this.Writer.AddLine(new HmmIsSyntax() {
                Location = syntax.Location,
                Field = syntax.Field,
                Operand = syntax.Operand,
                Result = syntax.Result
            });

            this.Types[syntax.Result] = BoolType.Instance;
            return syntax.Result;
        }

        public string VisitLoop(HmmLoopSyntax syntax) {
            this.Writer.PushBlock();
            this.isInLoops.Push(true);

            foreach (var stat in syntax.Body) {
                stat.Accept(this);
            }

            this.isInLoops.Pop();
            var body = this.Writer.PopBlock();

            this.Writer.AddLine(new HmmLoopSyntax() {
                Location = syntax.Location,
                Body = body
            });

            return "void";
        }

        public string VisitMemberAccess(HmmMemberAccess syntax) {
            throw new NotImplementedException();
        }

        public string VisitNew(HmmNewSyntax syntax) {
            // For simple stuff let the unifier deal with it
            if (syntax.Type == VoidType.Instance || syntax.Type == WordType.Instance || syntax.Type == BoolType.Instance) {
                var result = this.Unifier.Convert("void", syntax.Type, syntax.Location);

                this.Writer.AddLine(new HmmVariableStatement() {
                    Location = syntax.Location,
                    IsMutable = false,
                    Value = result,
                    Variable = syntax.Result
                });

                this.Types[syntax.Result] = syntax.Type;
                return syntax.Result;
            }
            else if (syntax.Type is ArrayType) {
                throw new NotImplementedException();
            }
            else if (syntax.Type.GetStructSignature(this.context).TryGetValue(out var structType)) {
                return this.TypeCheckNewStruct(syntax, structType);
            }
            else if (syntax.Type.GetUnionSignature(this.context).TryGetValue(out var unionType)) {
                return this.TypeCheckNewUnion(syntax, unionType);
            }

            throw TypeCheckException.UnexpectedType(syntax.Location, syntax.Type);
        }

        private string TypeCheckNewStruct(HmmNewSyntax syntax, StructType sig) {
            var namedMems = syntax.Assignments
                .Where(x => x.Field.HasValue)
                .ToArray();

            var unusedFields = sig.Members
                .Where(x => namedMems.All(y => y.Field != x.Name))
                .ToArray();

            var anonMems = syntax.Assignments
                .Except(namedMems)
                .ToArray();

            if (anonMems.Length > unusedFields.Length) {
                throw TypeCheckException.NewObjectHasExtraneousFields(syntax.Location, syntax.Type);
            }

            var inferredFields = anonMems
                .Zip(unusedFields)
                .Select(x => new HmmNewFieldAssignment() { Field = x.Second.Name, Value = x.First.Value })
                .ToArray();

            var allFields = namedMems.Concat(inferredFields).ToArray();

            foreach (var field in allFields) {
                Assert.IsTrue(field.Field.HasValue);
            }

            var dups = allFields
                .GroupBy(x => x.Field.GetValue())
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToArray();

            // Make sure there are no duplicate names
            if (dups.Length > 0) {
                throw TypeCheckException.NewObjectHasExtraneousFields(syntax.Location, syntax.Type, dups);
            }

            var undefinedFields = allFields
                .Select(x => x.Field.GetValue())
                .Except(sig.Members.Select(x => x.Name))
                .ToArray();

            // Make sure that all members are defined in the struct
            if (undefinedFields.Any()) {
                throw TypeCheckException.MemberUndefined(syntax.Location, syntax.Type, undefinedFields[0]);
            }

            var absentFields = sig.Members
                .Select(x => x.Name)
                .Except(allFields.Select(x => x.Field.GetValue()))
                .Select(x => sig.Members.First(y => x == y.Name))
                .ToArray();

            var requiredAbsentFields = absentFields
                .Where(x => !x.Type.HasVoidValue(this.context))
                .ToArray();

            // Make sure that all the missing members have a default value
            if (requiredAbsentFields.Any()) {
                throw TypeCheckException.TypeWithoutVoidValue(syntax.Location, requiredAbsentFields[0].Type);
            }

            var fieldsDict = allFields.ToDictionary(x => x.Field.GetValue(), x => x.Value);
            var newAssignments = new List<HmmNewFieldAssignment>();

            // Unify the arguments to the correct type
            foreach (var mem in sig.Members) {
                if (!fieldsDict.TryGetValue(mem.Name, out var value)) {
                    Assert.IsTrue(this.Unifier.CanConvert(VoidType.Instance, mem.Type));

                    value = this.Unifier.Convert("void", mem.Type, syntax.Location);
                }

                newAssignments.Add(new HmmNewFieldAssignment() {
                    Field = mem.Name,
                    Value = value
                });
            }

            this.Writer.AddLine(new HmmNewSyntax() {
                Location = syntax.Location,
                Assignments = newAssignments,
                Result = syntax.Result,
                Type = syntax.Type
            });

            this.Types[syntax.Result] = syntax.Type;
            return syntax.Result;
        }

        private string TypeCheckNewUnion(HmmNewSyntax syntax, UnionType sig) {
            if (syntax.Assignments.Count > 1) {
                throw TypeCheckException.NewUnionMultipleMembers(syntax.Location);
            }

            // TODO (?): Will fail if union doens't have any members

            string name;
            string value;

            if (syntax.Assignments.Count == 0) {
                name = sig.Members[0].Name;

                if (!sig.Members[0].Type.HasVoidValue(this.context)) {
                    throw TypeCheckException.TypeWithoutVoidValue(syntax.Location, syntax.Type);
                }

                value = this.Unifier.Convert("void", sig.Members[0].Type, syntax.Location);
            }
            else {
                name = syntax.Assignments[0].Field.OrElse(() => sig.Members[0].Name);
                value = syntax.Assignments[0].Value;
            }

            var mem = sig.Members.FirstOrDefault(x => x.Name == name);
            if (mem == null) {
                throw TypeCheckException.MemberUndefined(syntax.Location, syntax.Type, name);
            }

            value = this.Unifier.Convert(value, mem.Type, syntax.Location);

            this.Writer.AddLine(new HmmNewSyntax() {
                Location = syntax.Location,
                Type = syntax.Type,
                Assignments = [
                    new HmmNewFieldAssignment() {
                        Field = name,
                        Value = value
                    }
                ],
                Result = syntax.Result
            });

            this.Types[syntax.Result] = syntax.Type;
            return syntax.Result;
        }

        public string VisitReturn(HmmReturnSyntax syntax) {
            Assert.IsTrue(this.functionReturnTypes.Count > 0);

            var operand = this.Unifier.Convert(syntax.Operand, this.functionReturnTypes.Peek(), syntax.Location);

            this.Writer.AddLine(new HmmReturnSyntax() {
                Location = syntax.Location,
                Operand = operand
            });

            return "void";
        }

        public string VisitStructDeclaration(HmmStructDeclaration syntax) {
            this.Types[syntax.Name] = syntax.Signature;
            this.Writer.AddLine(syntax);

            // Make sure this struct isn't circular
            if (syntax.Type.GetRecursiveFieldTypes(this.context).Contains(syntax.Type)) {
                throw TypeCheckException.CircularValueObject(syntax.Location, syntax.Type);
            }

            return "void";
        }

        public string VisitTypeDeclaration(HmmTypeDeclaration syntax) {
            this.Writer.AddLine(syntax);

            return "void";
        }

        public string VisitUnaryOperator(HmmUnaryOperator syntax) {
            Assert.IsFalse(syntax.Operator == UnaryOperatorKind.Plus);
            Assert.IsFalse(syntax.Operator == UnaryOperatorKind.Minus);

            // TODO: AddressOf and Dereference

            if (syntax.Operator == UnaryOperatorKind.Not) {
                var arg = this.Unifier.Convert(syntax.Operand, BoolType.Instance, syntax.Location);

                this.Writer.AddLine(new HmmUnaryOperator() {
                    Location = syntax.Location,
                    Operand = arg,
                    Operator = syntax.Operator,
                    Result = syntax.Result
                });

                this.Types[syntax.Result] = BoolType.Instance;
                return syntax.Result;
            }
            else {
                throw new NotImplementedException();
            }
        }

        public string VisitUnionDeclaration(HmmUnionDeclaration syntax) {
            this.Types[syntax.Name] = syntax.Signature;
            this.Writer.AddLine(syntax);

            // Make sure this struct isn't circular
            if (syntax.Type.GetRecursiveFieldTypes(this.context).Contains(syntax.Type)) {
                throw TypeCheckException.CircularValueObject(syntax.Location, syntax.Type);
            }

            return "void";
        }

        public string VisitVariableStatement(HmmVariableStatement syntax) {
            this.Types.TransferType(syntax.Value, syntax.Variable);

            this.Writer.AddLine(new HmmVariableStatement() {
                Location = syntax.Location,
                IsMutable = syntax.IsMutable,
                Value = syntax.Value,
                Variable = syntax.Variable
            });

            return syntax.Variable;
        }
    }
}
