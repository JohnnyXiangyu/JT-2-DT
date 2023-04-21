using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace JT_2_DT
{
    internal class Cnf
    {
        public int VariableCount { get; set; }
        public List<List<int>> Clauses { get; set; }

        public Cnf(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                string[] words = line.Split(' ');
                switch (line[0])
                {
                    case 'p':
                        {
                            // initialize problem setting
                            VariableCount = int.Parse(words[2]);
                            int m = int.Parse(words[3]);
                            Clauses = new List<List<int>>(m);
                            break;
                        }
                    case 'c':
                        break;
                    default:
                        {
                            List<int> newClause = new(words.Length);
                            foreach (string i in words)
                            {
                                if (i[0] != '0')
                                {
                                    newClause.Add(Math.Abs(int.Parse(i)));
                                }
                            }

                            Clauses!.Add(newClause);
                            break;
                        }
                }
            }
        }
    }
}
