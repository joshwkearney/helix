using Helix;
using Helix.Analysis;
using Helix.Analysis.Flow;

namespace helix_tests {
    [TestClass]
    public class LifetimeTests {
       /* [TestMethod]
        public void TestBadLifetimes() {
            int numTests = 3;

            for (int i = 1; i <= numTests; i++) {
                Assert.ThrowsException<LifetimeException>(() => {
                    CompileFile($"bad_lifetimes_{i}.helix");
                });
            }
        }*/

        [TestMethod]
        public void TestGoodLifetimes() => CompileFile("good_lifetimes.helix");

        private static void CompileFile(string file) {
            var header = File.ReadAllText("../../../../../Resources/Helix.h");
            var contents = File.ReadAllText("./Programs/" + file);
            var parse = new HelixCompiler(header, contents);

            parse.Compile();
        }
    }
}