using Helix.HelixMinusMinus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace helix.common.Hmm {
    public class HmmWriter {
        private readonly Stack<List<IHmmSyntax>> lines = new();
        private readonly List<IHmmSyntax> typeDeclarations = [];
        private readonly List<IHmmSyntax> forwardDeclarations = [];

        public IReadOnlyList<IHmmSyntax> Lines {
            get {
                if (this.lines.Count == 1) {
                    return [..this.typeDeclarations, ..this.forwardDeclarations, ..this.lines.Peek()];
                }
                else {
                    return this.lines.Peek();
                }
            }
        }


        public HmmWriter() {
            this.lines.Push([]);
        }

        public void AddLine(IHmmSyntax line) {
            this.lines.Peek().Add(line);
        }

        public void AddTypeDeclaration(IHmmSyntax syntax) {
            this.typeDeclarations.Add(syntax);
        }

        public void AddFowardDeclaration(IHmmSyntax syntax) {
            this.forwardDeclarations.Add(syntax);
        }

        public void PushBlock() {
            this.lines.Push([]);
        }

        public IReadOnlyList<IHmmSyntax> PopBlock() {
            if (this.lines.Count <= 1) {
                return [];
            }

            return this.lines.Pop();
        }
    }
}
