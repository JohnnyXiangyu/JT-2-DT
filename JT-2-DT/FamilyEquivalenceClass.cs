namespace JT_2_DT;

public class FamilyEquivalenceClass 
{
	public HashSet<int> MasterFamily { get; set; } = new();
	public List<int> SubsumedClauses { get; set; } = new();
	public int MasterClause { get; set; } = 0;
	// public int Node { get; init; } = 0;
}