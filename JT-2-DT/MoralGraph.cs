using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JT_2_DT
{
    internal class MoralGraph
    {
        public Dictionary<int, HashSet<int>> AllEdgesBySmallVar { get; private set; }

        public MoralGraph(string CnfPath)
        {
            Cnf formula = new(CnfPath);
            AllEdgesBySmallVar = new();

            List<Task> tasks = new List<Task>(formula.VariableCount);

            for (int i = 1; i <= formula.VariableCount; i++)
            {
                int variable = i;
                var newEdges = new HashSet<int>();

                tasks[variable] = Task.Run(() =>
                {
                    foreach (IEnumerable<int> clause in formula.Clauses)
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
            throw new NotImplementedException();
        }
    }
}
