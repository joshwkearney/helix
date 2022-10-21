using Trophy;

var header = File.ReadAllText("Resources/Trophy.h");
var contents = File.ReadAllText("Resources/Program.trophy");
var parse = new TrophyCompiler(header, contents).Compile();

File.WriteAllText("Resources/Program.c", parse);

Console.WriteLine(parse);
Console.Read();