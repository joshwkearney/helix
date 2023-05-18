//using Helix.Analysis;
//using Helix.Analysis.Flow;
//using Helix.Analysis.TypeChecking;
//using Helix.Analysis.Types;
//using Helix.Generation;
//using Helix.Generation.Syntax;

//namespace Helix.Syntax.Decorators {
//    public class LifetimeProducer : ISyntaxDecorator {
//        public IdentifierPath OriginPath { get; }

//        public HelixType OriginType { get; }

//        public LifetimeRole Role { get; }

//        public LifetimeProducer(IdentifierPath path, HelixType baseType, LifetimeRole kind) {
//            this.OriginPath = path;
//            this.OriginType = baseType;
//            this.Role = kind;
//        }

//        public virtual void PostCheckTypes(ISyntaxTree syntax, TypeFrame types) {
//            // Go through all the variables and sub variables and set up the lifetimes
//            // correctly
//            foreach (var (relPath, _) in this.OriginType.GetMembers(types)) {
//                var varPath = this.OriginPath.AppendMember(relPath);
//                var lifetime = new Lifetime(varPath, 0, LifetimeTarget.Location, this.Role);

//                // Add this variable's lifetime
//                types.LifetimeRoots[varPath] = lifetime;
//            }
//        }

//        public virtual void PreAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) {
//            // Just create a location lifetime because we don't know if we have contents
//            // See AssignedLifetimeProducer for that
//            var baseLifetime = new Lifetime(
//                this.OriginPath.ToVariablePath(), 
//                0, 
//                LifetimeTarget.Location,
//                this.Role);

//            // Go through all the variables and sub variables and set up the lifetimes
//            // correctly
//            foreach (var (relPath, _) in this.OriginType.GetMembers(flow)) {
//                var memPath = this.OriginPath.AppendMember(relPath);
//                var varLifetime = new Lifetime(memPath, 0, LifetimeTarget.Location, this.Role);

//                // Make sure the stack outlives this lifetime
//                //flow.LifetimeGraph.RequireOutlives(Lifetime.Stack, varLifetime);

//                // Make sure we say the main lifetime outlives all of the member lifetimes
//                flow.LifetimeGraph.RequireOutlives(baseLifetime, varLifetime);

//                // Add this variable members's lifetime
//                flow.LocationLifetimes[memPath] = varLifetime;
//            }
//        }

//        public virtual void PostGenerateCode(ISyntaxTree syntax, ICSyntax result, FlowFrame flow, ICStatementWriter writer) {
//            // Figure out the c vlaue for our new lifetime based on the lifetimes it
//            // is supposed to outlive
//            var roots = flow.GetRoots(flow.LocationLifetimes[this.OriginPath.ToVariablePath()])
//                .Where(x => !x.Path.Variable.StartsWith(this.OriginPath));

//            var cLifetime = writer.CalculateSmallestLifetime(syntax.Location, roots);

//            if (this.Role == LifetimeRole.Inference) {
//                foreach (var (relPath, _) in this.OriginType.GetMembers(flow)) {
//                    var memPath = this.OriginPath.AppendMember(relPath);

//                    // This lifetime is inferred, which means it is being calculated from previous
//                    // lifetimes
//                    writer.RegisterLifetime(flow.LocationLifetimes[memPath], cLifetime);
//                }
//            }
//            else {
//                // This is a new lifetime declaration
//                foreach (var (relPath, _) in this.OriginType.GetMembers(flow)) {
//                    var memPath = this.OriginPath.AppendMember(relPath);

//                    writer.RegisterLifetime(flow.LocationLifetimes[memPath], cLifetime);
//                }
//            }
//        }
//    }

//    public class AssignedLifetimeProducer : LifetimeProducer, ISyntaxDecorator {
//        public Func<ISyntaxTree, FlowFrame, LifetimeBundle> AssignSyntax { get; }

//        public Func<ICSyntax, FlowFrame, ICStatementWriter, ICSyntax> CLifetimeSource { get; }

