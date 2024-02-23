using System;
using System.Collections.Generic;
using dnlib.DotNet;

namespace ICSharpCode.Decompiler
{
	internal sealed class NamespaceDefinition
	{
		internal readonly string FullName;
		internal readonly string Name;
		internal readonly List<NamespaceDefinition> Children = new List<NamespaceDefinition>();
		internal readonly List<TypeDef> Types = new List<TypeDef>();

		private NamespaceDefinition(string fullName, string name)
		{
			this.FullName = fullName;
			this.Name = name;
		}

		internal static NamespaceDefinition GetRootNamespace(IList<TypeDef> typeDefs)
		{
			var root = new NamespaceDefinition(string.Empty, string.Empty);
			var nsToDef = new Dictionary<string, NamespaceDefinition> { { string.Empty, root } };
			for (int i = 0; i < typeDefs.Count; i++)
			{
				TypeDef type = typeDefs[i];
				GetOrAddNamespace(nsToDef, type.Namespace).Types.Add(type);
			}
			return root;
		}

		private static NamespaceDefinition GetOrAddNamespace(Dictionary<string, NamespaceDefinition> nsToDef, string nsFullName)
		{
			if (nsToDef.TryGetValue(nsFullName, out NamespaceDefinition nsDefinition))
				return nsDefinition;
			int pos = nsFullName.LastIndexOf('.');
			NamespaceDefinition parent;
			string name;
			if (pos < 0) {
				parent = nsToDef[string.Empty]; // root
				name = nsFullName;
			} else {
				parent = GetOrAddNamespace(nsToDef, nsFullName.Substring(0, pos));
				name = nsFullName.Substring(pos + 1);
			}
			nsDefinition = new NamespaceDefinition(nsFullName, name);
			parent.Children.Add(nsDefinition);
			nsToDef.Add(nsFullName, nsDefinition);
			return nsDefinition;
		}
	}
}
