using System;
using System.Diagnostics;

namespace CocoaVersionCheck
{
	public static class ProcessHelper
	{
		public static string Run (string path, string[] args)
		{
			return Run (path, String.Join (" ", args));
		}
		
		public static string Run (string path, string args)
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
