namespace JT_2_DT
{
    internal class MoralGraph
    {
        public Dictionary<int, HashSet<int>> AllEdgesBySmallVar { get; private set; }
        public int VerticeCount { get => _formula.VariableCount; }
        public int EdgeCount
        {
            get
            {
                int edgeCount = 0;
                foreach ((int _, HashSet<int> set) in AllEdgesBySmallVar)
                {
                    edgeCount += set.Count;
                };
                return edgeCount;
            }
        }

        private readonly Cnf _formula;

        public MoralGraph(string CnfPath)
        {
            _formula = new(CnfPath);
            AllEdgesBySmallVar = new();

            List<Task> tasks = new List<Task>(_formula.VariableCount);

            for (int i = 1; i <= _formula.VariableCount; i++)
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

                AllEdgesBySmallVar[variable] = newEdges;
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void OutputToFile(string outPath)
        {
            using var stream = File.OpenWrite(outPath);
            using var writer = new StreamWriter(stream);

            writer.WriteLine($"p tw {_formula.VariableCount} {EdgeCount}");
            foreach ((int v1, HashSet<int> neighbours) in AllEdgesBySmallVar)
            {
                foreach (int v2 in neighbours)
                {
                    writer.WriteLine($"{v1} {v2}");
                }
            }
        }
    }
}
