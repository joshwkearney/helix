using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Generation;
using Helix.Features.Functions;
using Helix.Parsing;
using Helix.Generation.Syntax;
using Helix.Analysis.Flow;
using Helix.Syntax;
using Helix.Analysis.TypeChecking;

namespace Helix.Parsing {
    public partial class Parser {
        private ISyntaxTree InvokeExpression(ISyntaxTree first) {
            this.Advance(TokenKind.OpenParenthesis);

            var args = new List<ISyntaxTree>();

            while (!this.Peek(TokenKind.CloseParenthesis)) {
                args.Add(this.TopExpression());

                if (!this.TryAdvance(TokenKind.Comma)) {
                    break;
                }
            }

            var last = this.Advance(TokenKind.CloseParenthesis);
            var loc = first.Location.Span(last.Location);

            return new InvokeParseSyntax(loc, first, args);
        }
    }
}

namespace Helix.Features.Functions {
    public record InvokeParseSyntax : ISyntaxTree {
        private readonly ISyntaxTree target;
        private readonly IReadOnlyList<ISyntaxTree> args;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => this.args.Prepend(this.target);

        public bool IsPure => false;

        public InvokeParseSyntax(TokenLocation loc, ISyntaxTree target, 
            IReadOnlyList<ISyntaxTree> args) {

            this.Location = loc;
            this.target = target;
            this.args = args;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) {
            var target = this.target.CheckTypes(types).ToRValue(types);
            var targetType = types.ReturnTypes[target];

            // Make sure the target is a function
            if (targetType is not NamedType named || !types.Functions.TryGetValue(named.Path, out var sig)) {
                throw TypeException.ExpectedFunctionType(this.target.Location, targetType);
            }

            // Make sure the arg count lines up
            if (this.args.Count != sig.Parameters.Count) {
                throw TypeException.ParameterCountMismatch(
                    this.Location, 
                    sig.Parameters.Count, 
                    this.args.Count);
            }

            var newArgs = new ISyntaxTree[this.args.Count];

            // Make sure the arg types line up
            for (int i = 0; i < this.args.Count; i++) {
                var expectedType = sig.Parameters[i].Type;

                newArgs[i] = this.args[i].CheckTypes(types).ConvertTypeTo(expectedType, types);
            }

            // TODO: Fix this
            var captured = Array.Empty<Lifetime>() as IReadOnlyList<Lifetime>;

            // TODO: Put this back
            // If there are any reference types in the result that can be found
            // in any of the arguments then assume we captured that argument.
            // Note: Pointer and array types are normalized to writable in case
            // somebody is casting away their readonly-ness
            //if (!sig.ReturnType.IsValueType(types)) {
            //    var retRefs = sig.ReturnType
            //        .GetContainedTypes(types)
            //        .Where(x => !x.IsValueType(types))
            //        .Select(NormalizeTypes)
            //        .ToArray();

            //    foreach (var arg in newArgs) {
            //        bool overlap = types.ReturnTypes[arg]
            //            .GetContainedTypes(types)
            //            .Where(x => !x.IsValueType(types))
            //            .Select(NormalizeTypes)
            //            .Intersect(retRefs)
            //            .Any();

            //        if (overlap) {
            //            captured = types.Lifetimes[arg];
            //        }
            //    }
            //}

            // TODO: Introduce a new captured variable if the function being called
            // is "pooling". This is because the new captured variable will have a 
            // lifetime computed at runtime based on where the function actually allocates
            // its return value. This new lifetime will be taken from the context struct
            // passed to the function

            var result = new InvokeSyntax(this.Location, sig, newArgs);
            types.ReturnTypes[result] = sig.ReturnType;

            return result;            
        }

        private static HelixType NormalizeTypes(HelixType type) {
            if (type is PointerType ptr) {
                return new PointerType(ptr.InnerType, true);
            }
            else if (type is ArrayType arr) {
                return new ArrayType(arr.InnerType, true);
            }
            else {
                return type;
            }
        }
    }

    public record InvokeSyntax : ISyntaxTree {
        private readonly FunctionSignature sig;
        private readonly IReadOnlyList<ISyntaxTree> args;

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children => args;

        public bool IsPure => false;

        public InvokeSyntax(
            TokenLocation loc,
            FunctionSignature sig,
            IReadOnlyList<ISyntaxTree> args) {

            this.Location = loc;
            this.sig = sig;
            this.args = args;
        }

        public ISyntaxTree CheckTypes(TypeFrame types) => this;

        public ISyntaxTree ToRValue(TypeFrame types) => this;

        public void AnalyzeFlow(FlowFrame flow) {
            foreach (var arg in this.args) {
                arg.AnalyzeFlow(flow);
            }

            // TODO: Fix this
            flow.Lifetimes[this] = new LifetimeBundle();
        }

        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
            var args = this.args
                .Select(x => x.GenerateCode(types, writer))
                .Prepend(new CVariableLiteral("_pool"))
                .ToArray();

            var result = new CInvoke() {
                Target = new CVariableLiteral(writer.GetVariableName(this.sig.Path)),
                Arguments = args
            };

            var name = writer.GetVariableName();

            var stat = new CVariableDeclaration() {
                Name = name,
                Type = writer.ConvertType(this.sig.ReturnType),
                Assignment = result
            };

            writer.WriteStatement(stat);

            return new CVariableLiteral(name);
        }
    }
}