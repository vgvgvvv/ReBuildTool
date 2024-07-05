using System.Reflection;
using ReCSharpCommon.Result;
using ResetCore.Common;


namespace ReBuildTool.Service.Context;

public interface IService
{
	
}

public interface IProvideByService
{
	
}

public partial class ServiceContext : Singleton<ServiceContext>
{
	public override void Init()
	{
		if (InitFromIni())
		{
			return;
		}
		
		InitByDefault();
	}

	public Result<T> Create<T>(params object[] args) where T: class, IProvideByService
	{
		if (!TypeMap.TryGetValue(typeof(T), out var type))
		{
			return Result<T>.Fail($"cannot find type{typeof(T).Name} in map");
		}

		var paramTypes = args.Select(obj => obj.GetType()).ToArray();
		ConstructorInfo constructor = null;
		foreach (var con in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
			         .Concat(type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)))
		{
			var paramList = con.GetParameters();
			if (paramList.Length != paramTypes.Length)
			{
				continue;
			}
			var match = paramList
				.Zip(paramTypes, paramList)
				.All(tuple => tuple.First.ParameterType == tuple.Second);
			if (match)
			{
				constructor = con;
				break;
			}
		}
		
		if (constructor == null)
		{
			return Result.Fail<T>("cannot find constructor");
		}

		var result  = constructor.Invoke(args) as T;
		if (result == null)
		{
			return Result.Fail<T>("invoke constructor failed");
		}

		return Result.Ok(result);
	}
	
	public void RegisterType<T, U>() where U : T where T: IProvideByService
	{
		TypeMap[typeof(T)] = typeof(U);
	}

	public Result RegisterType<T>(Assembly assembly, string typeName)
	{
		var type = assembly.GetType(typeName);
		if (type == null)
		{
			return Result.Fail();
		}
		TypeMap[typeof(T)] = type;
		return Result.Ok();
	}

	
	public void UnRegisterType<T>() where T : IProvideByService
	{
		TypeMap.Remove(typeof(T));
	}
	
	public Result<T> FindService<T>() where T : IService
	{
		var type = typeof(T);
		if (Services.TryGetValue(typeof(T), out var service))
		{
			return Result.Ok((T) service);
		}

		return Result<T>.Fail("cannot find service");
	}
	
	public void RegisterService<T>(T service) where T : IService
	{
		Services[typeof(T)] = service;
	}

	public Result RegisterService<T>(Assembly assembly, string typeName) where T : class, IService
	{
		var instance = assembly.CreateInstance(typeName) as T;
		if (instance == null)
		{
			return Result.Fail();
		}
		Services[typeof(T)] = instance;
		return Result.Ok();
	}
	
	public void UnRegisterService<T>() where T : IService
	{
		Services.Remove(typeof(T));
	}
	
	private Dictionary<Type, Type> TypeMap { get; } = new();
	
	private Dictionary<Type, IService> Services { get; } = new();
}