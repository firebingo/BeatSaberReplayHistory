using BeatSaberReplayHistory;

namespace BsorParse.Util
{
	internal class BSORNotePool
	{
		private static readonly Queue<BSORNote> _pool;
		private static readonly object _lock = new object();

		static BSORNotePool()
		{
			_pool = new Queue<BSORNote>(5000);
			for (var i = 0; i < 5000; ++i)
			{
				_pool.Enqueue(new BSORNote());
			}
		}

		internal static BSORNote Rent()
		{
			lock (_lock)
			{
				if (_pool.Count != 0)
				{
					return _pool.Dequeue();
				}
				else
				{
					return new BSORNote();
				}
			}
		}

		internal static void Return(BSORNote note)
		{
			lock (_lock)
			{
				_pool.Enqueue(note);
			}
		}
	}
}
