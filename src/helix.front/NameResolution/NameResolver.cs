using helix.common;
using helix.common.Hmm;
using Helix.Analysis.Types;
using Helix.Frontend.ParseTree;
using Helix.HelixMinusMinus;
using Helix.Parsing;

namespace Helix.Frontend.NameResolution {
    internal class NameResolver : IParseTreeVisitor<string> {
        private readonly HmmWriter writer = new();
        private readonly Stack<IdentifierPath> scopes = new();
        private readonly NameMangler mangler = new();
        private readonly DeclarationStore declarations;

        private IdentifierPath Scope => this.scopes.Peek();

        public IReadOnlyList<IHmmSyntax> Result => this.writer.Lines;

        public NameResolver(DeclarationStore declarations) {
            this.declarations = declarations;

            this.scopes.Push(new IdentifierPath());
        }

        private TypeNameResolver GetTypeNameResolver(TokenLocation location) {
            return new TypeNameResolver(this.Scope, this.declarations, this.mangler, location);
        }

        public string VisitArrayLiteral(ArrayLiteral syntax) {
            var items = syntax.Args.Select(x => x.Accept(this)).ToArray();
            var result = this.mangler.MangleTempName(this.Scope);

            this.writer.AddLine(new HmmArrayLiteral() {
                Location = syntax.Location,
                Args = items,
                Result = result
            });

            return result;
        }

        public string VisitAs(AsSyntax syntax) {
            var resolvedType = syntax.Type.Accept(this.GetTypeNameResolver(syntax.Location));
            var target = syntax.Operand.Accept(this);
            var result = this.mangler.MangleTempName(this.Scope);

            this.writer.AddLine(new HmmAsSyntax() {
                Location = syntax.Location,
                Operand = target,
                Result = result,
                Type = resolvedType
            });

            return result;
        }

        public string VisitAssignment(AssignmentStatement syntax) {
            var target = syntax.Target.Accept(this);
            var assign = syntax.Assign.Accept(this);

            this.writer.AddLine(new HmmAssignment() {
                Location = syntax.Location,
                Variable = target,
                Value = assign
            });

            return "void";
        }

        public string VisitBinarySyntax(BinarySyntax syntax) {
            // Translate branching boolean instructions to proper if statements
            if (syntax.Operator == BinaryOperationKind.BranchingAnd) {
                var newSyntax = new IfSyntax() {
                    Location = syntax.Location,
                    Condition = new UnarySyntax() {
                        Location = syntax.Left.Location,
                        Operand = syntax.Left,
                        Operator = UnaryOperatorKind.Not
                    },
                    Affirmative = new BoolLiteral() {
                        Location = syntax.Left.Location,
                        Value = false
                    },
                    Negative = Option.Some(syntax.Right)
                };

                return newSyntax.Accept(this);
            }
            else if (syntax.Operator == BinaryOperationKind.BranchingOr) {
                var newSyntax = new IfSyntax() {
                    Location = syntax.Location,
                    Condition = syntax.Left,
                    Affirmative = new BoolLiteral() {
                        Location = syntax.Left.Location,
                        Value = true
                    },
                    Negative = Option.Some(syntax.Right)
                };

                return newSyntax.Accept(this);
            }

            var left = syntax.Left.Accept(this);
            var right = syntax.Right.Accept(this);
            var result = this.mangler.MangleTempName(this.Scope);

            this.writer.AddLine(new HmmBinaryOperator() {
                Location = syntax.Location,
                Left = left,
                Right = right,
                Operator = syntax.Operator,
                Result = result
            });

            return result;
        }

        public string VisitBlock(BlockSyntax syntax) {
            var scopeName = this.mangler.MangleTempName(this.Scope, "scope");
            var scopePath = this.Scope.Append(scopeName);

            // Just push a scope here but not a new set of lines, since we're flattening blocks

            this.scopes.Push(scopePath);
            var stats = syntax.Statements.Select(x => x.Accept(this)).ToArray();
            this.scopes.Pop();

            return stats.LastOrDefault() ?? "void";
        }

        public string VisitBoolLiteral(BoolLiteral syntax) => syntax.Value.ToString().ToLower();

        public string VisitBreak(BreakSyntax syntax) {
            this.writer.AddLine(new HmmBreakSyntax() { Location = syntax.Location });

            return "void";
        }

        public string VisitContinue(ContinueSyntax syntax) {
            this.writer.AddLine(new HmmContinueSyntax() { Location = syntax.Location });

            return "void";
        }

