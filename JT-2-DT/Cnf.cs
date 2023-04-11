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
        public IEnumerable<IEnumerable<int>> Clauses { get; set; }

        public Cnf(string filePath)
        {
            throw new NotImplementedException();
        }
    }
}
