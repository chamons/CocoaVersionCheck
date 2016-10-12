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

			bool verbose = false;
			bool show_help = false;
			var os = new OptionSet () {
				{ "h|?|help", "Displays the help", v => show_help = true},
				{ "v", "Display verbose details", v => verbose = true},
			};

			// Ignore any -psn_ arguments, since XS passes those in when debugging
			List<string> unprocessed = os.Parse (args).Where (x => !x.StartsWith ("-psn_", StringComparison.Ordinal)).ToList ();

			if (show_help || unprocessed.Count != 1 || !VersionScanner.IsValidBundle (unprocessed[0]))
				ShowHelp (os);

			VersionScanner scanner = new VersionScanner (unprocessed[0], verbose);
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
