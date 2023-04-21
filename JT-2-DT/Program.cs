using JT_2_DT;

UndirectedJtree jtree = new(@"Examples\ex001.td");
Console.WriteLine("tree decomposition loaded");

Cnf formula = new(@"Examples\short_cnf");
MoralGraph graph = new(formula);

Console.WriteLine(graph.Serialize());
