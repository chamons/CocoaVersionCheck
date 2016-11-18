using System;

namespace VersionCheck
{
	static class VersionParser
	{
		internal static Version FindBundleMinVersion (string infoPath, bool verbose)
		{
			string[] plistText = ProcessHelper.Run ("/usr/bin/plutil", "-convert xml1 -o - \"" + infoPath + "\"").Split ('\n');

			int envIndex = -1;
			for (int i = 0; i < plistText.Length; ++i)
			{
				if (plistText[i].Contains ("LSMinimumSystemVersion"))
				{
					envIndex = i + 1;
					break;
				}
			}

			if (envIndex == -1)
			{
				Console.WriteLine ("Warning - Unable to find LSMinimumSystemVersion in Info.plist. Assuming 10.7");
				return new Version (10, 7);
			}

			string minVersionLine = plistText[envIndex];

			Version version;
			if (!Version.TryParse (minVersionLine.Replace ("</string>", "").Replace ("<string>", ""), out version))
			{
				Console.WriteLine ("Warning - Unable to parse LSMinimumSystemVersion in Info.plist. Assuming 10.7");
				return new Version (10, 7);
			}

			if (verbose)
				Console.WriteLine ("Minimum Version - {0}\n", version);

			return version;
		}
	}
}
