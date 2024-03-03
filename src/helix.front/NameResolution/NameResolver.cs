using Helix.Common;
using Helix.Common.Hmm;
using Helix.Common.Tokens;
using Helix.Common.Types;
using Helix.Frontend.ParseTree;
using System.Linq.Expressions;

namespace Helix.Frontend.NameResolution {
    internal class NameResolver : IParseTreeVisitor<string> {
        private readonly Stack<IdentifierPath> scopes = [];
        private readonly Stack<HmmWriter> writers = [];
        private readonly Stack<bool> expectedLValue = [];
        private readonly NameMangler mangler;
        private readonly DeclarationStore declarations;

        private IdentifierPath Scope => this.scopes.Peek();

        private HmmWriter Writer => this.writers.Peek();

        public IReadOnlyList<IHmmSyntax> Result => this.Writer.AllLines;

        public bool ExpectedLValue => this.expectedLValue.Peek();

        public NameResolver(DeclarationStore declarations, NameMangler mangler) {
            this.declarations = declarations;
            this.mangler = mangler;

            this.scopes.Push(new IdentifierPath());
            this.writers.Push(new HmmWriter());
            this.expectedLValue.Push(false);
        }

        private TypeNameResolver GetTypeNameResolver(TokenLocation location) {
            return new TypeNameResolver(this.Scope, this.declarations, this.mangler, location);
        }

        public string VisitArrayLiteral(ArrayLiteral syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var items = syntax.Args.Select(x => x.Accept(this)).ToArray();
            var result = this.mangler.CreateMangledTempName(this.Scope);

            this.Writer.AddLine(new HmmArrayLiteral() {
                Location = syntax.Location,
                Args = items,
                Result = result
            });

            return result;
        }

        public string VisitAs(AsSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var resolvedType = syntax.Type.Accept(this.GetTypeNameResolver(syntax.Location));
            var target = syntax.Operand.Accept(this);
            var result = this.mangler.CreateMangledTempName(this.Scope);

            this.Writer.AddLine(new HmmAsSyntax() {
                Location = syntax.Location,
                Operand = target,
                Result = result,
                Type = resolvedType
            });

            return result;
        }

        public string VisitAssignment(AssignmentStatement syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            this.expectedLValue.Push(true);
            var target = syntax.Target.Accept(this);
            this.expectedLValue.Pop();

            var assign = syntax.Assign.Accept(this);

            this.Writer.AddLine(new HmmAssignment() {
                Location = syntax.Location,
                Variable = target,
                Value = assign
            });

            return "void";
        }

        public string VisitBinarySyntax(BinarySyntax syntax) {
            if (syntax.Operator == BinaryOperationKind.Index) {
                return this.ResolveIndex(syntax);
            }

            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

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
            var result = this.mangler.CreateMangledTempName(this.Scope);

            this.Writer.AddLine(new HmmBinarySyntax() {
                Location = syntax.Location,
                Left = left,
                Right = right,
                Operator = syntax.Operator,
                Result = result
            });

            return result;
        }

        private string ResolveIndex(BinarySyntax syntax) {
            Assert.IsTrue(syntax.Operator == BinaryOperationKind.Index);

            this.expectedLValue.Push(false);

            var arg = syntax.Left.Accept(this);
            var index = syntax.Right.Accept(this);
            var result = this.mangler.CreateMangledTempName(this.Scope);

            this.expectedLValue.Pop();

            this.Writer.AddLine(new HmmIndex() {
                Location = syntax.Location,
                IsLValue = this.ExpectedLValue,
                Operand = arg,
                Index = index,
                Result = result
            });

            return result;
        }

        public string VisitBlock(BlockSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var scopeName = this.mangler.CreateMangledTempName(this.Scope, "scope");
            var scopePath = this.Scope.Append(scopeName);

            // Just push a scope here but not a new set of lines, since we're flattening blocks

            this.scopes.Push(scopePath);
            var stats = syntax.Statements.Select(x => x.Accept(this)).ToArray();
            this.scopes.Pop();

            return stats.LastOrDefault() ?? "void";
        }

        public string VisitBoolLiteral(BoolLiteral syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            return syntax.Value.ToString().ToLower();
        }

        public string VisitBreak(BreakSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            this.Writer.AddLine(new HmmBreakSyntax() { Location = syntax.Location });

            return "void";
        }

        public string VisitContinue(ContinueSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            this.Writer.AddLine(new HmmContinueSyntax() { Location = syntax.Location });

            return "void";
        }

