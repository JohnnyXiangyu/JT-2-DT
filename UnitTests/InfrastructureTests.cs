using JT_2_DT;

namespace UnitTests;

[TestClass]
public class InfrastructureTests
{
    [TestMethod]
    public void JtreeImplementationShort()
    {
        Dtree dtree = new(Path.Combine("Examples", "short_jtree.txt"), new List<List<int>> { new() { 0, 1 }, new() { 2, 3 }, new() { 3, 4 } });
        foreach (var line in dtree.SerializeAsDtree())
        {
            Console.WriteLine(line());
        }
    }

    [TestMethod]
    public void JtreeImplementationLong()
    {
        Dtree dtree = new(Path.Combine("Examples", "ex001.td"), new List<List<int>> { new() { 0, 1 }, new() { 2, 3 }, new() { 3, 4 } });
        foreach (var line in dtree.SerializeAsDtree())
        {
            Console.WriteLine(line());
        }
    }

    [TestMethod]
    public void MoralGraphImplementation()
    {
        Cnf formula = new(Path.Combine("Examples", "short_cnf"));
        MoralGraph graph = new(formula);
        Console.WriteLine(graph.Serialize());
    }


}