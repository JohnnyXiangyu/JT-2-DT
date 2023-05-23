namespace JT_2_DT
{
    public class Cnf
    {
        public int VariableCount { get; set; }
        public List<List<int>> Clauses { get; set; }

        public Cnf(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);

            Clauses = new(); // fail safe

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
                            // register a clause
                            List<int> newClause = new(words.Length);
                            foreach (string word in words)
                            {
                                if (word[0] != '0')
                                {
                                    // use 1-base variable notation, but still convert them to abs
                                    // since we don't care about the original clause as long as we
                                    // have a valid mapping
                                    newClause.Add(Math.Abs(int.Parse(word))); 
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
