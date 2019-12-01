using Attempt12.DataFormat;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Attempt12.Language {
    public delegate Data LanguageMacro(IDictionary<Data, Data> input);

    public class Scope {
        //public ImmutableDictionary<string, Data> Variables { get; }

        public ImmutableDictionary<IReadOnlyList<string>, LanguageMacro> Macros { get; }

        public Scope() {
            //this.Variables = ImmutableDictionary<string, Data>.Empty;
            this.Macros = ImmutableDictionary<IReadOnlyList<string>, LanguageMacro>.Empty;
        }

        public Scope(ImmutableDictionary<IReadOnlyList<string>, LanguageMacro> macros) {
            //this.Variables = vars;
            this.Macros = macros;
        }

        //public Scope SetVariable(string name, Data value) {
        //    return new Scope(this.Variables.SetItem(name, value), this.Macros);
        //}

        public Scope SetMacro(LanguageMacro macro, params string[] keys) {
            return new Scope(this.Macros.SetItem(keys, macro));
        }
    }
}