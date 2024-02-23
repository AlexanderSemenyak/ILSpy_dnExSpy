using System;
using System.Collections.Generic;

namespace ICSharpCode.Decompiler.Util
{
	internal sealed class ThreadSafeObjectPool<T> where T : class
	{
		private readonly List<T> freeObjs;
		private readonly List<T> allObjs;
		private readonly Func<T> create;
		private readonly Action<T> resetObject;
		private readonly object lockObj = new object();

		public ThreadSafeObjectPool(Func<T> create, Action<T> resetObject)
		{
			this.create = create;
			this.resetObject = resetObject;
			freeObjs = new List<T>();
			allObjs = new List<T>();
		}

		public T Allocate()
		{
			lock (lockObj)
			{
				if (freeObjs.Count > 0)
				{
					int i = freeObjs.Count - 1;
					var o = freeObjs[i];
					freeObjs.RemoveAt(i);
					return o;
				}

				var newObj = create();
				allObjs.Add(newObj);
				return newObj;
			}
		}

		public void Free(T obj)
		{
			resetObject?.Invoke(obj);
			lock (lockObj)
				freeObjs.Add(obj);
		}

		public void ReuseAllObjects()
		{
			lock (lockObj)
			{
				freeObjs.Clear();
				foreach (T obj in allObjs) {
					resetObject?.Invoke(obj);
					freeObjs.Add(obj);
				}
			}
		}
	}
}
