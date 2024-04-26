using BeatSaberReplayHistory;

namespace BsorParse.Util
{
	internal class BSORNoteCutPool
	{
		private static readonly Queue<BSORNoteCutInfo> _pool;
		private static readonly object _lock = new object();

		static BSORNoteCutPool()
		{
			_pool = new Queue<BSORNoteCutInfo>(5000);
			for (var i = 0; i < 5000; ++i)
			{
				_pool.Enqueue(new BSORNoteCutInfo());
			}
		}

		internal static BSORNoteCutInfo Rent()
		{
			lock (_lock)
			{
				if (_pool.Count != 0)
				{
					return _pool.Dequeue();
				}
				else
				{
					return new BSORNoteCutInfo();
				}
			}
		}

		internal static void Return(BSORNoteCutInfo note)
		{
			lock (_lock)
			{
				_pool.Enqueue(note);
			}
		}
	}
}
