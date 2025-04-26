using Helix;

var header = File.ReadAllText("../../../../../resources/Helix.h");
var contents = File.ReadAllText("../../../../../resources/Program.helix");

try {
    var parse = new HelixCompiler(header, contents).Compile();

    File.WriteAllText("../../../../../resources/Program.c", parse);
    Console.WriteLine(parse);
}
catch (HelixException ex) {
    Console.WriteLine(ex.Message);
}

//Console.Read();