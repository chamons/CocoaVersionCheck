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
			MonoBundlePath = monoBundlePath;
			Verbose = verbose;
		}

		List<string> GenerateDirectoryList (IEnumerable<string> assemblies)
		{
			HashSet<string> dir = new HashSet<string> ();
			foreach (var assembly in assemblies)
				dir.Add (Path.GetDirectoryName (assembly));
			return dir.ToList ();
		}

		public List<ModuleDefinition> ResolveReferencesEntireBundle (string bundlePath)
		{
			var allFiles = Directory.EnumerateFiles (bundlePath, "*", SearchOption.AllDirectories);
			var managedAssemblies = allFiles.Where (x => x.ToLower ().EndsWith (".exe", StringComparison.Ordinal) || x.ToLower ().EndsWith (".dll", StringComparison.Ordinal));
			
			resolver = new DefaultAssemblyResolver ();

			foreach (var dir in GenerateDirectoryList (managedAssemblies))
				resolver.AddSearchDirectory (dir);

			readerParams = new ReaderParameters () { AssemblyResolver = resolver };

			return ResolveReferences (managedAssemblies, false);
		}

		public List<ModuleDefinition> ResolveReferences (string rootAssembly)
		{
			resolver = new DefaultAssemblyResolver ();
			resolver.AddSearchDirectory (MonoBundlePath);

			readerParams = new ReaderParameters () { AssemblyResolver = resolver };

			return ResolveReferences (new List<string> () { rootAssembly }, true);
		}

		List<ModuleDefinition> ResolveReferences (IEnumerable<string> rootAssemblies, bool resolveDependencies)
		{
			Stack<ModuleDefinition> modulesToResolve = new Stack<ModuleDefinition> ();
			foreach (var root in rootAssemblies)
			{
				ModuleDefinition rootDef = SafeResolveDependency (root);
				if (rootDef != null)
					modulesToResolve.Push (rootDef);
			}

			List<ModuleDefinition> userModules = new List<ModuleDefinition> ();

			while (modulesToResolve.Count > 0)
			{
				var current = modulesToResolve.Pop ();
				if (userModules.Any (x => x.Name == current.Name) || IsBlackListed (current.Name))
					continue;

				if (Verbose)
					Console.WriteLine ("Resolving {0}", current.Name);

				// If we're scanning entire bundle, no need to resolve dependencies since will be covered 
				if (resolveDependencies)
				{
					foreach (var dependency in current.AssemblyReferences.Where (x => !IsBlackListed (x.Name)))
					{
						if (Verbose)
							Console.WriteLine ("\tFound Dependency {0}", dependency.Name);

						ModuleDefinition resolvedDependency = SafeResolveDependency (dependency.Name + ".dll");
						if (resolvedDependency != null)
							modulesToResolve.Push (resolvedDependency);
					}
				}

				userModules.Add (current);
			}
			return userModules.ToList ();
		}

		ModuleDefinition SafeResolveDependency (string libPath)
		{
			try
			{
				return ModuleDefinition.ReadModule (libPath, readerParams);
			}
			catch (Exception e)
			{
				if (Verbose)
					Console.WriteLine ("\t\tUnable to find dependency {0}, skipping. {1}", Path.GetFileName (libPath), e.Message);
			}
			return null;
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