        public string VisitFor(ForSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

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
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var funcPath = this.Scope.Append(syntax.Name);

            this.scopes.Push(funcPath);
            this.writers.Push(this.Writer.CreateScope());

            foreach (var par in syntax.Signature.Parameters) {
                var parPath = this.Scope.Append(par.Name);

                this.declarations.SetDeclaration(parPath);
                this.mangler.MangleLocalName(parPath);
            }

            // Declare the function again in case we're not at the top level
            var funcType = (FunctionType)syntax.Signature.Accept(this.GetTypeNameResolver(syntax.Location));
            this.declarations.SetDeclaration(funcPath, funcType);

            syntax.Body.Accept(this);

            var bodyLines = this.writers.Pop().ScopedLines;
            this.scopes.Pop();

            this.Writer.AddTypeDeclaration(new HmmTypeDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Kind = TypeDeclarationKind.Function
            });

            this.Writer.AddFowardDeclaration(new HmmFunctionForwardDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Signature = funcType
            });

            this.Writer.AddLine(new HmmFunctionDeclaration() {
                Location = syntax.Location,
                Signature = funcType,
                Name = syntax.Name,
                Body = bodyLines
            });

            return "void";
        }

        public string VisitIf(IfSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var cond = syntax.Condition.Accept(this);

            var affirmName = this.mangler.MangleLocalName(this.Scope, "if_true");
            var affirmPath = this.Scope.Append(affirmName);

            this.scopes.Push(affirmPath);
            this.writers.Push(this.Writer.CreateScope());

            var affirm = syntax.Affirmative.Accept(this);

            var affirmLines = this.writers.Pop().ScopedLines;
            this.scopes.Pop();

            if (syntax.Negative.TryGetValue(out var negTree)) {
                var negName = this.mangler.MangleLocalName(this.Scope, "if_false");
                var negPath = this.Scope.Append(negName);

                this.scopes.Push(negPath);
                this.writers.Push(this.Writer.CreateScope());

                var negative = negTree.Accept(this);

                var negLines = this.writers.Pop().ScopedLines;
                this.scopes.Pop();

                var result = this.mangler.CreateMangledTempName(this.Scope);

                this.Writer.AddLine(new HmmIfExpression() {
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
                var result = this.mangler.CreateMangledTempName(this.Scope);

                this.Writer.AddLine(new HmmIfExpression() {
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
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var target = syntax.Target.Accept(this);
            var args = syntax.Args.Select(x => x.Accept(this)).ToArray();
            var result = this.mangler.CreateMangledTempName(this.Scope);

            this.Writer.AddLine(new HmmInvokeSyntax() {
                Location = syntax.Location,
                Target = target,
                Arguments = args,
                Result = result
            });

            return result;
        }

        public string VisitIs(IsSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var arg = syntax.Operand.Accept(this);
            var result = this.mangler.CreateMangledTempName(this.Scope);

            this.Writer.AddLine(new HmmIsSyntax() {
                Operand = arg,
                Field = syntax.Field,
                Location = syntax.Location,
                Result = result
            });

            return result;
        }

        public string VisitMemberAccess(MemberAccessSyntax syntax) {
            var arg = syntax.Target.Accept(this);
            var result = this.mangler.CreateMangledTempName(this.Scope);

            this.Writer.AddLine(new HmmMemberAccess() {
                Location = syntax.Location,
                Operand = arg,
                Member = syntax.Field,
                Result = result,
                IsLValue = this.ExpectedLValue
            });

            return result;
        }

        public string VisitNew(NewSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var type = syntax.Type.Accept(this.GetTypeNameResolver(syntax.Location));
            var result = this.mangler.CreateMangledTempName(this.Scope);

            var fields = syntax.Assignments
                .Select(x => new HmmNewFieldAssignment() {
                    Field = x.Name,
                    Value = x.Value.Accept(this)
                })
                .ToArray();

            this.Writer.AddLine(new HmmNewSyntax() {
                Location = syntax.Location,
                Assignments = fields,
                Result = result,
                Type = type
            });

            return result;
        }

        public string VisitReturn(ReturnSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var target = syntax.Payload.Accept(this);

            this.Writer.AddLine(new HmmReturnSyntax() {
                Location = syntax.Location,
                Operand = target
            });

            return "void";
        }

        public string VisitStructDeclaration(StructDeclaration syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var structType = (StructType)syntax.Signature.Accept(this.GetTypeNameResolver(syntax.Location));

            this.declarations.SetDeclaration(this.Scope, syntax.Name, structType);

            var mangled = this.mangler.GetMangledName(this.Scope, syntax.Name);
            var nominalType = new NominalType() { Name = mangled, DisplayName = syntax.Name };

            this.Writer.AddTypeDeclaration(new HmmTypeDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Kind = TypeDeclarationKind.Struct
            });

            this.Writer.AddLine(new HmmStructDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Signature = structType,
                Type = nominalType
            });

            return "void";
        }

        public string VisitUnarySyntax(UnarySyntax syntax) {
            if (syntax.Operator == UnaryOperatorKind.AddressOf) {
                return this.ResolveAddressOf(syntax);
            }
            else if (syntax.Operator == UnaryOperatorKind.Dereference) {
                return this.ResolveDereference(syntax);
            }

            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            // Lower plus and minus unary operators to a binary operator
            if (syntax.Operator == UnaryOperatorKind.Plus || syntax.Operator == UnaryOperatorKind.Minus) {
                var op = syntax.Operator == UnaryOperatorKind.Plus
                    ? BinaryOperationKind.Add
                    : BinaryOperationKind.Subtract;

                var newSyntax = new BinarySyntax() {
                    Location = syntax.Location,
                    Left = new WordLiteral() {
                        Location = syntax.Location,
                        Value = 0
                    },
                    Right = syntax.Operand,
                    Operator = op
                };

                return newSyntax.Accept(this);
            }

            var arg = syntax.Operand.Accept(this);
            var result = this.mangler.CreateMangledTempName(this.Scope);

            this.Writer.AddLine(new HmmUnaryOperator() {
                Location = syntax.Location,
                Operand = arg,
                Operator = syntax.Operator,
                Result = result
            });

            return result;
        }

        private string ResolveAddressOf(UnarySyntax syntax) {
            Assert.IsTrue(syntax.Operator == UnaryOperatorKind.AddressOf);

            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            this.expectedLValue.Push(true);

            var arg = syntax.Operand.Accept(this);
            var result = this.mangler.CreateMangledTempName(this.Scope);

            this.expectedLValue.Pop();

            this.Writer.AddLine(new HmmAddressOf() {
                Location = syntax.Location,
                Operand = arg,
                Result = result
            });

            return result;
        }

        private string ResolveDereference(UnarySyntax syntax) {
            Assert.IsTrue(syntax.Operator == UnaryOperatorKind.Dereference);

            this.expectedLValue.Push(false);

            var arg = syntax.Operand.Accept(this);
            var result = this.mangler.CreateMangledTempName(this.Scope);

            this.expectedLValue.Pop();

            this.Writer.AddLine(new HmmDereference() {
                Location = syntax.Location,
                IsLValue = this.ExpectedLValue,
                Operand = arg,
                Result = result
            });

            return result;
        }

        public string VisitUnionDeclaration(UnionDeclaration syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var unionType = (UnionType)syntax.Signature.Accept(this.GetTypeNameResolver(syntax.Location));

            this.declarations.SetDeclaration(this.Scope, syntax.Name, unionType);

            var mangled = this.mangler.GetMangledName(this.Scope, syntax.Name);
            var nominalType = new NominalType() { Name = mangled, DisplayName = syntax.Name };

            this.Writer.AddTypeDeclaration(new HmmTypeDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Kind = TypeDeclarationKind.Union
            });

            this.Writer.AddLine(new HmmUnionDeclaration() {
                Location = syntax.Location,
                Name = syntax.Name,
                Signature = unionType,
                Type = nominalType
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
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            this.declarations.SetDeclaration(this.Scope, syntax.VariableName);

            var mangled = this.mangler.MangleLocalName(this.Scope, syntax.VariableName);
            var value = syntax.Value.Accept(this);

            this.Writer.AddLine(new HmmVariableStatement() {
                Location = syntax.Location,
                IsMutable = true,
                Value = value,
                Variable = mangled
            });

            return "void";
        }

        public string VisitVoidLiteral(VoidLiteral syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            return "void";
        }

        public string VisitWhile(WhileSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

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

        public string VisitWordLiteral(WordLiteral syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            return syntax.Value.ToString();
        }

        public string VisitLoop(LoopSyntax syntax) {
            if (this.ExpectedLValue) {
                throw NameResolutionException.ExpectedLValue(syntax.Location);
            }

            var loopName = this.mangler.MangleLocalName(this.Scope, "loop");
            var loopPath = this.Scope.Append(loopName);

            this.scopes.Push(loopPath);
            this.writers.Push(this.Writer.CreateScope());

            syntax.Body.Accept(this);

            var loopLines = this.writers.Pop().ScopedLines;
            this.scopes.Pop();

            this.Writer.AddLine(new HmmLoopSyntax() {
                Location = syntax.Location,
                Body = loopLines
            });

            return "void";
        }
    }
}