        public string VisitFor(ForSyntax syntax) {
            var endIndex = new AsSyntax() {
                Location = syntax.FinalValue.Location,
                Operand = syntax.FinalValue,
                Type = new WordType()
            };

            var counterDeclaration = new VariableStatement() {
                Location = syntax.Location,
                VariableName = syntax.Variable,
                Value = new AsSyntax() {
                    Location = syntax.InitialValue.Location,
                    Operand = syntax.InitialValue,
                    Type = new WordType()
                }
            };

            var counterAccess = new VariableAccess() {
                Location = syntax.Location,
                VariableName = syntax.Variable
            };

            var counterIncrement = new AssignmentStatement() {
                Location = syntax.Location,
                Target = counterAccess,
                Assign = new BinarySyntax() {
                    Location = syntax.Location,
                    Left = counterAccess,
                    Right = new WordLiteral() {
                        Location = syntax.Location,
                        Value = 1
                    },
                    Operator = BinaryOperationKind.Add
                }
            };

            var test = new IfSyntax() {
                Location = syntax.Location,
                Condition = new BinarySyntax() {
                    Location = syntax.Location,
                    Left = counterAccess,
                    Right = endIndex,
                    Operator = syntax.Inclusive
                        ? BinaryOperationKind.GreaterThan
                        : BinaryOperationKind.GreaterThanOrEqualTo
                },
                Affirmative = new BreakSyntax() {
                    Location = syntax.Location
                }
            };

            var loopBlock = new BlockSyntax() {
                Location = syntax.Location,
                Statements = [test, syntax.Body, counterIncrement]
            };

            var loop = new LoopSyntax() {
                Location = syntax.Location,
                Body = loopBlock
            };

            counterDeclaration.Accept(this);
            loop.Accept(this);

            return "void";
        }

        public string VisitFunctionDeclaration(FunctionDeclaration syntax) {
            var funcPath = this.Scope.Append(syntax.Name);

            this.mangler.MangleGlobalName(funcPath);

            this.scopes.Push(funcPath);
            this.writer.PushBlock();

            foreach (var par in syntax.Signature.Parameters) {
                var parPath = this.Scope.Append(par.Name);

                this.declarations.SetDeclaration(parPath);
                this.mangler.MangleLocalName(parPath);
            }

            // Declare the function again in case we're not at the top level
            var funcType = (FunctionType)syntax.Signature.Accept(this.GetTypeNameResolver(syntax.Location));
            this.declarations.SetDeclaration(funcPath, funcType);

            syntax.Body.Accept(this);

            var bodyLines = this.writer.PopBlock();
            this.scopes.Pop();

            this.writer.AddTypeDeclaration(new HmmTypeDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Kind = TypeDeclarationKind.Function
            });

            this.writer.AddFowardDeclaration(new HmmFunctionForwardDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Signature = funcType
            });

            this.writer.AddLine(new HmmFunctionDeclaration() {
                Location = syntax.Location,
                Signature = funcType,
                Name = syntax.Name,
                Body = bodyLines
            });

