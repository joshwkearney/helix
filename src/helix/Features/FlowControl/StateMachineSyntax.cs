using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helix.Analysis;
using Helix.Analysis.Types;
using Helix.Features.Primitives;
using Helix.Generation;
using Helix.Generation.Syntax;
using Helix.Parsing;

namespace Helix.Features.FlowControl {
    public class StateMachineSyntax : ISyntaxTree {
        private readonly IReadOnlyDictionary<int, ConditionalState> conditions;
        private readonly IReadOnlyDictionary<int, ConstantState> constants;
        private readonly int finalState;
        private readonly bool isTypeChecked;

        private IEnumerable<int> Keys {
            get {
                return this.conditions.Keys
                    .Concat(this.constants.Keys)
                    .Distinct()
                    .OrderBy(x => x);
            }
        }

        public TokenLocation Location { get; }

        public IEnumerable<ISyntaxTree> Children {
            get {
                foreach (var key in this.Keys) {
                    if (this.constants.ContainsKey(key)) {
                        yield return this.constants[key].Expression;
                    }
                    else {
                        yield return this.conditions[key].Condition;
                    }
                }
            }
        }

        public bool IsPure => false;

        public StateMachineSyntax(TokenLocation loc, FlowRewriter flow, bool isTypeChecked = false) {
            this.Location = loc;
            this.finalState = flow.NextState;
            this.conditions = flow.ConditionalStates;
            this.constants = flow.ConstantStates;
            this.isTypeChecked = isTypeChecked;
        }

        public ISyntaxTree ToRValue(SyntaxFrame types) {
            if (!this.isTypeChecked) {
                throw TypeCheckingErrors.RValueRequired(this.Location);
            }

            return this;
        }

        public ISyntaxTree CheckTypes(SyntaxFrame types) {
            if (this.isTypeChecked) {
                return this;
            }

            // Add all the if statements to types
            foreach (var cond in this.conditions) {
                types.IfBranches.Add(cond.Value.IfId, new IfBranches() { 
                    TrueBranch = new VoidLiteral(this.Location).CheckTypes(types),
                    FalseBranch = new VoidLiteral(this.Location).CheckTypes(types)
                });
            }

            // Use this to remake all the states
            var flow = new FlowRewriter();
            flow.NextState = this.finalState;

            foreach (var key in this.Keys) {
                if (this.constants.TryGetValue(key, out var constant)) {
                    flow.ConstantStates[key] = new ConstantState() {
                        NextState = constant.NextState,
                        Expression = constant.Expression.CheckTypes(types).ToRValue(types)
                    };
                }
                else {
                    var condition = this.conditions[key];

                    flow.ConditionalStates[key] = new ConditionalState(
                        condition.Condition
                            .CheckTypes(types)
                            .ToRValue(types)
                            .UnifyTo(PrimitiveType.Bool, types),
                        condition.IfId,
                        condition.PositiveState,
                        condition.NegativeState
                    );
                }
            }

            var result = new StateMachineSyntax(this.Location, flow, true);

            types.ReturnTypes[result] = PrimitiveType.Void;
            return result;
        }

        public ICSyntax GenerateCode(SyntaxFrame types, ICStatementWriter writer) {
            if (!this.isTypeChecked) {
                throw new InvalidOperationException();
            }

            // Declare all the if variables
            foreach (var cond in this.conditions) {
                var path = cond.Value.IfId;
                var type = types.IfBranches[path].ReturnType;

                if (type != null) {
                    writer.WriteStatement(new CVariableDeclaration() {
                        Name = writer.GetVariableName(path),
                        Type = writer.ConvertType(type)
                    });
                }
            }

            writer.WriteEmptyLine();

            foreach (var key in this.Keys) {
                writer.WriteStatement(new CLabel() {
                    Value = "state" + key
                });

                if (this.constants.TryGetValue(key, out var constant)) {
                    constant.Expression.GenerateCode(types, writer);

                    writer.WriteStatement(new CGoto() {
                        Value = "state" + constant.NextState
                    });
                }
                else {
                    var cond = this.conditions[key];

                    writer.WriteStatement(new CIf() { 
                        Condition = cond.Condition.GenerateCode(types, writer),
                        IfTrue = new[] {
                            new CGoto() { Value = "state" + cond.PositiveState }
                        },
                        IfFalse = new[] {
                            new CGoto() { Value = "state" + cond.NegativeState }
                        }
                    });
                }

                writer.WriteEmptyLine();
            }

            writer.WriteStatement(new CLabel() {
                Value = "state" + this.finalState
            });

            return new CIntLiteral(0);
        }
    }

    public record ConditionalState {
        public ISyntaxTree Condition { get; }

        public IdentifierPath IfId { get; }

        public int PositiveState { get; }

        public int NegativeState { get; }

        public ConditionalState(ISyntaxTree condition, IdentifierPath ifid, int pos, int neg) {
            this.Condition = condition;
            this.IfId = ifid;
            this.PositiveState = pos;
            this.NegativeState = neg;
        }
    }

    public record ConstantState {
        public ISyntaxTree Expression { get; init; }

        public int NextState { get; init; }
    }

    public class FlowRewriter {
        public Dictionary<int, ConditionalState> ConditionalStates { get; } = new();

        public Dictionary<int, ConstantState> ConstantStates { get; } = new();

        public int BreakState { get; set; } = -1;

        public int ContinueState { get; set; } = -1;

        public int ReturnState { get; set; } = -1;

        public int NextState { get; set; } = 0;

        public void OptimizeStates() {
            // Get all the redundant keys
            var badKeys = this.ConstantStates
                .Where(x => x.Value.Expression.IsPure)
                .Select(x => x.Key)
                .ToHashSet();

            // Get the next good key for each constant state
            foreach (var (key, state) in this.ConstantStates.ToArray()) {
                if (!badKeys.Contains(key)) {
                    this.ConstantStates[key] = new ConstantState() {
                        Expression = state.Expression,
                        NextState = FindGoodState(state.NextState)
                    };
                }
            }

            // Get the next good key for each conditional state
            foreach (var (key, state) in this.ConditionalStates.ToArray()) {
                this.ConditionalStates[key] = new ConditionalState(
                    state.Condition,
                    state.IfId,
                    FindGoodState(state.PositiveState),
                    FindGoodState(state.NegativeState)
                );
            }

            // Remove all the redundant keys
            foreach (var state in badKeys) {
                this.ConstantStates.Remove(state);
            }

            int FindGoodState(int start) {
                while (badKeys.Contains(start)) {
                    start = this.ConstantStates[start].NextState;
                }

                return start;
            }
        }
    }
}
