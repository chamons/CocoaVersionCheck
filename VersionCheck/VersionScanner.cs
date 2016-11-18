using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Optional;

namespace VersionCheck
{
	public class VersionScanner
	{
		VersionCheckOptions Options;

		string MonoBundlePath => Path.Combine (Options.BundlePath, Options.ManagedAssemblyPath.ValueOr ("Contents/MonoBundle"));
		string InfoPath => Path.Combine (Options.BundlePath, "Contents/Info.plist");
		bool Verbose => Options.Verbose;

		Dictionary<string, Version> Violations = new Dictionary<string, Version> ();

		public VersionScanner (VersionCheckOptions options)
		{
			if (!IsValidBundle (options))
				throw new InvalidOperationException ();

			Options = options;
		}

		// TODO - Pass info or print detailed reason we're rejecting
		// 
		public static bool IsValidBundle (VersionCheckOptions options)
		{
			try
			{
				if (String.IsNullOrEmpty (options.BundlePath))
					return false;
				
				if (!Directory.Exists (options.BundlePath))
					return false;

				// If we're scanning entire bundle and it exists, just go with it
				if (options.ScanAllAssemblies)
					return true;

				if (!File.Exists (Path.Combine (options.BundlePath, "Contents/Info.plist")))
					return false;

				string monoBundlePath = Path.Combine (options.BundlePath, options.ManagedAssemblyPath.ValueOr ("Contents/MonoBundle"));

				if (!Directory.Exists (monoBundlePath))
					return false;

				if (!options.AllowMultipleExecutables && Directory.GetFiles (monoBundlePath, "*.exe").Length != 1)
					return false;

				return true;
			}
			catch
			{
				return false;
			}
		}

		public void Scan ()
		{
			// 1. Dig out the min version from info.list
			Version minVersion = VersionParser.FindBundleMinVersion (InfoPath, Verbose);

			// 2. Get main.exe and user dll it depends on
			AssemblyResolver resolver = new AssemblyResolver (MonoBundlePath, Verbose);
			List<ModuleDefinition> userModules;

			if (Options.ScanAllAssemblies)
				userModules = resolver.ResolveReferencesRecursively (Options);
			else
				userModules = resolver.ResolveReferences (Directory.GetFiles (MonoBundlePath, "*.exe")[0]);
		
			if (Verbose)
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
							if (Verbose)
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
							if (Verbose)
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
							AddViolation (name, apiVersion);
						break;
					}
					case "UnavailableAttribute":
					{
						var platform = (ObjCRuntime.PlatformName)attribute.ConstructorArguments[0].Value;
						if (platform == ObjCRuntime.PlatformName.MacOSX)
							AddViolation (name, null);
						break;
					}
					default:
						break;
				}
			}
		}

		void AddViolation (string name, Version version)
		{
			if (!Violations.ContainsKey (name))
				Violations.Add (name, version);
		}

		public int PrintResults ()
		{
			foreach (var v in Violations)
			{
				if (v.Value == null)
					Console.WriteLine ("{0} is Unavailable", v.Key);
				else
					Console.WriteLine ("{0} was introduced in {1}", v.Key, v.Value);
			}
			return Violations.Count > 0 ? -1 : 0;
		}
	}
}
