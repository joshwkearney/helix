using System.Collections.Immutable;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Generation.CSyntax;
using Helix.Features.FlowControl;
using Helix.Features.Functions;
using Helix.Features.Primitives;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Features.Variables;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Parsing {
    public partial class Parser {
        private FunctionParseSignature FunctionSignature() {
            var start = this.Advance(TokenKind.FunctionKeyword);
            var funcName = this.Advance(TokenKind.Identifier).Value;

            this.Advance(TokenKind.OpenParenthesis);
            this.scope = this.scope.Append(funcName);

            var pars = ImmutableList<ParseFunctionParameter>.Empty;
            while (!this.Peek(TokenKind.CloseParenthesis)) {
                bool isWritable;
                Token parStart;

                if (this.Peek(TokenKind.VarKeyword)) {
                    parStart = this.Advance(TokenKind.VarKeyword);
                    isWritable = true;
                }
                else {
                    parStart = this.Advance(TokenKind.LetKeyword);
                    isWritable = false;
                }

                var parName = this.Advance(TokenKind.Identifier).Value;
                this.Advance(TokenKind.AsKeyword);

                var parType = this.TopExpression();
                var parLoc = parStart.Location.Span(parType.Location);

                if (!this.Peek(TokenKind.CloseParenthesis)) {
                    this.Advance(TokenKind.Comma);
                }

                pars = pars.Add(new ParseFunctionParameter(parLoc, parName, parType, isWritable));
            }

            this.scope = this.scope.Pop();

            var end = this.Advance(TokenKind.CloseParenthesis);
            var returnType = new VoidLiteral(end.Location) as ISyntaxTree;

            if (this.TryAdvance(TokenKind.AsKeyword)) {
                returnType = this.TopExpression();
            }

            var loc = start.Location.Span(returnType.Location);
            var sig = new FunctionParseSignature(loc, funcName, returnType, pars);

            return sig;
        }

        private IDeclaration FunctionDeclaration() {
            var sig = this.FunctionSignature();

            if (!this.Peek(TokenKind.OpenBrace)) {
                this.Advance(TokenKind.Yields);
            }

            this.funcPath.Push(this.scope.Append(sig.Name));
            this.scope = this.scope.Append(sig.Name);
            var body = this.TopExpression();            

            this.Advance(TokenKind.Semicolon);
            this.scope = this.scope.Pop();
            this.funcPath.Pop();

            return new FunctionParseDeclaration(
                sig.Location.Span(body.Location), 
                sig,
                body);
        }
    }
}

namespace Helix.Features.Functions {
    public record FunctionParseDeclaration : IDeclaration {
        private readonly FunctionParseSignature signature;
        private readonly ISyntaxTree body;

        public TokenLocation Location { get; }

        public FunctionParseDeclaration(TokenLocation loc, FunctionParseSignature sig, ISyntaxTree body) {

            this.Location = loc;
            this.signature = sig;
            this.body = body;
        }

        public void DeclareNames(TypeFrame types) {
            FunctionsHelper.CheckForDuplicateParameters(
                this.Location, 
                this.signature.Parameters.Select(x => x.Name));

            FunctionsHelper.DeclareName(this.signature, types);
        }

        public void DeclareTypes(TypeFrame types) {
            var sig = this.signature.ResolveNames(types);

            // Declare this function
            types.Functions[sig.Path] = sig;
        }
        
        public IDeclaration CheckTypes(TypeFrame types) {
            var path = types.ResolvePath(this.Location.Scope, this.signature.Name);
            var sig = types.Functions[path];

            // Set the scope for type checking the body
            types = new TypeFrame(types);

            // Declare parameters
            FunctionsHelper.DeclareParameterTypes(this.Location, sig, types);

            // Declare a "heap" lifetime used for function returns
            types.LifetimeRoots[Lifetime.Heap.Path] = Lifetime.Heap;
            types.LifetimeRoots[Lifetime.Stack.Path] = Lifetime.Stack;

            // Check types
            var body = this.body;

            if (sig.ReturnType == PrimitiveType.Void) {
                body = new BlockSyntax(this.body.Location, new ISyntaxTree[] { 
                    this.body,
                    new VoidLiteral(this.body.Location)
                });
            }

            body = body.CheckTypes(types)
                .ToRValue(types)
                .ConvertTypeTo(sig.ReturnType, types);

#if DEBUG
            // Debug check: make sure that every syntax tree has a return type
            foreach (var expr in body.GetAllChildren()) {
                if (!types.ReturnTypes.ContainsKey(expr)) {
                    throw new Exception("Compiler assertion failed: syntax tree does not have a return type");
                }
            }
#endif

            return new FunctionDeclaration(this.Location, sig, body);
        }
    }

    public record FunctionDeclaration : IDeclaration {
        private readonly ISyntaxTree body;

        public FunctionSignature Signature { get; }

        public TokenLocation Location { get; }

        public FunctionDeclaration(TokenLocation loc, FunctionSignature sig, ISyntaxTree body) {

            this.Location = loc;
            this.Signature = sig;
            this.body = body;
        }

        public void DeclareNames(TypeFrame names) {
            throw new InvalidOperationException();
        }

