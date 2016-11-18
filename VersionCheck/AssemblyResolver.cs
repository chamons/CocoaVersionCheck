using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Mono.Cecil;

namespace VersionCheck
{
	public class AssemblyResolver
	{		
		DefaultAssemblyResolver resolver;
		ReaderParameters readerParams;
		string MonoBundlePath;
		bool Verbose;

		public AssemblyResolver (string monoBundlePath, bool verbose)
		{
			resolver = new DefaultAssemblyResolver ();
			resolver.AddSearchDirectory (monoBundlePath);

			readerParams = new ReaderParameters () { AssemblyResolver = resolver };

			MonoBundlePath = monoBundlePath;
			Verbose = verbose;
		}

		public List<ModuleDefinition> ResolveReferences (string rootAssembly)
		{
			return ResolveReferences (new List<string> () { rootAssembly });
		}

		public List<ModuleDefinition> ResolveReferences (IEnumerable<string> rootAssemblies)
		{
			Stack<ModuleDefinition> modulesToResolve = new Stack<ModuleDefinition> ();
			foreach (var root in rootAssemblies)
				modulesToResolve.Push (ModuleDefinition.ReadModule (root, readerParams));

			List<ModuleDefinition> userModules = new List<ModuleDefinition> ();

			while (modulesToResolve.Count > 0)
			{
				var current = modulesToResolve.Pop ();
				if (userModules.Any (x => x.Name == current.Name))
					continue;

				if (Verbose)
					Console.WriteLine ("Resolving {0}", current.Name);
				
				foreach (var dependency in current.AssemblyReferences.Where (x => !IsBlackListed (x.Name)))
				{
					if (Verbose)
						Console.WriteLine ("\tFound Dependency {0}", dependency.Name);
					
					try
					{
						ModuleDefinition resolvedDependency = ResolveDependency (dependency.Name);
						modulesToResolve.Push (resolvedDependency);
					}
					catch (FileNotFoundException)
					{
						if (Verbose)
							Console.WriteLine ("\t\tUnable to find dependnecy {0}, skipping.", dependency.Name);
					}
				}

				userModules.Add (current);
			}
			return userModules.ToList ();
		}

		ModuleDefinition ResolveDependency (string name)
		{
			string libPath = Path.Combine (MonoBundlePath, name + ".dll");
			return ModuleDefinition.ReadModule (libPath, readerParams);
		}

		static HashSet <string> AssemblyBlacklist = new HashSet <string> { "mscorlib", "Xamarin.Mac", "gtk-sharp", "gdk-sharp", "Xwt", "Microsoft.CSharp", "Mono.Posix", "pango-sharp", "Mono.Cairo", "glib-sharp" };

		bool IsBlackListed (string name)
		{
			if (AssemblyBlacklist.Contains (name))
				return true;
			if (name.StartsWith ("System", StringComparison.Ordinal))
				return true;
			if (name.StartsWith ("Microsoft.Build", StringComparison.Ordinal))
				return true;
			return false;
		}
	}
}
