// Copyright (c) 2018 Siegfried Pammer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;

using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp.TypeSystem;
using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler
{
	internal class DecompileRun
	{
		public HashSet<string> DefinedSymbols { get; } = new HashSet<string>();
		public HashSet<string> Namespaces { get; } = new HashSet<string>();
		public CancellationToken CancellationToken { get; set; }
		public DecompilerSettings Settings { get; }
		public IDocumentationProvider DocumentationProvider { get; set; }
		public Dictionary<ITypeDefinition, RecordDecompiler> RecordDecompilers { get; } = new Dictionary<ITypeDefinition, RecordDecompiler>();
		public Dictionary<ITypeDefinition, bool> TypeHierarchyIsKnown { get; } = new();

		public UsingScope UsingScope => CreateUsingScope(Namespaces);

		public DecompilerContext Context { get; set; }

		public DecompileRun(DecompilerSettings settings)
		{
			this.Settings = settings ?? throw new ArgumentNullException(nameof(settings));
		}

		UsingScope CreateUsingScope(HashSet<string> requiredNamespacesSuperset)
		{
			var usingScope = new UsingScope();
			string[] arrayForThreadSafety = new string[requiredNamespacesSuperset.Count];
			requiredNamespacesSuperset.CopyTo(arrayForThreadSafety);
			foreach (var ns in arrayForThreadSafety)
			{
				string[] parts = ns.Split('.');
				AstType nsType = new SimpleType(parts[0]);
				for (int i = 1; i < parts.Length; i++)
				{
					nsType = new MemberType { Target = nsType, MemberName = parts[i] };
				}

				if (nsType.ToTypeReference(CSharp.Resolver.NameLookupMode.TypeInUsingDeclaration) is TypeOrNamespaceReference reference)
					usingScope.Usings.Add(reference);
			}
			return usingScope;
		}

		public void Reset()
		{
			DefinedSymbols.Clear();
			Namespaces.Clear();
			RecordDecompilers.Clear();
			TypeHierarchyIsKnown.Clear();
		}
	}

	enum EnumValueDisplayMode
	{
		None,
		All,
		AllHex,
		FirstOnly
	}
}