        public void DeclareTypes(TypeFrame paths) {
            throw new InvalidOperationException();
        }

        public IDeclaration CheckTypes(TypeFrame types) {
            throw new InvalidOperationException();
        }

        public void AnalyzeFlow(FlowFrame flow) {
            // Set the scope for type checking the body
            var bodyTypes = new FlowFrame(flow);

            // Declare parameters
            FunctionsHelper.DeclareParameterFlow(this.Location, this.Signature, flow);

            this.body.AnalyzeFlow(flow);

            // Here we need to make sure that the return value can outlive the heap
            // We're going to find all roots that are contributing to the return value
            // and confirm that each one outlives the heap
            var roots = this.body.GetLifetimes(flow).Lifetimes
                .SelectMany(x => flow.LifetimeGraph.GetPrecursorLifetimes(x))
                .Where(x => x.Kind != LifetimeKind.Inferencee)
                .ToArray();

            roots = flow.ReduceRootSet(roots).ToArray();

            // Make sure all the roots outlive the heap
            if (!roots.All(x => flow.LifetimeGraph.DoesOutlive(x, Lifetime.Heap))) {
                throw new LifetimeException(
                   this.Location,
                   "Lifetime Inference Failed",
                   "This value cannot be returned from the function because the region it is allocated " 
                   + "on might not outlive the function's return region. The problematic regions are: " 
                   + $"'{ string.Join(", ", roots) }'.\n\nTo fix this error, you can try implementing a '.copy()' method " 
                   + $"on the type '{this.Signature.ReturnType}' so that it can be moved between regions, " 
                   + "or you can try adding explicit region annotations to the function's signature " 
                   + "to help the compiler prove that this return value is safe.");
            }

            // Add a dependency between every returned lifetime and the heap
            foreach (var lifetime in flow.Lifetimes[body].Lifetimes) {
                flow.LifetimeGraph.RequireOutlives(lifetime, Lifetime.Heap);
            }

#if DEBUG
            // Debug check: Make sure every part of the syntax tree has a lifetime
            foreach (var expr in body.GetAllChildren()) {
                if (!flow.Lifetimes.ContainsKey(expr)) {
                    throw new Exception("Compiler assertion failed: syntax tree does not have any captured variables");
                }
            }
#endif
        }

        public void GenerateCode(FlowFrame types, ICWriter writer) {
            writer.ResetTempNames();

            var returnType = this.Signature.ReturnType == PrimitiveType.Void
                ? new CNamedType("void")
                : writer.ConvertType(this.Signature.ReturnType);

            var pars = this.Signature
                .Parameters
                .Select((x, i) => new CParameter() { 
                    Type = writer.ConvertType(x.Type),
                    Name = writer.GetVariableName(this.Signature.Path.Append(x.Name))
                })
                .Prepend(new CParameter() {
                    Name = "_return_region",
                    Type = new CNamedType("int")
                })
                .ToArray();

            var funcName = writer.GetVariableName(this.Signature.Path);
            var body = new List<ICStatement>();
            var bodyWriter = new CStatementWriter(writer, body);

            // Register the heap lifetime for the body to use
            bodyWriter.RegisterLifetime(
                Lifetime.Heap, 
                new CVariableLiteral("_return_region"));

            // Register the parameter member paths
            foreach (var par in this.Signature.Parameters) {
                foreach (var (relPath, type) in par.Type.GetMembers(types)) {
                    writer.RegisterMemberPath(this.Signature.Path.Append(par.Name), relPath);
                }
            }

            // Register the parameter lifetimes
            foreach (var par in this.Signature.Parameters) {
                foreach (var (relPath, _) in par.Type.GetMembers(types)) {
                    var path = this.Signature.Path.Append(par.Name).Append(relPath);
                    var lifetime = types.VariableLifetimes[path];

                    bodyWriter.RegisterLifetime(lifetime, new CMemberAccess() {
                        Target = new CVariableLiteral(writer.GetVariableName(path)),
                        MemberName = "region"
                    });
                }
            }

            // Register the parameters as local variables
            foreach (var par in this.Signature.Parameters) {
                foreach (var (relPath, _) in par.Type.GetMembers(types)) {
                    var path = this.Signature.Path.Append(par.Name).Append(relPath);

                    bodyWriter.RegisterVariableKind(path, CVariableKind.Local);
                }
            }

            // Generate the body
            var retExpr = this.body.GenerateCode(types, bodyWriter);

            if (this.Signature.ReturnType != PrimitiveType.Void) {
                bodyWriter.WriteEmptyLine();
                bodyWriter.WriteStatement(new CReturn() { 
                    Target = retExpr
                });
            }

            // If the body ends with an empty line, trim it
            if (body.Any() && body.Last().IsEmpty) {
                body = body.SkipLast(1).ToList();
            }

            var decl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars,
                Body = body
            };

            var forwardDecl = new CFunctionDeclaration() {
                ReturnType = returnType,
                Name = funcName,
                Parameters = pars
            };

            writer.WriteDeclaration2(forwardDecl);

            writer.WriteDeclaration4(decl);
            writer.WriteDeclaration4(new CEmptyLine());
        }
    }
}
