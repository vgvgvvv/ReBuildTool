using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Setup.Configuration;
using UniToLua.Common;

namespace ReBuildTool.Test;

public class TestMsvcSDK
{
	[SetUp]
	public void Setup()
	{
		
	}

	[Test]
	public void TestVSInstallPaths()
	{
		foreach (var (version, path) in GetVisualStudioInstallPaths())
		{
			Log.Info($"version :{version} path :{path}");
		}
	}
	
	private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
	public static IEnumerable<(string, string)> GetVisualStudioInstallPaths() 
	{
		var result = new List<(string, string)>();

		try
		{
			var query = new SetupConfiguration() as ISetupConfiguration2;
			var e = query.EnumAllInstances();

			int fetched;
			var instances = new ISetupInstance[1];

			do
			{
				e.Next(1, instances, out fetched);

				if (fetched > 0)
				{
					var instance2 = (ISetupInstance2)instances[0];
					result.Add((instance2.GetInstallationVersion(), instance2.GetInstallationPath()));
				}
			}
			while (fetched > 0);
		}
		catch (COMException ex) when (ex.HResult == REGDB_E_CLASSNOTREG)
		{
			throw;
		}
		catch (Exception)
		{
			throw;
		}

		return result;
	}
}