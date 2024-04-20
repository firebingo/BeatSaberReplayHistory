using BsorParse.Model;

namespace BsorParse.Util
{
	internal class ScoreEventPool
	{
		private static readonly Queue<ScoreEvent> _pool;
		private static readonly object _lock = new object();

		static ScoreEventPool()
		{
			_pool = new Queue<ScoreEvent>(5000);
			for (var i = 0; i < 5000; ++i)
			{
				_pool.Enqueue(new ScoreEvent());
			}
		}

		internal static ScoreEvent Rent()
		{
			lock (_lock)
			{
				if (_pool.Count != 0)
				{
					return _pool.Dequeue();
				}
				else
				{
					return new ScoreEvent();
				}
			}
		}

		internal static void Return(ScoreEvent note)
		{
			lock (_lock)
			{
				note.ClearEvent();
				_pool.Enqueue(note);
			}
		}
	}
}
