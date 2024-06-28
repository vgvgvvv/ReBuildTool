using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.CompileService;

public interface ICppProject : IProvideByService
{
	void Parse();
	void Setup();
	void Build(string? targetName = null);
}