//        public AssignedLifetimeProducer(IdentifierPath lifetimePath, 
//                                        HelixType baseType,
//                                        LifetimeRole kind, 
//                                        Func<ISyntaxTree, FlowFrame, LifetimeBundle> assign,
//                                        Func<ICSyntax, FlowFrame, ICStatementWriter, ICSyntax> cLifetimeSource)
//            : base(lifetimePath, baseType, kind) {

//            this.AssignSyntax = assign;
//            this.CLifetimeSource = cLifetimeSource;
//        }

//        public override void PostCheckTypes(ISyntaxTree syntax, TypeFrame types) {
//            base.PostCheckTypes(syntax, types);

//            // Go through all the variables and sub variables and set up the lifetimes
//            // correctly
//            foreach (var (relPath, type) in this.OriginType.GetMembers(types)) {
//                if (type.IsValueType(types)) {
//                    continue;
//                }

//                var varPath = this.OriginPath.AppendMember(relPath);

//                // IMPORTANT: Even for inferred variables, the lifetime of the value stored
//                // in those variables is NOT inferred
//                var lifetime = new Lifetime(varPath, 0, LifetimeTarget.StoredValue, LifetimeRole.Relational);

//                // Add this variable's lifetime
//                types.LifetimeRoots[varPath] = lifetime;
//            }
//        }

//        // NOTE: Don't change this to PreAnalyzeFlow. We need this.AssignSyntax
//        // to be flow analyzed for this to run
//        public void PostAnalyzeFlow(ISyntaxTree syntax, FlowFrame flow) {
//            var assignBundle = this.AssignSyntax(syntax, flow);

//            // Go through all the variables and sub variables and set up the lifetimes
//            // correctly
//            foreach (var (relPath, type) in OriginType.GetMembers(flow)) {
//                if (type.IsValueType(flow)) {
//                    continue;
//                }

//                var path = this.OriginPath.AppendMember(relPath);
//                var locationLifetime = flow.LocationLifetimes[path];

//                // IMPORTANT: Even for inferred variables, the lifetime of the value stored
//                // in those variables is NOT inferred
//                var storedValueLifetime = new Lifetime(path, 0, LifetimeTarget.StoredValue, LifetimeRole.Relational);

//                // Add a dependency between this version of the variable lifetime
//                // and the assigned expression. Whenever an alias might occur the
//                // version will be incremented, so this will not be unsafe with
//                // mutable variables
//                flow.LifetimeGraph.RequireOutlives(
//                    assignBundle[relPath],
//                    storedValueLifetime);

//                // Make sure we outlive the location this value is being stored in
//                flow.LifetimeGraph.RequireOutlives(storedValueLifetime, locationLifetime);

//                // Add this variable members's lifetime
//                flow.StoredValueLifetimes[path] = storedValueLifetime;
//            }
//        }

//        public override void PostGenerateCode(ISyntaxTree syntax, ICSyntax result, FlowFrame flow, ICStatementWriter writer) {
//            base.PostGenerateCode(syntax, result, flow, writer);

//            // This is a new lifetime declaration
//            foreach (var (relPath, type) in this.OriginType.GetMembers(flow)) {
//                var memPath = this.OriginPath.AppendMember(relPath);
//                var memAccess = this.CLifetimeSource(result, flow, writer);

//                // Only register lifetimes that exist
//                if (type.IsValueType(flow)) {
//                    continue;
//                }

//                foreach (var segment in relPath.Segments) {
//                    memAccess = new CMemberAccess() {
//                        IsPointerAccess = false,
//                        MemberName = segment,
//                        Target = memAccess
//                    };
//                }

//                memAccess = new CMemberAccess() {
//                    IsPointerAccess = false,
//                    MemberName = "region",
//                    Target = memAccess
//                };

//                writer.RegisterLifetime(flow.StoredValueLifetimes[memPath], memAccess);
//            }
//        }
//    }
//}