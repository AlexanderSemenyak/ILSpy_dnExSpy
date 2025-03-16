using System.Collections.Generic;

using dnlib.DotNet;

using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ICSharpCode.Decompiler
{
	public sealed class TypeSystemCache
	{
		private readonly Dictionary<ModuleDef, (int Version, IDecompilerTypeSystem TypeSystem)> cache =
			new Dictionary<ModuleDef, (int Version, IDecompilerTypeSystem TypeSystem)>();

		public IDecompilerTypeSystem GetTypeSystem(ModuleDef module, DecompilerSettings settings)
		{
			lock (cache)
			{
				if (cache.TryGetValue(module, out var entry) && entry.Version == settings.SettingsVersion)
					return entry.TypeSystem;

				var typeSystem = new DecompilerTypeSystem(new MetadataFile(module), settings);
				cache[module] = (settings.SettingsVersion, typeSystem);
				return typeSystem;
			}
		}
	}
}
