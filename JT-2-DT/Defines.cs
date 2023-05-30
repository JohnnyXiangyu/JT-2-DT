using System.Runtime.InteropServices;

namespace JT_2_DT;

public static class Defines 
{
	public static string OsSuffix { get 
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
		{
			return "windows.exe";
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) 
		{
			return "linux";
		}
		else 
		{
			throw new NotImplementedException("unsupported operating system");
		}
	}}
}