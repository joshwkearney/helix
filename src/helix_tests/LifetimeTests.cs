using Helix;
using Helix.Analysis;
using Helix.Analysis.Flow;

namespace helix_tests {
    [TestClass]
    public class LifetimeTests {
       [TestMethod]
        public void TestBadLifetimes() {
            Assert.ThrowsException<HelixException>(() => {
                CompileFile($"bad_lifetimes.helix");
            });
        }

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