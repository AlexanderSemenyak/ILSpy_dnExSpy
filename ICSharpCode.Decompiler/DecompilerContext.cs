using System.Collections.Generic;
using System.Threading;

using dnlib.DotNet;

using dnSpy.Contracts.Decompiler;

using ICSharpCode.Decompiler.CSharp;

namespace ICSharpCode.Decompiler
{
	public class DecompilerContext
	{
		public DecompilerContext(int settingsVersion, ModuleDef currentModule, MetadataTextColorProvider metadataTextColorProvider = null)
			: this(settingsVersion, currentModule, metadataTextColorProvider, false)
		{
		}

		public DecompilerContext(int settingsVersion, ModuleDef currentModule, MetadataTextColorProvider metadataTextColorProvider, bool calculateILSpans)
		{
			this.Settings = new DecompilerSettings(LanguageVersion.CSharp11_0)
				{ FileScopedNamespaces = false, NumericIntPtr = false };
			this.UsingNamespaces = new List<string>();
			this.SettingsVersion = settingsVersion;
			this.CurrentModule = currentModule;
			this.CalculateILSpans = calculateILSpans;
			this.Cache = new DecompilerCache(this);
			this.MetadataTextColorProvider = metadataTextColorProvider ?? CSharpMetadataTextColorProvider.Instance;
		}

		public void Reset()
		{
			this.CurrentModule = null;
			this.CancellationToken = CancellationToken.None;
			this.CurrentType = null;
			this.Settings = new DecompilerSettings();
			this.UsingNamespaces.Clear();
			this.Cache.Reset();
		}

		public MetadataTextColorProvider MetadataTextColorProvider;
		public ModuleDef CurrentModule;
		public CancellationToken CancellationToken;
		public TypeDef CurrentType;
		public DecompilerSettings Settings;
		public readonly int SettingsVersion;
		public readonly DecompilerCache Cache;
		public bool CalculateILSpans;
		public bool AsyncMethodBodyDecompilation;
		public readonly List<string> UsingNamespaces;
	}
}
