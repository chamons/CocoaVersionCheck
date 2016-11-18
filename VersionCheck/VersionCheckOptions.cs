using System;
namespace VersionCheck
{
	public class VersionCheckOptions
	{
		public bool Verbose { get; set; }
		public bool ShowHelp { get; set;}
		public bool AllowMultipleExecutables { get; set;}
		public bool ScanAllAssemblies { get; set; }

		public Optional.Option<string> ManagedAssemblyPath { get; set;}
		public string BundlePath { get; set; }

		public VersionCheckOptions ()
		{
			ManagedAssemblyPath = Optional.Option.None<string> ();
		}
	}
}
