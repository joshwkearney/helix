using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Trophy.Analysis;
using Trophy.Analysis.Types;
using Trophy.CodeGeneration;
using Trophy.CodeGeneration.CSyntax;
using Trophy.Parsing;

namespace Trophy.Features.Functions {
    public class LambdaSyntaxA : ISyntaxA {
        public TokenLocation Location { get; }

        public IReadOnlyList<ParseFunctionParameter> Parameters { get; }

        public ISyntaxA Body { get; }

        public LambdaSyntaxA(TokenLocation loc, ISyntaxA body, IReadOnlyList<ParseFunctionParameter> pars) {
            this.Location = loc;
            this.Body = body;
            this.Parameters = pars;
        }

        public ISyntaxB CheckNames(INamesRecorder names) {
            var path = names.Context.Scope.Append("$lambda" + names.GetNewVariableId());

            var pars = FunctionsHelper.CheckParameters(this.Parameters, names, this.Location);

            var closestHeap = RegionsHelper.GetClosestHeap(names.Context.Region);

            // Check for duplicate parameter names
            FunctionsHelper.CheckForDuplicateParameters(this.Location, pars.Select(x => x.Name));

            // Resolve body names
            var body = FunctionsHelper.ResolveBodyNames(names, path, closestHeap, this.Body, pars);

            // Reserve ids for the parameters
            var ids = pars.Select(_ => names.GetNewVariableId()).ToArray();

            return new LambdaSyntaxB(this.Location, path, body, closestHeap, pars, ids);
        }
    }

    public class LambdaSyntaxB : ISyntaxB {
        public TokenLocation Location { get; }

        public ISyntaxB Body { get; }

        public IdentifierPath EnclosingHeap { get; }

        public IdentifierPath FunctionPath { get; }

        public IReadOnlyList<FunctionParameter> Parameters { get; }

        public IReadOnlyList<int> ParameterIds { get; }

        public IImmutableSet<VariableUsage> VariableUsage {
            get {
                var parPaths = this.Parameters
                    .Select(x => this.FunctionPath.Append(x.Name))
                    .ToImmutableHashSet();

                return this.Body.VariableUsage
                    .Where(x => !parPaths.Contains(x.VariablePath))
                    .ToImmutableHashSet();
            }
        }

        public LambdaSyntaxB(
            TokenLocation location, 
            IdentifierPath path,
            ISyntaxB body, 
            IdentifierPath region,
            IReadOnlyList<FunctionParameter> parameters, 
            IReadOnlyList<int> parIds) {

            this.Location = location;
            this.Body = body;
            this.Parameters = parameters;
            this.FunctionPath = path;
            this.EnclosingHeap = region;
            this.ParameterIds = parIds;
        }

        public ISyntaxC CheckTypes(ITypesRecorder types) {
            var parPaths = this.Parameters
                .Select(x => this.FunctionPath.Append(x.Name))
                .ToHashSet();

            // Find all of the free variables
            var freeVars = this.Body.VariableUsage
                .Where(x => x.Kind != VariableUsageKind.Region)
                .Select(x => x.VariablePath)
                .Except(this.Parameters.Select(x => this.FunctionPath.Append(x.Name)))
                .Select(x => (path: x, info: types.TryGetVariable(x).GetValue()))
                .ToArray();

            var context = types.Context.WithContainingFunction(ContainingFunction.Lambda);
            var body = types.WithContext(types.Context, types => {
                // Flow-type all of the free variables to be parameters, excluding captured regions
                foreach (var (path, info) in freeVars) {
                    var newInfo = new VariableInfo(
                        name: info.Name,
                        innerType: info.Type,
                        kind: info.Kind,
                        source: VariableSource.Parameter,
                        id: info.UniqueId);

                    types.DeclareName(path, NamePayload.FromVariable(newInfo));
                }

                // Declare the explicit parameters
                FunctionsHelper.DeclareParameters(types, this.FunctionPath, this.Parameters, this.ParameterIds);

                return this.Body.CheckTypes(types);
            });

            // The return value must be allocated on our region or be one of the arguments
            FunctionsHelper.CheckForInvalidReturnScope(this.Body.Location, this.EnclosingHeap, body);

            var name = this.FunctionPath.Segments.Last();
            var sig = new FunctionSignature(name, body.ReturnType, this.Parameters.ToImmutableList());
            var parTypes = this.Parameters.Select(x => x.Type).ToArray();
            var returnType = new FunctionType(body.ReturnType, parTypes);
            var freeVarTypes = freeVars.Select(x => x.info).ToArray();

            var freeRegions = this.Body.VariableUsage
                .Where(x => x.Kind == VariableUsageKind.Region)
                .Select(x => x.VariablePath)
                .ToArray();

            return new LambdaSyntaxC(
                sig, 
                returnType, 
                this.FunctionPath, 
                this.EnclosingHeap, 
                body, 
                freeVarTypes, 
                this.ParameterIds, 
                freeRegions);
        }
    }

