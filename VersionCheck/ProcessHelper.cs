using System;
using System.Diagnostics;

namespace VersionCheck
{
	internal static class ProcessHelper
	{
		internal static string Run (string path, string[] args)
		{
			return Run (path, String.Join (" ", args));
		}
		
		internal static string Run (string path, string args)
		{
			ProcessStartInfo startInfo = new ProcessStartInfo ()
			{
				FileName = path,
				Arguments = args,
				UseShellExecute = false,
				RedirectStandardOutput = true
			};

			var process = Process.Start (startInfo);

			string output = process.StandardOutput.ReadToEnd ();
			process.WaitForExit ();
			return output;
		}
	}
}
