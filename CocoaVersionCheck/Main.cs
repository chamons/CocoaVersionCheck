using System;
using System.Collections.Generic;
using AppKit;
using Mono.Options;
using System.Linq;
using VersionCheck;

namespace CocoaVersionCheck
{
	static class EntryPoint
	{
		static int Main (string[] args)
		{
			NSApplication.Init ();

			VersionCheckOptions versionCheckOptions = new VersionCheckOptions ();

			var os = new OptionSet () {
				{ "h|?|help", "Displays the help", v => versionCheckOptions.ShowHelp = true},
				{ "v", "Display verbose details", v => versionCheckOptions.Verbose = true},
				{ "p|managed-assembly-path=", "Relative path inside bundle to find managed assemblies if not Contents/MonoBundle",
					v => versionCheckOptions.ManagedAssemblyPath = Optional.Option.Some (v) },
				{ "m|allow-multiple-exe", "Accept bundle even with multiple managed assemblies in path", v => versionCheckOptions.AllowMultipleExecutables = true },
				{ "a|scan-all-assemblies", "Instead of picking the first managed exe and scanning dependencies, scan all managed assemblies in bundle (Implies allow-multiple-exe)",
					v => {
						versionCheckOptions.AllowMultipleExecutables = true;
						versionCheckOptions.ScanAllAssemblies = true;
					}},
			};

			// Ignore any -psn_ arguments, since XS passes those in when debugging
			List<string> unprocessed = os.Parse (args).Where (x => !x.StartsWith ("-psn_", StringComparison.Ordinal)).ToList ();

			if (versionCheckOptions.ShowHelp || unprocessed.Count != 1)
				ShowHelp (os);

			versionCheckOptions.BundlePath = unprocessed[0];

			if (!VersionScanner.IsValidBundle (versionCheckOptions))
				ShowHelp (os);

			if (versionCheckOptions.ScanAllAssemblies && versionCheckOptions.ManagedAssemblyPath.HasValue)
			{
				Console.WriteLine ("scan-all-assemblies recursively scans all assemblies in bundles and thus setting managed-assembly-path is invalid.");
				ShowHelp (os);
			}

			VersionScanner scanner = new VersionScanner (versionCheckOptions);
			scanner.Scan ();
			return scanner.PrintResults ();
		}

		static void ShowHelp (OptionSet os)
		{
			Console.WriteLine ("Usage: CocoaVersionCheck [options] application.app");
			os.WriteOptionDescriptions (Console.Out);
			NSApplication.SharedApplication.Terminate (NSApplication.SharedApplication);
		}
	}
}
