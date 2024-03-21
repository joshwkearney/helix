using Helix.Common;
using Helix.Common.Hmm;
using Helix.Common.Types;
using Helix.MiddleEnd.FlowTyping;
using Helix.MiddleEnd.Interpreting;

namespace Helix.MiddleEnd.TypeChecking {
    internal class TypeChecker : IHmmVisitor<TypeCheckResult> {
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

        private readonly AnalysisContext context;
        private readonly Evaluator eval;
        private readonly PredicatesTracker predicateTracker;

        private HmmWriter Writer => this.context.Writer;

        private TypeStore Types => this.context.Types;

        private TypeUnifier Unifier => this.context.Unifier;

        private AliasStore Aliases => this.context.Aliases;

        public TypeChecker(AnalysisContext context) {
            this.context = context;
            this.eval = new Evaluator(context);
            this.predicateTracker = new PredicatesTracker(context);
        }

        public TypeCheckResult VisitArrayLiteral(HmmArrayLiteral syntax) {
            if (syntax.Args.Count == 0) {
                return TypeCheckResult.VoidResult;
            }

            var totalType = this.Types[syntax.Args[0]];

            for (int i = 1; i < syntax.Args.Count; i++) {
                var argType = this.Types[syntax.Args[i]];

                if (!this.Unifier.TryUnifyWithConvert(argType, totalType).TryGetValue(out totalType)) {
                    throw TypeCheckException.TypeConversionFailed(syntax.Location, totalType, argType);
                }
            }

            var args = syntax.Args.Select(x => this.Unifier.Convert(x, totalType, syntax.Location)).ToArray();

            this.context.AliasTracker.RegisterArrayLiteral(syntax.Result, args.Length, totalType);

            this.Writer.AddLine(new HmmArrayLiteral() {
                Location = syntax.Location,
                Args = args,
                Result = syntax.Result
            });

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        public TypeCheckResult VisitAssignment(HmmAssignment syntax) {
            var sigType = this.Types[syntax.Variable].GetSupertype();
            var assign = this.Unifier.Convert(syntax.Value, sigType, syntax.Location);

            this.Writer.AddLine(new HmmAssignment() {
                Location = syntax.Location,
                Variable = syntax.Variable,
                Value = assign
            });

            this.context.AliasTracker.RegisterAssignment(syntax.Variable, syntax.Value);

            return TypeCheckResult.VoidResult;
        }

        public TypeCheckResult VisitAsSyntax(HmmAsSyntax syntax) {
            var result = this.Unifier.Convert(syntax.Operand, syntax.Type, syntax.Location);

            this.Writer.AddLine(new HmmVariableStatement() {
                Location = syntax.Location,
                IsMutable = false,
                Value = result,
                Variable = syntax.Result
            });

            this.context.AliasTracker.RegisterLocal(syntax.Result, result);

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        public TypeCheckResult VisitBinarySyntax(HmmBinarySyntax syntax) {
            Assert.IsFalse(syntax.Operator == BinaryOperationKind.BranchingAnd);
            Assert.IsFalse(syntax.Operator == BinaryOperationKind.BranchingOr);
            Assert.IsFalse(syntax.Operator == BinaryOperationKind.Index);

            // Try to evaluate this at compile time
            if (this.eval.TryEvaluateVisitBinarySyntax(syntax, out var evalResult)) {
                return evalResult;
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

            this.Writer.AddLine(new HmmBinarySyntax() {
                Location = syntax.Location,
                Left = left,
                Right = right,
                Operator = syntax.Operator,
                Result = syntax.Result
            });

            this.context.AliasTracker.RegisterLocal(syntax.Result, returnType);
            this.predicateTracker.TrackBinaryExpression(syntax);

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        public TypeCheckResult VisitBreak(HmmBreakSyntax syntax) {
            Assert.IsTrue(this.context.ControlFlowStack.Any());

            if (!this.context.ControlFlowStack.Peek().IsInsideLoop) {
                throw TypeCheckException.NotInLoop(syntax.Location);
            }

            this.context.ControlFlowStack.Peek().AddLoopAppendix(this.context.ScopeStack.Peek());

            this.Writer.AddLine(new HmmBreakSyntax() {
                Location = syntax.Location
            });

            return TypeCheckResult.Break;
        }

        public TypeCheckResult VisitContinue(HmmContinueSyntax syntax) {
            Assert.IsTrue(this.context.ControlFlowStack.Any());

            if (!this.context.ControlFlowStack.Peek().IsInsideLoop) {
                throw TypeCheckException.NotInLoop(syntax.Location);
            }

            this.Writer.AddLine(new HmmContinueSyntax() {
                Location = syntax.Location
            });

            return TypeCheckResult.Continue;
        }

        public TypeCheckResult VisitFunctionDeclaration(HmmFunctionDeclaration syntax) {
            var frame = new ControlFlowFrame();
            frame.SetReturnType(syntax.Signature.ReturnType);

            this.context.ControlFlowStack.Push(frame);
            this.context.WriterStack.Push(this.Writer.CreateScope());

            foreach (var par in syntax.Signature.Parameters) {
                this.context.AliasTracker.RegisterFunctionParameter(par.Name, par.Type);
            }

            // Check the body
            var (_, bodyFlow) = this.CheckBody(syntax.Body);

            // Add a void function return if we need it
            if (syntax.Signature.ReturnType == VoidType.Instance && !bodyFlow.DoesFunctionReturn) {
                var line = new HmmReturnSyntax() { Location = syntax.Location, Operand = "void" };

                line.Accept(this);
                bodyFlow = ControlFlow.FunctionReturnFlow;
            }

            // Make sure we return
            if (!bodyFlow.DoesFunctionReturn) {
                throw TypeCheckException.NoReturn(syntax.Location);
            }

            var writtenBody = this.context.WriterStack.Pop().ScopedLines;
            this.context.ControlFlowStack.Pop();

            this.Writer.AddLine(new HmmFunctionDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Signature = syntax.Signature,
                Body = writtenBody
            });

            return TypeCheckResult.VoidResult;
        }

        public TypeCheckResult VisitFunctionForwardDeclaration(HmmFunctionForwardDeclaration syntax) {
            this.Types[new NamedLocation(syntax.Name)] = syntax.Signature;
            this.Writer.AddFowardDeclaration(syntax);

            return TypeCheckResult.VoidResult;
        }

        public TypeCheckResult VisitIfExpression(HmmIfExpression syntax) {
            var cond = this.Unifier.Convert(syntax.Condition, BoolType.Instance, syntax.Location);

            if (this.eval.TryEvaluateIfExpression(syntax, cond, out var result)) {
                return result;
            }

            // Write affirmative block
            this.context.ScopeStack.Push(this.context.ScopeStack.Peek().CreateScope());
            this.context.WriterStack.Push(this.Writer.CreateScope());
            this.context.Types.SetImplications(this.context.Predicates[cond].GetImplications());

            var (_, affirmFlow) = this.CheckBody(syntax.AffirmativeBody);

            var affirmBody = this.context.WriterStack.Pop();
            var affirmScope = this.context.ScopeStack.Pop();

            // Write negative block
            this.context.ScopeStack.Push(this.context.ScopeStack.Peek().CreateScope());
            this.context.WriterStack.Push(this.Writer.CreateScope());
            this.context.Types.SetImplications(this.context.Predicates[cond].Negate().GetImplications());

            var (_, negFlow) = this.CheckBody(syntax.NegativeBody);

            var negBody = this.context.WriterStack.Pop();
            var negScope = this.context.ScopeStack.Pop();

            // Unify types
            var affirmType = this.Types[syntax.Affirmative];
            var negType = this.Types[syntax.Negative];
            var totalType = this.Unifier.UnifyWithConvert(affirmType, negType, syntax.Location);

            // Unify the true branch in its scope
            this.context.ScopeStack.Push(affirmScope);
            this.context.WriterStack.Push(affirmBody);

            var affirm = this.Unifier.Convert(syntax.Affirmative, totalType, syntax.Location);

            this.context.WriterStack.Pop();
            this.context.ScopeStack.Pop();

            // Unify the false branch in its scope
            this.context.ScopeStack.Push(negScope);
            this.context.WriterStack.Push(negBody);

            var neg = this.Unifier.Convert(syntax.Negative, totalType, syntax.Location);

            this.context.WriterStack.Pop();
            this.context.ScopeStack.Pop();

            this.Writer.AddLine(new HmmIfExpression() {
                Location = syntax.Location,
                Affirmative = affirm,
                Negative = neg,
                AffirmativeBody = affirmBody.ScopedLines,
                NegativeBody = negBody.ScopedLines,
                Condition = cond,
                Result = syntax.Result
            });

            // Combine the branch aliases and replace the current ones
            if (affirmFlow.DoesJump) {
                this.context.ScopeStack.Pop();
                this.context.ScopeStack.Push(negScope);
            }
            else if (negFlow.DoesJump) {
                this.context.ScopeStack.Pop();
                this.context.ScopeStack.Push(affirmScope);
            }
            else {
                this.context.ScopeStack.Pop();
                this.context.ScopeStack.Push(affirmScope.MergeWith(negScope));
            }            

            this.context.AliasTracker.RegisterIf(syntax.Result, totalType, affirm, neg);

            var totalFlow = affirmFlow.Merge(negFlow);

            if (totalFlow.DoesJump) {
                return new TypeCheckResult("void", totalFlow);
            } 
            else {
                return new TypeCheckResult(syntax.Result, totalFlow);
            }
        }

        public TypeCheckResult VisitInvoke(HmmInvokeSyntax syntax) {
            var targetType = this.Types[syntax.Target];

            if (!targetType.TryGetFunctionSignature(this.context).TryGetValue(out var sig)) {
                throw TypeCheckException.ExpectedFunctionType(syntax.Location, targetType);
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

            this.context.AliasTracker.RegisterInvoke(syntax.Result, sig.ReturnType, newArgs, sig.Parameters.Select(x => x.Type).ToArray());

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        public TypeCheckResult VisitIs(HmmIsSyntax syntax) {
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

            this.context.AliasTracker.RegisterLocal(syntax.Result, BoolType.Instance);

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        public TypeCheckResult VisitLoop(HmmLoopSyntax syntax) {
            Assert.IsTrue(this.context.ControlFlowStack.Any());

            if (this.eval.TryEvaluateLoop(syntax, out var eval)) {
                return eval;
            }

            var returnFlow = TypeCheckResult.NormalFlow("void");

            var outerScope = this.context.ScopeStack.Peek();

            while (true) {
                this.context.ControlFlowStack.Push(this.context.ControlFlowStack.Peek().CreateLoopFrame());
                this.context.WriterStack.Push(this.Writer.CreateScope());
                this.context.ScopeStack.Push(outerScope.CreateScope());

                var (_, bodyFlow) = this.CheckBody(syntax.Body);

                var loopScope = this.context.ScopeStack.Pop();
                var body = this.context.WriterStack.Pop();
                var loopControlFlow = this.context.ControlFlowStack.Peek();

                // If the loop modified anything the outer scope, we need to type check this again
                // because types from previous iterations could affect later iterations

                if (!outerScope.WasModifiedBy(loopScope)) {
                    this.Writer.AddLine(new HmmLoopSyntax() {
                        Location = syntax.Location,
                        Body = body.ScopedLines
                    });

                    // Get all the aliases that existed at the time of a break statement, which are
                    // going to be the state of the world after the loop finishes since break statements
                    // are the only way for a loop to terminate
                    outerScope = loopControlFlow.LoopAppendix.Aggregate((x, y) => x.MergeWith(y));

                    if (bodyFlow.DoesFunctionReturn) {
                        returnFlow = new TypeCheckResult("void", bodyFlow);
                    }

                    break;
                }
                else {
                    outerScope = outerScope.MergeWith(loopScope);
                }
            }

            this.context.ScopeStack.Pop();
            this.context.ScopeStack.Push(outerScope);

            return returnFlow;
        }

        public TypeCheckResult VisitMemberAccess(HmmMemberAccess syntax) {
            var targetType = this.Types[syntax.Operand];

            if (targetType.GetArraySignature(this.context).TryGetValue(out _)) {
                if (syntax.Member != "count") {
                    throw TypeCheckException.MemberUndefined(syntax.Location, targetType, syntax.Member);
                }

                if (syntax.IsLValue) {
                    throw TypeCheckException.ExpectedRValue(syntax.Location);
                }

                this.Writer.AddLine(new HmmMemberAccess() {
                    Location = syntax.Location,
                    Member = syntax.Member,
                    Operand = syntax.Operand,
                    Result = syntax.Result,
                    IsLValue = false
                });

                return TypeCheckResult.NormalFlow(syntax.Result);
            }
            else if (targetType.GetStructSignature(this.context).TryGetValue(out var structType)) {
                var mem = structType.Members.FirstOrDefault(x => x.Name == syntax.Member);
                if (mem == null) {
                    throw TypeCheckException.MemberUndefined(syntax.Location, targetType, syntax.Member);
                }

                if (syntax.IsLValue) {
                    this.context.AliasTracker.RegisterMemberAccessReference(syntax.Result, syntax.Operand, mem.Name);
                }
                else {
                    if (this.eval.TryEvaluateRValueStructMemberAccess(syntax, structType, out var result)) {
                        return result;
                    }
                    else {
                        this.context.AliasTracker.RegisterMemberAccess(syntax.Result, syntax.Operand, mem.Name);
                    }
                }

                this.Writer.AddLine(new HmmMemberAccess() {
                    Location = syntax.Location,
                    Member = syntax.Member,
                    Operand = syntax.Operand,
                    Result = syntax.Result,
                    IsLValue = syntax.IsLValue
                });

                return TypeCheckResult.NormalFlow(syntax.Result);
            }
            else {
                throw TypeCheckException.UnexpectedType(syntax.Location, targetType);
            }
        }

        public TypeCheckResult VisitNew(HmmNewSyntax syntax) {
            // For simple stuff let the unifier deal with it
            if (syntax.Type == VoidType.Instance || syntax.Type == WordType.Instance || syntax.Type == BoolType.Instance) {
                var result = this.Unifier.Convert("void", syntax.Type, syntax.Location);

                this.Writer.AddLine(new HmmVariableStatement() {
                    Location = syntax.Location,
                    IsMutable = false,
                    Value = result,
                    Variable = syntax.Result
                });

                this.context.AliasTracker.RegisterLocal(syntax.Result, syntax.Type);

                return TypeCheckResult.NormalFlow(syntax.Result);
            }
            else if (syntax.Type is ArrayType arraySig) {
                return this.TypeCheckNewArray(syntax, arraySig);
            }
            else if (syntax.Type.GetStructSignature(this.context).TryGetValue(out var structType)) {
                return this.TypeCheckNewStruct(syntax, structType);
            }
            else if (syntax.Type.GetUnionSignature(this.context).TryGetValue(out var unionType)) {
                return this.TypeCheckNewUnion(syntax, unionType);
            }

            throw TypeCheckException.UnexpectedType(syntax.Location, syntax.Type);
        }

        private TypeCheckResult TypeCheckNewArray(HmmNewSyntax syntax, ArrayType sig) {
            if (syntax.Assignments.Count > 0) {
                throw TypeCheckException.NewObjectHasExtraneousFields(syntax.Location, syntax.Type);
            }

            this.Writer.AddLine(new HmmNewSyntax() {
                Location = syntax.Location,
                Result = syntax.Result,
                Type = syntax.Type
            });

            this.context.AliasTracker.RegisterLocal(syntax.Result, syntax.Type);

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        private TypeCheckResult TypeCheckNewStruct(HmmNewSyntax syntax, StructType sig) {
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

            this.context.AliasTracker.RegisterNewStruct(syntax.Result, syntax.Type, newAssignments);

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        private TypeCheckResult TypeCheckNewUnion(HmmNewSyntax syntax, UnionType sig) {
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

            var assignments = new[] {
                new HmmNewFieldAssignment() {
                    Field = name,
                    Value = value
                }
            };

            this.Writer.AddLine(new HmmNewSyntax() {
                Location = syntax.Location,
                Type = syntax.Type,
                Assignments = assignments,
                Result = syntax.Result
            });

            this.context.AliasTracker.RegisterNewUnion(syntax.Result, syntax.Type, value);

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        public TypeCheckResult VisitReturn(HmmReturnSyntax syntax) {
            Assert.IsTrue(this.context.ControlFlowStack.Any());

            var returnType = this.context.ControlFlowStack.Peek().FunctionReturnType;
            var operand = this.Unifier.Convert(syntax.Operand, returnType, syntax.Location);

            this.Writer.AddLine(new HmmReturnSyntax() {
                Location = syntax.Location,
                Operand = operand
            });

            return TypeCheckResult.FunctionReturn;
        }

        public TypeCheckResult VisitStructDeclaration(HmmStructDeclaration syntax) {
            this.Types[new NamedLocation(syntax.Name)] = syntax.Signature;
            this.Writer.AddLine(syntax);

            // Make sure this struct isn't circular
            if (syntax.Type.GetRecursiveFieldTypes(this.context).Contains(syntax.Type)) {
                throw TypeCheckException.CircularValueObject(syntax.Location, syntax.Type);
            }

            return TypeCheckResult.VoidResult;
        }

        public TypeCheckResult VisitTypeDeclaration(HmmTypeDeclaration syntax) {
            this.Writer.AddTypeDeclaration(syntax);

            return TypeCheckResult.VoidResult;
        }

        public TypeCheckResult VisitUnaryOperator(HmmUnaryOperator syntax) {
            Assert.IsFalse(syntax.Operator == UnaryOperatorKind.Plus);
            Assert.IsFalse(syntax.Operator == UnaryOperatorKind.Minus);
            Assert.IsFalse(syntax.Operator == UnaryOperatorKind.AddressOf);
            Assert.IsFalse(syntax.Operator == UnaryOperatorKind.Dereference);

            if (this.eval.TryEvaluateUnarySyntax(syntax, out var eval)) {
                return eval;
            }

            if (syntax.Operator == UnaryOperatorKind.Not) {
                var arg = this.Unifier.Convert(syntax.Operand, BoolType.Instance, syntax.Location);

                this.Writer.AddLine(new HmmUnaryOperator() {
                    Location = syntax.Location,
                    Operand = arg,
                    Operator = syntax.Operator,
                    Result = syntax.Result
                });

                this.context.AliasTracker.RegisterLocal(syntax.Result, BoolType.Instance);

                return TypeCheckResult.NormalFlow(syntax.Result);
            }

            throw TypeCheckException.InvalidUnaryOperator(syntax.Location, this.Types[syntax.Operand], syntax.Operator);
        }

        public TypeCheckResult VisitUnionDeclaration(HmmUnionDeclaration syntax) {
            this.Types[new NamedLocation(syntax.Name)] = syntax.Signature;
            this.Writer.AddLine(syntax);

            // Make sure this struct isn't circular
            if (syntax.Type.GetRecursiveFieldTypes(this.context).Contains(syntax.Type)) {
                throw TypeCheckException.CircularValueObject(syntax.Location, syntax.Type);
            }

            return TypeCheckResult.VoidResult;
        }

        public TypeCheckResult VisitVariableStatement(HmmVariableStatement syntax) {
            var assignType = this.Types[syntax.Value];
            var sigType = assignType.GetSupertype();
            var assign = this.Unifier.Convert(syntax.Value, sigType, syntax.Location);

            this.Writer.AddLine(new HmmVariableStatement() {
                Location = syntax.Location,
                IsMutable = syntax.IsMutable,
                Value = assign,
                Variable = syntax.Variable
            });

            this.context.AliasTracker.RegisterLocal(syntax.Variable, assign);

            return TypeCheckResult.VoidResult;
        }

        public TypeCheckResult VisitDereference(HmmDereference syntax) {
            var operandType = this.Types[syntax.Operand];

            if (!operandType.GetPointerSignature(this.context).TryGetValue(out var sig)) {
                throw TypeCheckException.ExpectedPointerType(syntax.Location, operandType);
            }

            if (!syntax.IsLValue && this.eval.TryEvaluateRValueDereference(syntax, out var result)) {
                return result;
            }

            this.Writer.AddLine(new HmmDereference() {
                Location = syntax.Location,
                IsLValue = syntax.IsLValue,
                Operand = syntax.Operand,
                Result = syntax.Result
            });

            if (syntax.IsLValue) {
                this.context.AliasTracker.RegisterDereferencedPointerReference(syntax.Result, sig.InnerType, syntax.Operand);
            }
            else {
                this.context.AliasTracker.RegisterDereferencedPointer(syntax.Result, sig.InnerType, syntax.Operand);
            }

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        public TypeCheckResult VisitIndex(HmmIndex syntax) {
            var opType = this.Types[syntax.Operand];

            if (!opType.GetArraySignature(this.context).TryGetValue(out var arrayType)) {
                throw TypeCheckException.ExpectedArrayType(syntax.Location, opType);
            }

            var index = this.Unifier.Convert(syntax.Index, WordType.Instance, syntax.Location);

            this.Writer.AddLine(new HmmIndex() {
                Location = syntax.Location,
                Operand = syntax.Operand,
                Index = index,
                Result = syntax.Result,
                IsLValue = syntax.IsLValue
            });

            if (syntax.IsLValue) {
                this.context.AliasTracker.RegisterArrayIndexReference(syntax.Result, syntax.Operand, index, arrayType.InnerType);
            }
            else {
                this.context.AliasTracker.RegisterArrayIndex(syntax.Result, syntax.Operand, index, arrayType.InnerType);
            }

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        public TypeCheckResult VisitAddressOf(HmmAddressOf syntax) {
            var operandType = this.Types[syntax.Operand];

            // The name resolver has already confirmed that our operand is a real lvalue, so no need to check that

            this.Writer.AddLine(new HmmAddressOf() {
                Location = syntax.Location,
                Operand = syntax.Operand,
                Result = syntax.Result
            });

            var returnType = new PointerType() { InnerType = operandType };

            this.context.AliasTracker.RegisterAddressOf(syntax.Result, syntax.Operand);

            return TypeCheckResult.NormalFlow(syntax.Result);
        }

        public TypeCheckResult CheckBody(IReadOnlyList<IHmmSyntax> stats) {
            var resultFlow = ControlFlow.NormalFlow;

            foreach (var stat in stats) {
                var statResult = stat.Accept(this);

                if (statResult.ControlFlow.DoesJump) {
                    return new TypeCheckResult("void", statResult.ControlFlow);
                }

                resultFlow = resultFlow.Merge(statResult.ControlFlow);
            }

            return new TypeCheckResult("void", resultFlow);
        }
    }
}
