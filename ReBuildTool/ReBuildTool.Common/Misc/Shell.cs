﻿using System.Diagnostics;
using NiceIO;
using ResetCore.Common;

namespace ReBuildTool.Common;

public class Shell : IDisposable
{
	public enum Status
	{
		Waiting,
		Running,
		Finished
	}
	
	public static Shell Create()
	{
		return new Shell();
	}
	
	private Shell()
	{
		CurrentStatus = Status.Waiting;
	}
	
	public Shell WithProgram(NPath program)
	{
		if (CurrentStatus != Status.Waiting)
		{
			throw new Exception("Shell is already running or finished.");
		}
		Program = program;
		return this;
	}
	
	public Shell WithProgram(string program)
	{
		if (CurrentStatus != Status.Waiting)
		{
			throw new Exception("Shell is already running or finished.");
		}
		Program = program;
		return this;
	}
	
	public Shell WithEnvVars(Dictionary<string, string> envVars)
	{
		if (CurrentStatus != Status.Waiting)
		{
			throw new Exception("Shell is already running or finished.");
		}
		EnvVars.AddRange(envVars);
		return this;
	}
	
	public Shell WithEnvVars(string key, string value)
	{
		if (CurrentStatus != Status.Waiting)
		{
			throw new Exception("Shell is already running or finished.");
		}
		EnvVars.Add(key, value);
		return this;
	}

	public Shell WithArguments(IEnumerable<string> arguments)
	{
		if (CurrentStatus != Status.Waiting)
        {
        	throw new Exception("Shell is already running or finished.");
        }
		Arguments.AddRange(arguments);
		return this;
	}
	
	public Shell AppendArgument(string argument)
	{
		if (CurrentStatus != Status.Waiting)
		{
			throw new Exception("Shell is already running or finished.");
		}
		Arguments.Add(argument);
		return this;
	}

	public Shell AppendArgumentPair(string key, string value)
	{
		if (CurrentStatus != Status.Waiting)
		{
			throw new Exception("Shell is already running or finished.");
		}
		AppendArgument(key);
		AppendArgument(value);
		return this;
	}

	public Shell Execute()
	{
		if (CurrentStatus != Status.Waiting)
		{
			throw new Exception("Shell is already running or finished.");
		}
		if (Process != null)
		{
			Process.Dispose();
			Process = null;
		}
		Process = new Process();
		
		ProcessStartInfo startInfo = new ProcessStartInfo();
		startInfo.FileName = Program.ToString();
		startInfo.UseShellExecute = false; 
		startInfo.RedirectStandardOutput = true; 
        startInfo.CreateNoWindow = true;
        startInfo.Arguments = string.Join(' ', Arguments);
        foreach (var (key, value) in EnvVars)
		{
	        startInfo.Environment.Add(key, value);
		}

        Process.StartInfo = startInfo;
        Process.OutputDataReceived += (sender, args) =>
        {
	        if (args.Data != null)
	        {
		        Log.Info(args.Data);
	        }
        };
        
        Process.ErrorDataReceived += (sender, args) =>
        {
	        if (args.Data != null)
	        {
		        Log.Error(args.Data);
	        }
        };
        
        Process.Exited += (sender, args) =>
        {
	        CurrentStatus = Status.Finished;
        };
        
        Process.Start();
        CurrentStatus = Status.Running;
		return this;
	}

	public Shell WaitForEnd()
	{
		if (CurrentStatus != Status.Running)
		{
			throw new Exception("Shell is not running.");
		}
		if (Process != null)
		{
			Process.WaitForExit();
			CurrentStatus = Status.Finished;
		}
		return this;
	}

	public bool IsSuccess()
	{
		if (CurrentStatus != Status.Finished)
		{
			throw new Exception("Shell is not finished.");
		}
		if (Process != null)
		{
			if (!Process.HasExited)
			{
				return false;
			}
			return Process.ExitCode == 0;
		}
		return false;
	}
	
	public string Program { get; private set; }
	public List<string> Arguments { get; private set; } = new List<string>();
	public Process? Process { get; private set; }
	public Status CurrentStatus { get; private set; }
	public  Dictionary<string, string> EnvVars { get; } = new();
	
	public void Dispose()
	{
		Process?.Dispose();
	}
}