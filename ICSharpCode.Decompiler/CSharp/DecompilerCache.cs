using System.Collections.Generic;

using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.IL.Transforms;
using ICSharpCode.Decompiler.Util;

namespace ICSharpCode.Decompiler.CSharp
{
	/// <summary>
	/// Shared by all code in the current decompiler thread. It must only be accessed from the
	/// owning thread.
	/// </summary>
	public sealed class DecompilerCache {
		private readonly ObjectPool<IAstTransform[]> csharpPipelinePool;
		private readonly ThreadSafeObjectPool<IList<IILTransform>> ilPipelinePool;

		public DecompilerCache(DecompilerContext ctx) {
			this.csharpPipelinePool = new ObjectPool<IAstTransform[]>(() => CSharpTransformationPipeline.CreatePipeline(ctx), null);
			this.ilPipelinePool = new ThreadSafeObjectPool<IList<IILTransform>>(CSharpDecompiler.GetILTransforms, null);
		}

		public void Reset() {
			csharpPipelinePool.ReuseAllObjects();
			ilPipelinePool.ReuseAllObjects();
		}

		public IAstTransform[] GetCSharpPipeline() {
			return csharpPipelinePool.Allocate();
		}

		public void Return(IAstTransform[] pipeline) {
			csharpPipelinePool.Free(pipeline);
		}

		public IList<IILTransform> GetILPipeline() {
			return ilPipelinePool.Allocate();
		}

		public void Return(IList<IILTransform> pipeline) {
			ilPipelinePool.Free(pipeline);
		}
	}
}
