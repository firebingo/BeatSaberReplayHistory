namespace BeatSaberReplayHistory
{
	public enum V1BSORStructType
	{
		Info = 0,
		Frames = 1,
		Notes = 2,
		Walls = 3,
		Heights = 4,
		Pauses = 5,
		SaberOffsets = 6,
		CustomData = 7
	}

	public enum V1BSORNoteEventType
	{
		Good = 0,
		Bad = 1,
		Miss = 2,
		Bomb = 3
	}

	public class BSORReplay
	{
		public BSORInfo Info { get; set; } = new BSORInfo();
		public List<BSORFrame> Frames { get; set; } = [];
		public List<BSORNote> Notes { get; set; } = [];
		public List<BSORWall> Walls { get; set; } = [];
		public List<BSORHeight> Heights { get; set; } = [];
		public List<BSORPause> Pauses { get; set; } = [];
		public SaberOffsets SaberOffsets = new SaberOffsets();
		public Dictionary<string, byte[]> CustomData = [];
	}

	public struct BSOREuler
	{
		public BSORVector3 Position;
		public BSORQuaternion Rotation;
	}

	public struct BSORVector3
	{
		public float X;
		public float Y;
		public float Z;
	}

	public struct BSORQuaternion
	{
		public float X;
		public float Y;
		public float Z;
		public float W;
	}

	public class BSORInfo
	{
		public string Version { get; set; } = string.Empty;
		public string GameVersion { get; set; } = string.Empty;
		public string Timestamp { get; set; } = string.Empty;
		public string PlayerId { get; set; } = string.Empty;
		public string PlayerName { get; set; } = string.Empty;
		public string Platform { get; set; } = string.Empty;
		public string TrackingSystem { get; set; } = string.Empty;
		public string HMD { get; set; } = string.Empty;
		public string Controller { get; set; } = string.Empty;
		public string Hash { get; set; } = string.Empty;
		public string SongName { get; set; } = string.Empty;
		public string Mapper { get; set; } = string.Empty;
		public string Difficulty { get; set; } = string.Empty;
		public int Score { get; set; } = 0;
		public string Mode { get; set; } = string.Empty;
		public string Environment { get; set; } = string.Empty;
		public string Modifiers { get; set; } = string.Empty;
		public float JumpDistance { get; set; } = 0.0f;
		public bool LeftHanded { get; set; } = false;
		public float Height { get; set; } = 0.0f;
		public float StartTime { get; set; } = 0.0f;
		public float FailTime { get; set; } = 0.0f;
		public float Speed { get; set; } = 0.0f;
		public DateTimeOffset? Time
		{
			get => !long.TryParse(Timestamp, out var l) ? null : DateTimeOffset.FromUnixTimeSeconds(l).ToLocalTime();
		}
	}

	public class BSORFrame
	{
		public float Time { get; set; } = 0.0f;
		public int FPS { get; set; } = 0;
		public BSOREuler Head { get; set; } = new BSOREuler();
		public BSOREuler Left { get; set; } = new BSOREuler();
		public BSOREuler Right { get; set; } = new BSOREuler();
	}

	public class BSORNote
	{
		public int NoteId { get; set; } = 0;
		public float EventTime { get; set; } = 0.0f;
		public float SpawnTime { get; set; } = 0.0f;
		public V1BSORNoteEventType EventType { get; set; } = V1BSORNoteEventType.Good;
		public BSORNoteCutInfo? CutInfo { get; set; } = null;
	}

	public class BSORNoteCutInfo
	{
		public bool SpeedOk { get; set; } = false;
		public bool DirectionOk { get; set; } = false;
		public bool SaberTypeOk { get; set; } = false;
		public bool WasCutTooSoon { get; set; } = false;
		public float SaberSpeed { get; set; } = 0.0f;
		public BSORVector3 SaberDir { get; set; } = new BSORVector3();
		public int SaberType { get; set; } = 0;
		public float TimeDeviation { get; set; } = 0.0f;
		public float CutDirDeviation { get; set; } = 0.0f;
		public BSORVector3 CutPoint { get; set; } = new BSORVector3();
		public BSORVector3 CutNormal { get; set; } = new BSORVector3();
		public float CutDistanceToCenter { get; set; } = 0.0f;
		public float CutAngle { get; set; } = 0.0f;
		public float BeforeCutRating { get; set; } = 0.0f;
		public float AfterCutRating { get; set; } = 0.0f;
	}

	public class BSORWall
	{
		public int WallId { get; set; } = 0;
		public float Energy { get; set; } = 0.0f;
		public float Time { get; set; } = 0.0f;
		public float SpawnTime { get; set; } = 0.0f;
	}

	public class BSORHeight
	{
		public float Height { get; set; }
		public float Time { get; set; }
	}

	public class BSORPause
	{
		public long Duration { get; set; }
		public float Time { get; set; }
	}

	public class SaberOffsets
	{
		public BSORVector3 LeftSaberLocalPosition { get; set; }
		public BSORQuaternion LeftSaberLocalRotation { get; set; }
		public BSORVector3 RightSaberLocalPosition { get; set; }
		public BSORQuaternion RightSaberLocalRotation { get; set; }
	}
}
