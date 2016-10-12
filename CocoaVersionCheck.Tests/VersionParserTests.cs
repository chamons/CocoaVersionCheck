using System;
using System.IO;
using Foundation;
using NUnit.Framework;
using VersionCheck;

namespace CocoaVersionCheck.Tests
{
	public class VersionParserTests
	{
		string ResourcePath => NSBundle.MainBundle.ResourcePath;

		[Test]
		public void VersionParser_SmokeTest ()
		{
			Version foundVersion = VersionParser.FindBundleMinVersion (Path.Combine (ResourcePath, "StandardInfo.plist"), false);
			Assert.AreEqual (new Version (10, 11), foundVersion);
		}

		[Test]
		public void VersionParser_NoMinVersion_ReturnsLion ()
		{
			Version foundVersion = VersionParser.FindBundleMinVersion (Path.Combine (ResourcePath, "NoMinVersion.plist"), false);
			Assert.AreEqual (new Version (10, 7), foundVersion);
		}

		[Test]
		public void VersionParser_MultiLineVersion_ReturnsLion ()
		{
			Version foundVersion = VersionParser.FindBundleMinVersion (Path.Combine (ResourcePath, "MinVersionTwoLines.plist"), false);
			Assert.AreEqual (new Version (10, 7), foundVersion);
		}
	}
}
