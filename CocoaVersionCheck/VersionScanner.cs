using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;

namespace CocoaVersionCheck
{
	public class VersionScanner
	{
		string BundlePath;
		string MonoBundlePath => Path.Combine (BundlePath, "Contents/MonoBundle");
		string InfoPath => Path.Combine (BundlePath, "Contents/Info.plist");

		DefaultAssemblyResolver resolver;
		Dictionary<string, Version> Violations = new Dictionary<string, Version> ();

		public VersionScanner (string bundlePath)
		{
			if (!IsValidBundle (bundlePath))
				throw new InvalidOperationException ();
			
			BundlePath = bundlePath;

			resolver = new DefaultAssemblyResolver ();
			resolver.AddSearchDirectory (MonoBundlePath);
		}

		// TODO - Pass info or print detailed reason we're rejecting
		public static bool IsValidBundle (string bundlePath)
		{
			try
			{
				if (String.IsNullOrEmpty (bundlePath))
					return false;
				
				if (!Directory.Exists (bundlePath))
					return false;

				if (!File.Exists (Path.Combine (bundlePath, "Contents/Info.plist")))
					return false;

				string monoBundlePath = Path.Combine (bundlePath, "Contents/MonoBundle");

				if (!Directory.Exists (monoBundlePath))
					return false;

				if (Directory.GetFiles (monoBundlePath, "*.exe").Length != 1)
					return false;

				return true;
			}
			catch
			{
				return false;
			}
		}

		// TODO - Right now we're just looking at static linkages to exe. We should also let user to pass in additional
		// libs to scan. 
		public void Scan ()
		{
			// 1. Dig out the min version from info.list
			Version minVersion = VersionParser.FindBundleMinVersion (InfoPath);

			// 2. Get main.exe and user dll it depends on
			string mainExecutable = Directory.GetFiles (MonoBundlePath, "*.exe")[0];
			ModuleDefinition mainDef = ModuleDefinition.ReadModule (mainExecutable, new ReaderParameters () { AssemblyResolver = resolver });

			List<ModuleDefinition> userModules = new List<ModuleDefinition> () { mainDef };
			userModules.AddRange (mainDef.AssemblyReferences.Where (x => !AssemblyBlacklist.Contains (x.Name)). Select (x => ResolveDependency (x.Name)));

			if (EntryPoint.Verbose)
				Console.WriteLine ("User Assemblies Resolved: {0}", String.Join (" ", userModules.Select (x => x.Name)));

			// 3. Reflect over them all, looking for Xamarin.Mac references and check for attributes
			foreach (var module in userModules)
			{
				foreach (var refType in module.GetTypeReferences ().Where (x => x.Scope.Name == "Xamarin.Mac"))
				{
					var resolvedType = refType.Resolve ();
					CheckAttributes (minVersion, refType.Name, resolvedType.CustomAttributes);
				}

				foreach (var memberType in module.GetMemberReferences ().Where (x => x.DeclaringType.Scope.Name == "Xamarin.Mac"))
				{
					var resolvedType = memberType.DeclaringType.Resolve ();

					if (memberType is MemberReference)
					{
						MethodDefinition method = resolvedType.Methods.FirstOrDefault (x => x.Name == memberType.Name);
						if (method == null)
						{
							if (EntryPoint.Verbose)
								Console.WriteLine ("Unable to resolve: {0} on {1}", memberType.Name, memberType.DeclaringType);
							continue;
						}
						CheckAttributes (minVersion, resolvedType.Name + "." + memberType.Name, method.CustomAttributes);
					}
					else if (memberType is PropertyReference)
					{
						PropertyDefinition property = resolvedType.Properties.FirstOrDefault (x => x.Name == memberType.Name);
						if (property == null)
						{
							if (EntryPoint.Verbose)
								Console.WriteLine ("Unable to resolve: {0} on {1}", memberType.Name, memberType.DeclaringType);
							continue;
						}

						CheckAttributes (minVersion, resolvedType.Name + "." + property.Name, property.CustomAttributes);
					}
					else
					{
						throw new NotImplementedException ();
					}
				}
			}
		}

		static string[] AssemblyBlacklist = new string[] { "mscorlib", "System.Core", "System", "System.Net.Http", "System.Xml", "Xamarin.Mac" };

		ModuleDefinition ResolveDependency (string name)
		{
			string libPath = Path.Combine (MonoBundlePath, name + ".dll");
			return ModuleDefinition.ReadModule (libPath, new ReaderParameters () { AssemblyResolver = resolver });
		}

		void CheckAttributes (Version minVersion, string name, IEnumerable<CustomAttribute> attributes)
		{
			foreach (var attribute in attributes)
			{
				switch (attribute.AttributeType.Name)
				{
					// (PlatformName platform, int majorVersion, int minorVersion, ...
					case "IntroducedAttribute":
					{
						int major = (int)attribute.ConstructorArguments[1].Value;
						int minor = (int)attribute.ConstructorArguments[2].Value;
						Version apiVersion = new Version (major, minor);
						if (apiVersion > minVersion)
							Violations.Add (name, apiVersion);
						break;
					}
					case "UnavailableAttribute":
					{
						int platform = (int)attribute.ConstructorArguments[0].Value;
						if (platform == 1)
							Violations.Add (name, null);
						break;
					}
					default:
						break;
				}
			}
		}

		public int PrintResults ()
		{
			foreach (var v in Violations)
			{
				if (v.Value == null)
					Console.Write ("{0} is Unavailable", v.Key);
				else
					Console.Write ("{0} was introduced in {1} ", v.Key, v.Value);
				return -1;
			}
			return 0;
		}
	}
}
