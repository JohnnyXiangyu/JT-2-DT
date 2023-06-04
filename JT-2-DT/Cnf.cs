namespace JT_2_DT
{
	public class Cnf
	{
		public int VariableCount { get; set; }
		public List<List<int>> Clauses { get; set; }

		public Cnf(string filePath)
		{
			string[] lines = File.ReadAllLines(filePath);

			List<List<int>> rawClauses = new(); // fail safe
			List<int> newClause = new();

			// read the signed variables
			foreach (var line in lines)
			{
				List<string> words = new(line.Trim().Split(' '));
				words.RemoveAll(x => x.Length == 0);
				
				if (words.Count == 0) { continue; }
				
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
							foreach (string word in words)
							{
								if (word == " ") 
								{
									continue;
								}
								if (word == "0")
								{
									rawClauses.Add(newClause);
									newClause = new();
								}
								else 
								{
									newClause.Add(int.Parse(word));
								}
							}
							break;
						}
				}
			}
			
			// pre-processing
			Clauses = rawClauses.SelectMany(x => 
			{
				HashSet<int> seenLiterals = new();
				bool valid = true;
				foreach (int literal in x)
				{
					if (seenLiterals.Contains(-literal)) 
					{
						valid = false;
						break;
					}
					seenLiterals.Add(literal);
				}
				
				if (!valid) 
				{
					return Array.Empty<List<int>>();
				}
				
				return new List<int>[] 
				{
					x
				};
			}).Select(x => 
			{
				for (int i = 0; i < x.Count; i ++) 
				{
					x[i] = Math.Abs(x[i]);
				}
				return x;
			}).ToList();
		}
	}
}
