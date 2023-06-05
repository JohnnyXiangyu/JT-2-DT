using System.Text;

namespace JT_2_DT.Utils;

public class DataBuilder 
{
	private StringBuilder _builder = new();

	public void Append<T>(T newField) 
	{
		_builder.Append(newField);
		_builder.Append(", ");
	}
	
	public override string ToString() => _builder.ToString();
}