    public class LambdaSyntaxC : ISyntaxC {
        private static int counter = 0;

        public FunctionSignature Signature { get; }

        public IdentifierPath FunctionPath { get; }

        public ISyntaxC Body { get; }

        public IReadOnlyList<VariableInfo> FreeVariables { get; }

        public IdentifierPath EnclosingRegion { get; }

        public IReadOnlyList<int> ParameterIds { get; }

        public IReadOnlyList<IdentifierPath> FreeRegions { get; }

        public ITrophyType ReturnType { get; }

        public LambdaSyntaxC(
            FunctionSignature sig, 
            ITrophyType returnType, 
            IdentifierPath funcPath, 
            IdentifierPath region,
            ISyntaxC body, 
            IReadOnlyList<VariableInfo> freeVars,
            IReadOnlyList<int> parIds,
            IReadOnlyList<IdentifierPath> freeRegions) {

            this.Signature = sig;
            this.FunctionPath = funcPath;
            this.Body = body;
            this.ReturnType = returnType;
            this.FreeVariables = freeVars;
            this.EnclosingRegion = region;
            this.ParameterIds = parIds;
            this.FreeRegions = freeRegions;
        }

        private string GenerateClosureFunction(CType envType, ICWriter writer) {
            var returnType = writer.ConvertType(this.Signature.ReturnType);
            var pars = this.Signature
                .Parameters
                .Select((x, i) => new CParameter(writer.ConvertType(x.Type), "$" + x.Name + this.ParameterIds[i]))
                .Prepend(new CParameter(CType.VoidPointer, "environment"))
                .ToArray();

            // Set up the body statement writer
            var bodyWriter = new CStatementWriter();
            var bodyStats = new List<CStatement>();
            bodyWriter.StatementWritten += (s, e) => bodyStats.Add(e);

            // Dereference the environment
            var envTempName = "environment_temp" + counter++;
            var envDeref = CExpression.Dereference(CExpression.VariableLiteral(envTempName));
            var env = CExpression.Cast(CType.Pointer(envType), CExpression.VariableLiteral("environment"));

            bodyWriter.WriteStatement(CStatement.VariableDeclaration(CType.Pointer(envType), envTempName, env));
            bodyWriter.WriteStatement(CStatement.NewLine());

            bodyWriter.WriteStatement(CStatement.Comment("Unpack the closure environment"));

            // Unpack the environment variables
            foreach (var info in this.FreeVariables) {
                var name = "$" + info.Name + info.UniqueId;
                var type = writer.ConvertType(info.Type);
                var assign = CExpression.MemberAccess(envDeref, name);

                bodyWriter.WriteStatement(CStatement.VariableDeclaration(type, name, assign));
            }

            // Unpack the environment regions
            foreach (var region in this.FreeRegions) {
                var name = region.Segments.Last();
                var type = CType.Pointer(CType.NamedType("Region"));
                var assign = CExpression.MemberAccess(envDeref, name);

                bodyWriter.WriteStatement(CStatement.VariableDeclaration(type, name, assign));
            }

            bodyWriter.WriteStatement(CStatement.NewLine());

            // Generate the body
            var retExpr = this.Body.GenerateCode(writer, bodyWriter);

            if (this.Signature.ReturnType.IsVoidType) {
                bodyStats.Add(CStatement.FromExpression(retExpr));
            }
            else {
                bodyStats.Add(CStatement.Return(retExpr));
            }

            CDeclaration decl;
            CDeclaration forwardDecl;

            if (this.Signature.ReturnType.IsVoidType) {
                decl = CDeclaration.Function("$" + this.FunctionPath, true, pars, bodyStats);
                forwardDecl = CDeclaration.FunctionPrototype("$" + this.FunctionPath, true, pars);
            }
            else {
                decl = CDeclaration.Function(returnType, "$" + this.FunctionPath, true, pars, bodyStats);
                forwardDecl = CDeclaration.FunctionPrototype(returnType, "$" + this.FunctionPath, true, pars);
            }

            // Generate the function
            writer.WriteDeclaration3(decl);
            writer.WriteDeclaration3(CDeclaration.EmptyLine());

            writer.WriteDeclaration2(forwardDecl);
            writer.WriteDeclaration2(CDeclaration.EmptyLine());

            return "$" + this.FunctionPath;
        }

