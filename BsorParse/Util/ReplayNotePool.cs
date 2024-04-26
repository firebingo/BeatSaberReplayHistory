using BsorParse.Model;

namespace BsorParse.Util
{
	internal static class ReplayNotePool
	{
		private static readonly Queue<ReplayNote> _pool;
		private static readonly object _lock = new object();

		static ReplayNotePool()
		{
			_pool = new Queue<ReplayNote>(250);
			for (var i = 0; i < 100; ++i)
			{
				_pool.Enqueue(new ReplayNote());
			}
		}

		internal static ReplayNote Rent()
		{
			lock (_lock)
			{
				if (_pool.Count != 0)
				{
					return _pool.Dequeue();
				}
				else
				{
					return new ReplayNote();
				}
			}
		}

		internal static void Return(ReplayNote note)
		{
			lock (_lock)
			{
				note.ClearNote();
				_pool.Enqueue(note);
			}
		}
	}
}
