using System;
using AppKit;
using Foundation;
using GuiUnit;

namespace CocoaVersionCheck.Tests
{
	static class MainClass
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();
			NSRunLoop.Main.InvokeOnMainThread (RunTests);
			NSApplication.Main (args);
		}

		static void RunTests ()
		{
			TestRunner.MainLoop = new NSRunLoopIntegration ();
			TestRunner.Main (new[] {
				typeof(MainClass).Assembly.Location,
				"-labels",
				"-noheader"
			});
		}

		class NSRunLoopIntegration : NSObject, IMainLoopIntegration
		{
			public void InitializeToolkit ()
			{
			}

			public void RunMainLoop ()
			{
			}

			public void InvokeOnMainLoop (InvokerHelper helper)
			{
				NSApplication.SharedApplication.InvokeOnMainThread (helper.Invoke);
			}

			public void Shutdown ()
			{
				Environment.Exit (TestRunner.ExitCode);
			}
		}
	}
}