        private CType GenerateClosureEnvironmentStruct(ICWriter writer) {
            var envType = "ClosureEnvironment" + counter++;
            var pars = this.FreeVariables
                .Select(x => new CParameter(writer.ConvertType(x.Type), "$" + x.Name + x.UniqueId))
                .Concat(this.FreeRegions.Select(x => new CParameter(CType.NamedType("Region*"), x.Segments.Last())))
                .ToArray();

            writer.WriteDeclaration1(CDeclaration.StructPrototype(envType));
            writer.WriteDeclaration1(CDeclaration.EmptyLine());

            writer.WriteDeclaration2(CDeclaration.Struct(envType, pars));
            writer.WriteDeclaration2(CDeclaration.EmptyLine());

            return CType.NamedType(envType);
        }

        private CExpression GenerateStackEnvironment(CType envType, ICWriter writer, ICStatementWriter statWriter) {
            var envName = "closure_environment" + counter++;

            // Declare the environment struct
            statWriter.WriteStatement(CStatement.VariableDeclaration(envType, envName));

            return CExpression.AddressOf(CExpression.VariableLiteral(envName));
        }

        private CExpression GenerateRegionEnvironment(CType envType, ICWriter writer, ICStatementWriter statWriter) {
            var regionName = this.EnclosingRegion.Segments.Last();

            // Write data assignment
            return CExpression.Invoke(CExpression.VariableLiteral("region_alloc"), new[] {
                        CExpression.VariableLiteral(regionName),
                        CExpression.Sizeof(envType)
                });
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter parentWriter) {
            var envType = this.GenerateClosureEnvironmentStruct(writer);
            var funcName = this.GenerateClosureFunction(envType, writer);

            var envName = "closure_environment" + counter++;
            var closureName = "closure_temp" + counter++;
            var closureType = writer.ConvertType(this.ReturnType);

            var envAccess = CExpression.Dereference(CExpression.VariableLiteral(envName));
            var closureAccess = CExpression.VariableLiteral(closureName);

            parentWriter.WriteStatement(CStatement.Comment("Pack the lambda environment"));

            if (RegionsHelper.IsStack(this.EnclosingRegion)) {
                // Create the environment
                parentWriter.WriteStatement(
                    CStatement.VariableDeclaration(
                        CType.Pointer(envType), 
                        envName, 
                        this.GenerateStackEnvironment(envType, writer, parentWriter)));
            }
            else {
                var env = this.GenerateRegionEnvironment(envType, writer, parentWriter);
                env = CExpression.Cast(CType.Pointer(envType), env);

                // Create the environment
                parentWriter.WriteStatement(CStatement.VariableDeclaration(CType.Pointer(envType), envName, env));
            }            

            // Write the environment fields
            foreach (var info in this.FreeVariables) {
                var assign = CExpression.VariableLiteral("$" + info.Name + info.UniqueId);

                if (info.Source == VariableSource.Local) {
                    assign = CExpression.AddressOf(assign);
                }

                parentWriter.WriteStatement(
                    CStatement.Assignment(
                        CExpression.MemberAccess(envAccess, "$" + info.Name + info.UniqueId),
                        assign));
            }

            // Write the captured regions
            foreach (var info in this.FreeRegions) {
                var assign = CExpression.VariableLiteral(info.Segments.Last());

                parentWriter.WriteStatement(
                    CStatement.Assignment(
                        CExpression.MemberAccess(envAccess, info.Segments.Last()),
                        assign));
            }

            parentWriter.WriteStatement(CStatement.NewLine());

            // Write the closure struct
            parentWriter.WriteStatement(CStatement.Comment("Lambda expression literal"));
            parentWriter.WriteStatement(CStatement.VariableDeclaration(closureType, closureName));

            // Write the closure environment
            parentWriter.WriteStatement(
                CStatement.Assignment(
                    CExpression.MemberAccess(closureAccess, "environment"),
                    CExpression.VariableLiteral(envName)));

            // Write the closure function
            parentWriter.WriteStatement(
                CStatement.Assignment(
                    CExpression.MemberAccess(closureAccess, "function"),
                    CExpression.AddressOf(CExpression.VariableLiteral(funcName))));

            parentWriter.WriteStatement(CStatement.NewLine());

            return closureAccess;
        }
    }
}