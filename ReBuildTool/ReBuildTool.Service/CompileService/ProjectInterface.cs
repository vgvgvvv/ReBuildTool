using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.CompileService;

public interface IProjectInterface : IProvideByService
{
	void Parse();
	void Setup();
	void Build(string? targetName = null);
	void Clean();
	void ReBuild(string? targetName = null);
}