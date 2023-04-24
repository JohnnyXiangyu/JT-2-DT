using System.Text;

namespace JT_2_DT
{
    internal class MoralGraph
    {
        public List<HashSet<int>> AllEdgesBySmallVar { get; private set; }
        public int VerticeCount { get => _formula.VariableCount; }
        public int EdgeCount
        {
            get
            {
                int edgeCount = 0;
                foreach (HashSet<int> set in AllEdgesBySmallVar)
                {
                    edgeCount += set.Count;
                };
                return edgeCount;
            }
        }

        private readonly Cnf _formula;


        public MoralGraph(Cnf formula)
        {
            _formula = formula;
            AllEdgesBySmallVar = new(_formula.VariableCount);

            Task[] tasks = new Task[_formula.VariableCount];

            for (int i = 0; i < _formula.VariableCount; i++)
            {
                int variable = i;
                var newEdges = new HashSet<int>();

                tasks[variable] = Task.Run(() =>
                {
                    foreach (IEnumerable<int> clause in _formula.Clauses)
                    {
                        if (clause.Any(x => x == variable))
                        {
                            foreach (int other in clause)
                            {
                                if (other > variable)
                                {
                                    newEdges.Add(other);
                                }
                            }
                        }

                    }
                });

                AllEdgesBySmallVar.Add(newEdges);
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void OutputToFile(string outPath)
        {
            using var stream = File.OpenWrite(outPath);
            using var writer = new StreamWriter(stream);

            writer.Write(Serialize());
        }

        public string Serialize()
        {
            StringBuilder builder = new();

            builder.AppendLine($"p tw {_formula.VariableCount} {EdgeCount}");
            for (int v1 = 0; v1 < AllEdgesBySmallVar.Count; v1 ++)
            {
                var neighbours = AllEdgesBySmallVar[v1];
                foreach (int v2 in neighbours)
                {
                    builder.AppendLine($"{v1 + 1} {v2 + 1}");
                }
            }

            return builder.ToString();  
        }
    }
}
