using Attempt16.Syntax;
using Attempt16.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Attempt16.Analysis {
    public class TypeChecker {
        public CompilationUnit CompilationUnit { get; }

        public TypeChecker(CompilationUnit unit) {
            this.CompilationUnit = unit;
        }

        public Scope TypeCheck() {
            // First make sure there are no duplicate names
            if (this.CompilationUnit.Declarations.Select(x => x.Name).Distinct().Count() != this.CompilationUnit.Declarations.Count) {
                throw new Exception();
            }

            var scope = new Scope()
                .AppendTypeVariable("int", new IntType())
                .AppendTypeVariable("void", new VoidType());

            foreach (var decl in this.CompilationUnit.Declarations) {
                if (decl is FunctionDeclaration funcDecl) {
                    var type = new SingularFunctionType() {
                        Parameters = funcDecl.Parameters,
                        ScopePath = new IdentifierPath(funcDecl.Name),
                        ReturnTypePath = funcDecl.ReturnType
                    };

                    scope = scope.AppendVariable(
                        funcDecl.Name,
                        new VariableInfo() {
                            ScopePath = new IdentifierPath(funcDecl.Name),
                            Source = VariableSource.Local,
                            Type = type
                        }
                    );
                }
                else if (decl is StructDeclaration structDecl) {
                    scope = scope.AppendTypeVariable(
                        structDecl.Name,
                        new SingularStructType() {
                            Name = structDecl.Name,
                            Members = structDecl.Members,
                            ScopePath = new IdentifierPath(structDecl.Name)
                        }
                    );
                }
            }

            // Check each declaration individually
            var checker = new ExpressionTypeChecker(scope);

            foreach (var decl in this.CompilationUnit.Declarations) {
                decl.Accept(checker);
            }

            return scope;
        }
    }

    public class ExpressionTypeChecker : ISyntaxVisitor<int>, IDeclarationVisitor<int> {
        private readonly Stack<Scope> scopes = new Stack<Scope>();
        private readonly Stack<int> currentBlockId = new Stack<int>();
        private readonly TypeUnifier unifier = new TypeUnifier();

        public ExpressionTypeChecker(Scope scope) {
            this.scopes.Push(scope);
        }

        public int VisitBlock(BlockSyntax syntax) {
            if (syntax.Statements.Any()) {
                // Get the id for this scope
                int id = this.currentBlockId.Pop();
                this.currentBlockId.Push(id + 1);

                // Start a new scope
                var scope = this.scopes.Peek().SelectPath(x => x.Append(id.ToString()));
                this.scopes.Push(scope);

                // Start the block counter over for blocks within this block
                this.currentBlockId.Push(0);

                foreach (var stat in syntax.Statements) {
                    stat.Accept(this);
                }

                // Make sure the returned value isn't using a reference from something inside this scope
                syntax.ReturnCapturedVariables = syntax.Statements.Last().ReturnCapturedVariables;

                if (syntax.ReturnCapturedVariables.Any(x => x.StartsWith(scope.Path))) {
                    throw new Exception();
                }

                // Close out the scope
                this.scopes.Pop();
                this.currentBlockId.Pop();

                syntax.ReturnType = syntax.Statements.Last().ReturnType;
            }
            else {
                syntax.ReturnType = new VoidType();
            }

            return 0;
        }

        public int VisitVariableEquate(VariableStatement syntax) {
            if (this.scopes.Peek().FindVariable(syntax.VariableName).Any()) {
                throw new Exception("Variable already exists");
            }

            syntax.Value.Accept(this);

            // The other side must be a variable or reference
            if (!(syntax.Value.ReturnType is VariableType varType)) {
                throw new Exception("Equated type must be a variable type");
            }

            if (syntax.VarCount != this.GetCorrectVarCount(varType) - 1) {
                throw new Exception();
            }

            // Don't add this "variable" as captured, instead use the captured variables of whatever
            // is being aliased
            syntax.ReturnType = varType.TargetType;
            syntax.ReturnCapturedVariables = syntax.Value.ReturnCapturedVariables;

            var path = this.scopes.Peek().Path.Append(syntax.VariableName);

            var info = new VariableInfo() {
                Source = VariableSource.Alias,
                Type = syntax.ReturnType,
                ScopePath = path
            };

            this.scopes.Push(this.scopes.Pop().AppendVariable(syntax.VariableName, info));

            return 0;
        }

        public int VisitFunctionDeclaration(FunctionDeclaration decl) {
            this.currentBlockId.Push(0);

            var varInfo = this.scopes.Peek().FindVariable(decl.Name);
            if (!varInfo.Any()) {
                throw new Exception();
            }

            var funcType = (SingularFunctionType)varInfo.GetValue().Type;
            decl.FunctionType = funcType;

            // Start a new scope
            var scope = this.scopes.Peek().SelectPath(x => x.Append(decl.Name));

            // Add the function parameters to the scope
            foreach (var par in decl.Parameters) {
                var parType = this.scopes.Peek().FindType(par.TypePath).GetValue();

                if (parType is VariableType remType) {
                    scope = scope.AppendVariable(par.Name, new VariableInfo() {
                        Type = remType.TargetType,
                        Source = VariableSource.Alias,
                        ScopePath = scope.Path.Append(par.Name)
                    });
                }
                else {
                    scope = scope.AppendVariable(par.Name, new VariableInfo() {
                        Type = parType,
                        Source = VariableSource.ValueParameter,
                        ScopePath = scope.Path.Append(par.Name)
                    });
                }
            }

            this.scopes.Push(scope);

            // Analyze the body
            decl.Body.Accept(this);

            // Make sure the return type matches
            decl.Body = this.unifier.UnifyTo(decl.Body, this.scopes.Peek().FindType(decl.ReturnType).GetValue());

            // Close the scope
            this.scopes.Pop();
            this.currentBlockId.Pop();

            return 0;
        }

        public int VisitIf(IfSyntax syntax) {
            syntax.Condition.Accept(this);
            syntax.Condition = this.unifier.UnifyTo(syntax.Condition, new IntType());
            syntax.Affirmative.Accept(this);

            if (syntax.Negative == null) {
                syntax.ReturnType = new VoidType();
            }
            else {
                syntax.Negative.Accept(this);

                syntax.Negative = this.unifier.UnifyTo(syntax.Negative, syntax.Affirmative.ReturnType);
                syntax.ReturnType = syntax.Affirmative.ReturnType;
                syntax.ReturnCapturedVariables.UnionWith(syntax.Affirmative.ReturnCapturedVariables);
                syntax.ReturnCapturedVariables.UnionWith(syntax.Negative.ReturnCapturedVariables);
            }

            return 0;
        }

        public int VisitIntLiteral(IntLiteral syntax) {
            syntax.ReturnType = new IntType();
            return 0;
        }

        public int VisitStore(StoreSyntax syntax) {
            syntax.Target.Accept(this);

            if (!(syntax.Target.ReturnType is VariableType vartype)) {
                throw new Exception();
            }

            syntax.Value.Accept(this);

            // Make sure the types match
            syntax.Value = this.unifier.UnifyTo(syntax.Value, vartype.TargetType);

            // Sanity check
            if (syntax.Target.ReturnCapturedVariables.Count != 1) {
                throw new Exception();
            }

            // Make sure the expression being stored does not capture itself
            var variablePath = syntax.Target.ReturnCapturedVariables.First();
            if (syntax.Value.ReturnCapturedVariables.Contains(variablePath)) {
                throw new Exception();
            }

            // Make sure the thing being stored will outlive the variable
            var varScope = variablePath.Pop();

            foreach (var cap in syntax.Value.ReturnCapturedVariables) {
                if (cap.StartsWith(varScope)) {
                    var capScope = cap.Pop();

                    if (!varScope.StartsWith(capScope)) {
                        throw new Exception();
                    }
                }
            }

            syntax.ReturnType = new VoidType();

            return 0;
        }

        public int VisitVariableStore(VariableStatement syntax) {
            if (this.scopes.Peek().FindVariable(syntax.VariableName).Any()) {
                throw new Exception("Variable already exists");
            }

            var path = this.scopes.Peek().Path.Append(syntax.VariableName);

            syntax.Value.Accept(this);
            syntax.ReturnType = syntax.Value.ReturnType;
            syntax.ReturnCapturedVariables.Add(path);

            if (syntax.VarCount != this.GetCorrectVarCount(syntax.Value.ReturnType)) {
                throw new Exception();
            }

            if (syntax.ReturnType.Equals(new VoidType())) {
                throw new Exception("Void variables are not valid");
            }

            var info = new VariableInfo() {
                Source = VariableSource.Local,
                Type = syntax.ReturnType,
                ScopePath = path
            };

            this.scopes.Push(this.scopes.Pop().AppendVariable(syntax.VariableName, info));

            return 0;
        }

        public int VisitVariableLiteral(VariableLiteral syntax) {
            if (!this.scopes.Peek().FindVariable(syntax.VariableName).TryGetValue(out var info)) {
                CompilerErrors.UndeclaredVariable(syntax.VariableName);
            }

            if (info.Type is VariableType rem) {
                syntax.ReturnType = rem.TargetType;
                syntax.Source = info.Source;
                syntax.ReturnCapturedVariables.Add(info.ScopePath);
            }
            else {
                syntax.ReturnType = info.Type;
                syntax.Source = info.Source;
            }

            return 0;
        }

        public int VisitVariableLocationLiteral(VariableLocationLiteral syntax) {
            if (!this.scopes.Peek().FindVariable(syntax.VariableName).TryGetValue(out var info)) {
                CompilerErrors.UndeclaredVariable(syntax.VariableName);
            }

            if (info.Source == VariableSource.ValueParameter) {
                throw new Exception();
            }

            syntax.ReturnType = new VariableType(info.Type);
            syntax.Source = info.Source;
            syntax.ReturnCapturedVariables.Add(info.ScopePath);

            return 0;
        }

        public int VisitValueof(ValueofSyntax syntax) {
            syntax.Value.Accept(this);

            ILanguageType target;
            if (syntax.Value.ReturnType is VariableType varType) {
                target = varType.TargetType;
            }
            else {
                throw new Exception();
            }


            if (target is IntType) {
                syntax.ReturnType = target;
            }
            else if (target is VariableType) {
                syntax.ReturnType = target;
            }

            return 0;
        }

        private int GetCorrectVarCount(ILanguageType type) {
            if (type is VariableType rem) {
                return 1 + this.GetCorrectVarCount(rem.TargetType);
            }
            else {
                return 1;
            }
        }

        public int VisitBinaryExpression(BinaryExpression syntax) {
            syntax.Left.Accept(this);
            syntax.Right.Accept(this);

            if (!(syntax.Left.ReturnType is IntType)) {
                throw new Exception();
            }

            if (!(syntax.Right.ReturnType is IntType)) {
                throw new Exception();
            }

            syntax.ReturnType = new IntType();

            return 0;
        }

        public int VisitWhileStatement(WhileStatement syntax) {
            syntax.Condition.Accept(this);
            syntax.Condition = this.unifier.UnifyTo(syntax.Condition, new IntType());

            syntax.Body.Accept(this);
            syntax.ReturnType = new VoidType();

            return 0;
        }

        public int VisitFunctionCall(FunctionCallSyntax syntax) {
            syntax.Target.Accept(this);

            if (!(syntax.Target.ReturnType is SingularFunctionType funcType)) {
                throw new Exception();
            }

            if (funcType.Parameters.Count != syntax.Arguments.Count) {
                throw new Exception();
            }

            foreach (var arg in syntax.Arguments) {
                arg.Accept(this);
            }

            // Find the correct scope
            var path = funcType.ScopePath.Pop();
            var scope = this.scopes.TakeWhile(x => x.Path.StartsWith(path)).Last();

            var zip = syntax.Arguments.Zip(
                funcType.Parameters.Select(x => this.scopes.Peek().FindType(x.TypePath).GetValue()), 
                (x, y) => (x, y));

            var newArgs = new List<ISyntax>();

            foreach (var (value, type2) in zip) {
                newArgs.Add(this.unifier.UnifyTo(value, type2));
            }

            syntax.ReturnType = this.scopes.Peek().FindType(funcType.ReturnTypePath).GetValue();
            syntax.ReturnCapturedVariables = new HashSet<IdentifierPath>();

            foreach (var arg in syntax.Arguments) {
                syntax.ReturnCapturedVariables.UnionWith(arg.ReturnCapturedVariables);
            }

            if (!(syntax.ReturnType is VariableType)) {
                syntax.ReturnCapturedVariables.Clear();
            }

            return 0;
        }

        public int VisitStructDeclaration(StructDeclaration decl) {
            var type = (SingularStructType)this.scopes.Peek().FindType(decl.Name).GetValue();
            decl.StructType = type;

            var detector = new RecursiveStructDetector(this.scopes.Peek());

            if (type.Accept(detector)) {
                throw new Exception();
            }

            return 0;
        }

        public int VisitVariableInitialization(VariableStatement syntax) {
            if (syntax.Operation == DeclarationOperation.Equate) {
                return this.VisitVariableEquate(syntax);
            }
            else {
                return this.VisitVariableStore(syntax);
            }
        }

        public int VisitStructInitialization(StructInitializationSyntax syntax) {
            if (!this.scopes.Peek().FindType(syntax.StructName).TryGetValue(out var syntaxType)) {
                throw new Exception();
            }

            if (!(syntaxType is SingularStructType structType)) {
                throw new Exception();
            }

            var mems = new HashSet<string>(syntax.Members.Select(x => x.MemberName));
            if (!mems.SetEquals(structType.Members.Select(x => x.Name))) {
                throw new Exception();
            }


            // Find the correct scope
            var path = structType.ScopePath.Pop();
            var scope = this.scopes.TakeWhile(x => x.Path.StartsWith(path)).Last();

            var join = syntax.Members.Join(
                structType.Members, 
                x => x.MemberName, 
                x => x.Name, 
                (x, y) => (x.MemberName, x.Value, scope.FindType(y.TypePath).GetValue(), x.Operation));

            var captured = new HashSet<IdentifierPath>();

            foreach (var (name, value, type, op) in join) {                
                value.Accept(this);

                if (op == DeclarationOperation.Equate) {
                    if (!type.Equals(value.ReturnType)) {
                        throw new Exception();
                    }
                }
                else {
                    if (!(type is VariableType varType)) {
                        throw new Exception();
                    }

                    if (!varType.TargetType.Equals(value.ReturnType)) {
                        throw new Exception();
                    }

                    captured.Add(scopes.Peek().Path.Append("$implicit_variable"));
                }

                captured.UnionWith(value.ReturnCapturedVariables);
            }

            syntax.ReturnCapturedVariables = captured;
            syntax.ReturnType = syntax.StructType = structType;

            return 0;
        }

        public int VisitMemberAccessSyntax(MemberAccessSyntax syntax) {
            syntax.Target = syntax.Target.Accept(new LiteralVariableReplacer());
            syntax.Target.Accept(this);
            syntax.ReturnCapturedVariables = syntax.Target.ReturnCapturedVariables;

            if (!(syntax.Target.ReturnType is VariableType varType)) {
                throw new Exception();
            }

            var structType = (SingularStructType)varType.TargetType;

            var mem = structType.Members.Where(x => x.Name == syntax.MemberName).FirstOrDefault();
            if (mem == null) {
                throw new Exception();
            }

            var memtype = this.scopes.Peek().FindType(mem.TypePath).GetValue();

            if (syntax.IsLiteralAccess) {
                if (!(memtype is VariableType)) {
                    throw new Exception();
                }

                syntax.ReturnType = memtype;
            }
            else {
                if (memtype is VariableType memVarType) {
                    syntax.ReturnType = memVarType.TargetType;
                }
                else {
                    syntax.ReturnType = memtype;
                }
            }

            if (!(syntax.ReturnType is VariableType)) {
                syntax.ReturnCapturedVariables = new HashSet<IdentifierPath>();
            }

            return 0;
        }
    }
}