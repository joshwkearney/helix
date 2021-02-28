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

        public IReadOnlyList<FunctionParameter> Parameters { get; }

        public ISyntaxA Body { get; }

        public LambdaSyntaxA(TokenLocation loc, ISyntaxA body, IReadOnlyList<FunctionParameter> pars) {
            this.Location = loc;
            this.Body = body;
            this.Parameters = pars;
        }

        public ISyntaxB CheckNames(INameRecorder names) {
            var path = names.CurrentScope.Append("$lambda" + names.GetNewVariableId());
            var pars = this.Parameters
                .Select(x => new FunctionParameter(x.Name, names.ResolveTypeNames(x.Type, this.Location)))
                .ToArray();
            var region = names.CurrentRegion;

            // Check for duplicate parameter names
            FunctionsHelper.CheckForDuplicateParameters(this.Location, pars);

            // Resolve body names
            var body = FunctionsHelper.ResolveBodyNames(names, path, this.Body, pars);

            // Reserve ids for the parameters
            var ids = pars.Select(_ => names.GetNewVariableId()).ToArray();

            return new LambdaSyntaxB(this.Location, path, body, region, pars, ids);
        }
    }

    public class LambdaSyntaxB : ISyntaxB {
        public TokenLocation Location { get; }

        public ISyntaxB Body { get; }

        public IdentifierPath Region { get; }

        public IdentifierPath FunctionPath { get; }

        public IReadOnlyList<FunctionParameter> Parameters { get; }

        public IReadOnlyList<int> ParameterIds { get; }

        public ImmutableDictionary<IdentifierPath, VariableUsageKind> VariableUsage {
            get {
                var parPaths = this.Parameters.Select(x => this.FunctionPath.Append(x.Name)).ToArray();

                return this.Body.VariableUsage.RemoveRange(parPaths);
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
            this.Region = region;
            this.ParameterIds = parIds;
        }

        public ISyntaxC CheckTypes(ITypeRecorder types) {
            // Find all of the free variables
            var freeVars = this.Body.VariableUsage
                .RemoveRange(this.Parameters.Select(x => this.FunctionPath.Append(x.Name)))
                .Select(x => x.Key)
                .Select(x => (path: x, info: types.TryGetVariable(x).GetValue()))
                .ToArray();

            // Allow variables to be flow-typed
            types.PushFlow();

            // Flow-type all of the free variables to be parameters
            foreach (var (path, info) in freeVars) {
                var defKind = VariableDefinitionKind.ParameterRef;

                if (info.DefinitionKind == VariableDefinitionKind.LocalVar || info.DefinitionKind == VariableDefinitionKind.ParameterVar) {
                    defKind = VariableDefinitionKind.ParameterVar;
                }

                var newInfo = new VariableInfo(
                    name:               info.Name,
                    innerType:          info.Type,
                    kind:               defKind,
                    id:                 info.UniqueId,
                    valueLifetimes:     info.ValueLifetimes,
                    variableLifetimes:  info.VariableLifetimes);

                types.DeclareVariable(path, newInfo);
            }

            // Declare the explicit parameters
            FunctionsHelper.DeclareParameters(types, this.FunctionPath, this.Parameters, this.ParameterIds);

            // Type check the body
            var body = this.Body.CheckTypes(types);

            // Remove the flow typing
            types.PopFlow();

            // The return value must be allocated on our region or be one of the arguments
            FunctionsHelper.CheckForInvalidReturnScope(this.Body.Location, body);

            var name = this.FunctionPath.Segments.Last();
            var sig = new FunctionSignature(name, body.ReturnType, this.Parameters.ToImmutableList());
            var parTypes = this.Parameters.Select(x => x.Type).ToArray();
            var returnType = new FunctionType(body.ReturnType, parTypes);
            var freeVarTypes = freeVars.Select(x => x.info).ToArray();

            return new LambdaSyntaxC(sig, returnType, this.FunctionPath, this.Region, body, freeVarTypes, this.ParameterIds);
        }
    }

    public class LambdaSyntaxC : ISyntaxC {
        private static int counter = 0;

        private readonly FunctionSignature sig;
        private readonly IdentifierPath funcPath;
        private readonly ISyntaxC body;
        private readonly IReadOnlyList<VariableInfo> freeVars;
        private readonly IdentifierPath regionPath;
        private readonly IReadOnlyList<int> parIds;

        public TrophyType ReturnType { get; }

        public ImmutableHashSet<IdentifierPath> Lifetimes {
            get {
                return this.freeVars
                    .SelectMany(x => x.VariableLifetimes)
                    .Append(this.regionPath)
                    .ToImmutableHashSet();
            }
        }

        public LambdaSyntaxC(
            FunctionSignature sig, 
            TrophyType returnType, 
            IdentifierPath funcPath, 
            IdentifierPath region,
            ISyntaxC body, 
            IReadOnlyList<VariableInfo> freeVars,
            IReadOnlyList<int> parIds) {

            this.sig = sig;
            this.funcPath = funcPath;
            this.body = body;
            this.ReturnType = returnType;
            this.freeVars = freeVars;
            this.regionPath = region;
            this.parIds = parIds;
        }

        private string GenerateClosureFunction(CType envType, ICWriter writer) {
            var returnType = writer.ConvertType(this.sig.ReturnType);
            var pars = this.sig
                .Parameters
                .Select((x, i) => new CParameter(writer.ConvertType(x.Type), "$" + x.Name + this.parIds[i]))
                .Prepend(new CParameter(CType.VoidPointer, "environment"))
                .ToArray();

            // Set up the body statement writer
            var bodyWriter = new CStatementWriter();
            var bodyStats = new List<CStatement>();
            bodyWriter.StatementWritten += (s, e) => bodyStats.Add(e);

            // Dereference the environment
            var envTempName = "environment_temp" + counter++;
            var envDeref = CExpression.Dereference(CExpression.VariableLiteral(envTempName));
            bodyWriter.WriteStatement(CStatement.VariableDeclaration(CType.Pointer(envType), envTempName, CExpression.VariableLiteral("environment")));

            // Unpack the heap
            bodyWriter.WriteStatement(
                CStatement.VariableDeclaration(
                    CType.NamedType("Region*"),
                    "heap",
                    CExpression.MemberAccess(envDeref, "heap")));

            // Unpack the environment
            foreach (var info in this.freeVars) {
                var name = "$" + info.Name + info.UniqueId;
                var type = CType.Pointer(writer.ConvertType(info.Type));
                var assign = CExpression.MemberAccess(envDeref, name);

                bodyWriter.WriteStatement(CStatement.VariableDeclaration(type, name, assign));
            }

            bodyWriter.WriteStatement(CStatement.NewLine());

            // Generate the body
            var retExpr = this.body.GenerateCode(writer, bodyWriter);
            bodyStats.Add(CStatement.Return(retExpr));

            var decl = CDeclaration.Function(returnType, "$" + this.funcPath, true, pars, bodyStats);
            var forwardDecl = CDeclaration.FunctionPrototype(returnType, "$" + this.funcPath, true, pars);

            // Generate the function
            writer.WriteDeclaration(decl);
            writer.WriteDeclaration(CDeclaration.EmptyLine());

            writer.WriteForwardDeclaration(forwardDecl);
            writer.WriteForwardDeclaration(CDeclaration.EmptyLine());

            return "$" + this.funcPath;
        }

        private CType GenerateClosureEnvironmentStruct(ICWriter writer) {
            var envType = "ClosureEnvironment" + counter++;
            var pars = this.freeVars
                .Select(x => new CParameter(CType.Pointer(writer.ConvertType(x.Type)), "$" + x.Name + x.UniqueId))
                .Append(new CParameter(CType.NamedType("Region*"), "heap"))
                .ToArray();

            writer.WriteForwardDeclaration(CDeclaration.StructPrototype(envType));
            writer.WriteForwardDeclaration(CDeclaration.Struct(envType, pars));
            writer.WriteForwardDeclaration(CDeclaration.EmptyLine());

            return CType.NamedType(envType);
        }

        private CExpression GenerateStackEnvironment(CType envType, ICWriter writer, ICStatementWriter statWriter) {
            var envName = "closure_environment" + counter++;

            // Declare the environment struct
            statWriter.WriteStatement(CStatement.VariableDeclaration(envType, envName));

            return CExpression.AddressOf(CExpression.VariableLiteral(envName));
        }

        private CExpression GenerateRegionEnvironment(CType envType, ICWriter writer, ICStatementWriter statWriter) {
            var regionName = this.regionPath.Segments.Last();

            // Write data assignment
            return CExpression.Invoke(CExpression.VariableLiteral("region_alloc"), new[] {
                        CExpression.VariableLiteral(regionName),
                        CExpression.Sizeof(envType)
                });
        }

        public CExpression GenerateCode(ICWriter writer, ICStatementWriter parentWriter) {
            writer.RequireRegions();

            var envType = this.GenerateClosureEnvironmentStruct(writer);
            var funcName = this.GenerateClosureFunction(envType, writer);

            var envName = "closure_environment" + counter++;
            var closureName = "closure_temp" + counter++;
            var closureType = writer.ConvertType(this.ReturnType);

            var envAccess = CExpression.Dereference(CExpression.VariableLiteral(envName));
            var closureAccess = CExpression.VariableLiteral(closureName);

            if (this.regionPath == IdentifierPath.StackPath) {
                // Create the environment
                parentWriter.WriteStatement(
                    CStatement.VariableDeclaration(
                        CType.Pointer(envType), 
                        envName, 
                        this.GenerateStackEnvironment(envType, writer, parentWriter)));

                // Assign the heap
                parentWriter.WriteStatement(
                CStatement.Assignment(
                    CExpression.MemberAccess(envAccess, "heap"),
                    CExpression.VariableLiteral(IdentifierPath.HeapPath.Segments.Last())));
            }
            else {
                // Create the environment
                parentWriter.WriteStatement(
                    CStatement.VariableDeclaration(
                        CType.Pointer(envType),
                        envName,
                        this.GenerateRegionEnvironment(envType, writer, parentWriter)));

                // Assign the heap
                parentWriter.WriteStatement(
                CStatement.Assignment(
                    CExpression.MemberAccess(envAccess, "heap"),
                    CExpression.VariableLiteral(this.regionPath.Segments.Last())));
            }            

            // Write the environment fields
            foreach (var info in this.freeVars) {
                var assign = CExpression.VariableLiteral("$" + info.Name + info.UniqueId);

                if (info.DefinitionKind != VariableDefinitionKind.ParameterRef && info.DefinitionKind != VariableDefinitionKind.ParameterVar) {
                    assign = CExpression.AddressOf(assign);
                }

                parentWriter.WriteStatement(
                    CStatement.Assignment(
                        CExpression.MemberAccess(envAccess, "$" + info.Name + info.UniqueId),
                        assign));
            }

            parentWriter.WriteStatement(CStatement.NewLine());

            // Write the closure struct
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