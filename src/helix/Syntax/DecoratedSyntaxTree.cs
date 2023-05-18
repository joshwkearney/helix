//using Helix.Analysis;
//using Helix.Analysis.Flow;
//using Helix.Analysis.TypeChecking;
//using Helix.Analysis.Types;
//using Helix.Generation;
//using Helix.Generation.Syntax;
//using Helix.Parsing;

//namespace Helix.Syntax {
//    public class DecoratedSyntaxTree : ISyntaxTree {
//        public IEnumerable<ISyntaxDecorator> Decorators { get; }

//        public ISyntaxTree WrappedSyntax { get; }

//        public TokenLocation Location => WrappedSyntax.Location;

//        public IEnumerable<ISyntaxTree> Children => WrappedSyntax.Children;

//        public bool IsPure => WrappedSyntax.IsPure;

//        public DecoratedSyntaxTree(ISyntaxTree inner, IEnumerable<ISyntaxDecorator> decos) {
//            this.WrappedSyntax = inner;
//            Decorators = decos.ToArray();
//        }

//        private DecoratedSyntaxTree ReplaceWrappedSyntax(ISyntaxTree newInner) {
//            return new DecoratedSyntaxTree(newInner, Decorators);
//        }

//        public Option<HelixType> AsType(TypeFrame types) => WrappedSyntax.AsType(types);

//        public ISyntaxTree ToRValue(TypeFrame types) {
//            return this.ReplaceWrappedSyntax(this.WrappedSyntax.ToRValue(types)).CheckTypes(types);
//        }

//        public ISyntaxTree ToLValue(TypeFrame types) {
//            return this.ReplaceWrappedSyntax(this.WrappedSyntax.ToLValue(types)).CheckTypes(types);
//        }

//        public ISyntaxTree CheckTypes(TypeFrame types) {
//            if (this.IsTypeChecked(types)) {
//                return this;
//            }

//            foreach (var deco in Decorators) {
//                deco.PreCheckTypes(this.WrappedSyntax, types);
//            }

//            var result = this.WrappedSyntax.CheckTypes(types);
//            var returnType = result.GetReturnType(types);

//            foreach (var deco in Decorators) {
//                deco.PostCheckTypes(result, types);
//            }

//            result = this.ReplaceWrappedSyntax(result);
//            result.SetReturnType(returnType, types);

//            return result;
//        }

//        public void AnalyzeFlow(FlowFrame flow) {
//            foreach (var deco in Decorators) {
//                deco.PreAnalyzeFlow(this.WrappedSyntax, flow);
//            }

//            this.WrappedSyntax.AnalyzeFlow(flow);
//            this.SetLifetimes(this.WrappedSyntax.GetLifetimes(flow), flow);

//            foreach (var deco in Decorators) {
//                deco.PostAnalyzeFlow(this.WrappedSyntax, flow);
//            }
//        }

//        public ICSyntax GenerateCode(FlowFrame types, ICStatementWriter writer) {
//            foreach (var deco in Decorators) {
//                deco.PreGenerateCode(this, types, writer);
//            }

//            var result = this.WrappedSyntax.GenerateCode(types, writer);

//            foreach (var deco in Decorators) {
//                deco.PostGenerateCode(this.WrappedSyntax, result, types, writer);
//            }

//            return result;
//        }
//    }
//}
