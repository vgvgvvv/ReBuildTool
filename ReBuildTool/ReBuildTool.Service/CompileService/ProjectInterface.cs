using ReBuildTool.Service.Context;

namespace ReBuildTool.Service.CompileService;

public interface IProjectInterface : IProvideByService
{
	/// <summary>
	/// Fetch declared packages and write the lock, and nothing else. Kept separate from
	/// <see cref="Parse"/>, which also compiles and loads the rule assembly and will scaffold a
	/// default Target/Module for a project that has none - side effects that have no business
	/// happening during a cache-warm or an offline prep run.
	/// </summary>
	void Restore();

	void Parse();
	void Setup();
	void Build(string? targetName = null);
	void Clean();
	void ReBuild(string? targetName = null);
}