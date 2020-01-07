using System;
using System.Text;

namespace Attempt17.CodeGeneration {
    public interface ICodeWriter {
        ICodeWriter Line(string line);
    }
}