            return "void";
        }

        public string VisitIf(IfSyntax syntax) {
            var cond = syntax.Condition.Accept(this);

            var affirmName = this.mangler.MangleLocalName(this.Scope, "if_true");
            var affirmPath = this.Scope.Append(affirmName);

            this.scopes.Push(affirmPath);
            this.writer.PushBlock();

            var affirm = syntax.Affirmative.Accept(this);

            var affirmLines = this.writer.PopBlock();
            this.scopes.Pop();

            if (syntax.Negative.TryGetValue(out var negTree)) {
                var negName = this.mangler.MangleLocalName(this.Scope, "if_false");
                var negPath = this.Scope.Append(negName);

                this.scopes.Push(negPath);
                this.writer.PushBlock();

                var negative = negTree.Accept(this);

                var negLines = this.writer.PopBlock();
                this.scopes.Pop();

                var result = this.mangler.MangleTempName(this.Scope);

                this.writer.AddLine(new HmmIfExpression() {
                    Location = syntax.Location,
                    Condition = cond,
                    Affirmative = affirm,
                    AffirmativeBody = affirmLines,
                    Negative = negative,
                    NegativeBody = negLines,
                    Result = result
                });

                return result;
            }
            else {
                var result = this.mangler.MangleTempName(this.Scope);

                this.writer.AddLine(new HmmIfExpression() {
                    Condition = cond,
                    Affirmative = "void",
                    AffirmativeBody = affirmLines,
                    Negative = "void",
                    NegativeBody = [],
                    Location = syntax.Location,
                    Result = result
                });

                return "void";
            }
        } 

        public string VisitInvoke(InvokeSyntax syntax) {
            var target = syntax.Target.Accept(this);
            var args = syntax.Args.Select(x => x.Accept(this)).ToArray();
            var result = this.mangler.MangleTempName(this.Scope);

            this.writer.AddLine(new HmmInvokeSyntax() {
                Location = syntax.Location,
                Target = target,
                Arguments = args,
                Result = result
            });

            return result;
        }

        public string VisitIs(IsSyntax syntax) {
            var arg = syntax.Operand.Accept(this);
            var result = this.mangler.MangleTempName(this.Scope);

            this.writer.AddLine(new HmmIsSyntax() {
                Operand = arg,
                Field = syntax.Field,
                Location = syntax.Location,
                Result = result
            });

            return result;
        }

        public string VisitMemberAccess(MemberAccessSyntax syntax) {
            var arg = syntax.Target.Accept(this);
            var result = this.mangler.MangleTempName(this.Scope);

            this.writer.AddLine(new HmmMemberAccess() {
                Location = syntax.Location,
                Operand = arg,
                FieldName = syntax.Field,
                Result = result
            });

            return result;
        }

        public string VisitNew(NewSyntax syntax) {
            var type = syntax.Type.Accept(this.GetTypeNameResolver(syntax.Location));
            var result = this.mangler.MangleTempName(this.Scope);

            var fields = syntax.Assignments
                .Select(x => new HmmNewFieldAssignment() {
                    Field = x.Name,
                    Value = x.Value.Accept(this)
                })
                .ToArray();

            this.writer.AddLine(new HmmNewSyntax() {
                Location = syntax.Location,
                Assignments = fields,
                Result = result,
                Type = type
            });

            return result;
        }

        public string VisitReturn(ReturnSyntax syntax) {
            var target = syntax.Payload.Accept(this);

            this.writer.AddLine(new HmmReturnSyntax() {
                Location = syntax.Location,
                Operand = target
            });

            return "void";
        }

        public string VisitStructDeclaration(StructDeclaration syntax) {
            var structType = (StructType)syntax.Signature.Accept(this.GetTypeNameResolver(syntax.Location));

            this.declarations.SetDeclaration(this.Scope, syntax.Name, structType);
            this.mangler.MangleGlobalName(this.Scope, syntax.Name);

            this.writer.AddTypeDeclaration(new HmmTypeDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Kind = TypeDeclarationKind.Struct
            });

            this.writer.AddLine(new HmmStructDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Signature = structType
            });

            return "void";
        }

        public string VisitUnarySyntax(UnarySyntax syntax) {
            var arg = syntax.Operand.Accept(this);
            var result = this.mangler.MangleTempName(this.Scope);

            this.writer.AddLine(new HmmUnaryOperator() {
                Location = syntax.Location,
                Operand = arg,
                Operator = syntax.Operator,
                Result = result
            });

            return result;
        }

        public string VisitUnionDeclaration(UnionDeclaration syntax) {
            var unionType = (UnionType)syntax.Signature.Accept(this.GetTypeNameResolver(syntax.Location));

            this.declarations.SetDeclaration(this.Scope, syntax.Name, unionType);
            this.mangler.MangleGlobalName(this.Scope, syntax.Name);

            this.writer.AddTypeDeclaration(new HmmTypeDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Kind = TypeDeclarationKind.Union
            });

            this.writer.AddLine(new HmmUnionDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Signature = unionType
            });

            return "void";
        }

        public string VisitVariableAccess(VariableAccess syntax) {
            if (!this.declarations.ResolveDeclaration(this.Scope, syntax.VariableName).TryGetValue(out var path)) {
                throw NameResolutionException.IdentifierUndefined(syntax.Location, syntax.VariableName);
            }

            return this.mangler.GetMangledName(path);
        }

        public string VisitVariableStatement(VariableStatement syntax) {
            this.declarations.SetDeclaration(this.Scope, syntax.VariableName);

            var mangled = this.mangler.MangleLocalName(this.Scope, syntax.VariableName);
            var value = syntax.Value.Accept(this);

            this.writer.AddLine(new HmmVariableStatement() {
                Location = syntax.Location,
                IsMutable = true,
                Value = value,
                Variable = mangled
            });

            return "void";
        }

        public string VisitVoidLiteral(VoidLiteral syntax) {
            return "void";
        }

        public string VisitWhile(WhileSyntax syntax) {
            var test = new IfSyntax() {
                Location = syntax.Condition.Location,
                Condition = new UnarySyntax() {
                    Location = syntax.Condition.Location,
                    Operand = syntax.Condition,
                    Operator = UnaryOperatorKind.Not
                },
                Affirmative = new BreakSyntax() {
                    Location = syntax.Condition.Location
                }
            };

            var body = new BlockSyntax() {
                Location = syntax.Body.Location,
                Statements = [test, syntax.Body]
            };

            var loop = new LoopSyntax() {
                Location = syntax.Location,
                Body = body
            };

            return loop.Accept(this);
        }

        public string VisitWordLiteral(WordLiteral syntax) => syntax.Value.ToString();

        public string VisitLoop(LoopSyntax syntax) {
            var loopName = this.mangler.MangleLocalName(this.Scope, "loop");
            var loopPath = this.Scope.Append(loopName);

            this.scopes.Push(loopPath);
            this.writer.PushBlock();

            syntax.Body.Accept(this);

            var loopLines = this.writer.PopBlock();
            this.scopes.Pop();

            this.writer.AddLine(new HmmLoopSyntax() {
                Location = syntax.Location,
                Body = loopLines
            });

            return "void";
        }
    }
}
