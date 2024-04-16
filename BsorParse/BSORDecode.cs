namespace BeatSaberReplayHistory
{
	public class BSORDecode
	{
		private readonly int V1_STRUCT_COUNT = 6;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="skipFrames">If you don't need frame data this can save a lot of decoding time</param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public async Task<BSORReplay> DecodeBSORV1(Stream stream, bool skipFrames = true)
		{
			var retval = new BSORReplay();

			if (!stream.CanRead)
				throw new Exception("File stream is not readable");

			if (stream.CanSeek)
				stream.Seek(0, SeekOrigin.Begin);

			int magic = await BSORUtil.ReadInt(stream);
			byte version = BSORUtil.ReadByte(stream);

			if (version == 1 && magic == 0x442d3d69)
			{
				for (var i = 0; i < V1_STRUCT_COUNT; ++i)
				{
					var type = (V1BSORStructType)BSORUtil.ReadByte(stream);
					switch (type)
					{
						case V1BSORStructType.Info:
							retval.Info = await DecodeInfoV1(stream);
							break;
						case V1BSORStructType.Frames:
							retval.Frames = await DecodeFramesV1(stream, skipFrames);
							break;
						case V1BSORStructType.Notes:
							retval.Notes = await DecodeNotesV1(stream);
							break;
						case V1BSORStructType.Walls:
							retval.Walls = await DecodeWallsV1(stream);
							break;
						case V1BSORStructType.Heights:
							retval.Heights = await DecodeHeightsV1(stream);
							break;
						case V1BSORStructType.Pauses:
							retval.Pauses = await DecodePausesV1(stream);
							break;
						default:
							throw new Exception("Found invalid struct type in file.");
					}
				}
			}

			return retval;
		}

		private static async Task<BSORInfo> DecodeInfoV1(Stream stream) =>
			new BSORInfo()
			{
				Version = await BSORUtil.ReadString(stream),
				GameVersion = await BSORUtil.ReadString(stream),
				Timestamp = await BSORUtil.ReadString(stream),
				PlayerId = await BSORUtil.ReadString(stream),
				PlayerName = await BSORUtil.ReadString(stream),
				Platform = await BSORUtil.ReadString(stream),
				TrackingSystem = await BSORUtil.ReadString(stream),
				HMD = await BSORUtil.ReadString(stream),
				Controller = await BSORUtil.ReadString(stream),
				Hash = await BSORUtil.ReadString(stream),
				SongName = await BSORUtil.ReadString(stream),
				Mapper = await BSORUtil.ReadString(stream),
				Difficulty = await BSORUtil.ReadString(stream),
				Score = await BSORUtil.ReadInt(stream),
				Mode = await BSORUtil.ReadString(stream),
				Environment = await BSORUtil.ReadString(stream),
				Modifiers = await BSORUtil.ReadString(stream),
				JumpDistance = await BSORUtil.ReadFloat(stream),
				LeftHanded = BSORUtil.ReadBool(stream),
				Height = await BSORUtil.ReadFloat(stream),
				StartTime = await BSORUtil.ReadFloat(stream),
				FailTime = await BSORUtil.ReadFloat(stream),
				Speed = await BSORUtil.ReadFloat(stream),
			};

		private static async Task<List<BSORFrame>> DecodeFramesV1(Stream stream, bool skipFrames = false)
		{
			var retval = new List<BSORFrame>();
			var length = await BSORUtil.ReadInt(stream);
			for (var i = 0; i < length; i++)
			{
				if (skipFrames)
				{
					SkipDecodeFrameV1(stream);
				}
				else
				{
					var frame = await DecodeFrameV1(stream);
					if (frame.Time >= 0.0001f && (retval.Count == 0 || frame.Time != retval[^1].Time))
						retval.Add(frame);
				}
			}

			return retval;
		}

		private static void SkipDecodeFrameV1(Stream stream)
		{
			stream.Seek(92, SeekOrigin.Current);
		}

		private static async Task<BSORFrame> DecodeFrameV1(Stream stream) =>
			new BSORFrame()
			{
				Time = await BSORUtil.ReadFloat(stream),
				FPS = await BSORUtil.ReadInt(stream),
				Head = await BSORUtil.ReadEuler(stream),
				Left = await BSORUtil.ReadEuler(stream),
				Right = await BSORUtil.ReadEuler(stream)
			};

		private static async Task<List<BSORNote>> DecodeNotesV1(Stream stream)
		{
			var retval = new List<BSORNote>();
			var length = await BSORUtil.ReadInt(stream);
			for (var i = 0; i < length; i++)
			{
				retval.Add(await DecodeNoteV1(stream));
			}
			return retval;
		}

		private static async Task<BSORNote> DecodeNoteV1(Stream stream)
		{
			var retVal = new BSORNote()
			{
				NoteId = await BSORUtil.ReadInt(stream),
				EventTime = await BSORUtil.ReadFloat(stream),
				SpawnTime = await BSORUtil.ReadFloat(stream),
				EventType = (V1BSORNoteEventType)await BSORUtil.ReadInt(stream)
			};
			if (retVal.EventType == V1BSORNoteEventType.Good || retVal.EventType == V1BSORNoteEventType.Bad)
				retVal.CutInfo = await DecodeNoteCutInfoV1(stream);
			return retVal;
		}

		private static async Task<BSORNoteCutInfo> DecodeNoteCutInfoV1(Stream stream) =>
			new BSORNoteCutInfo()
			{
				SpeedOk = BSORUtil.ReadBool(stream),
				DirectionOk = BSORUtil.ReadBool(stream),
				SaberTypeOk = BSORUtil.ReadBool(stream),
				WasCutTooSoon = BSORUtil.ReadBool(stream),
				SaberSpeed = await BSORUtil.ReadFloat(stream),
				SaberDir = await BSORUtil.ReadVector3(stream),
				SaberType = await BSORUtil.ReadInt(stream),
				TimeDeviation = await BSORUtil.ReadFloat(stream),
				CutDirDeviation = await BSORUtil.ReadFloat(stream),
				CutPoint = await BSORUtil.ReadVector3(stream),
				CutNormal = await BSORUtil.ReadVector3(stream),
				CutDistanceToCenter = await BSORUtil.ReadFloat(stream),
				CutAngle = await BSORUtil.ReadFloat(stream),
				BeforeCutRating = await BSORUtil.ReadFloat(stream),
				AfterCutRating = await BSORUtil.ReadFloat(stream)
			};

		private static async Task<List<BSORWall>> DecodeWallsV1(Stream stream)
		{
			var retval = new List<BSORWall>();
			var length = await BSORUtil.ReadInt(stream);
			for (var i = 0; i < length; i++)
			{
				retval.Add(new BSORWall()
				{
					WallId = await BSORUtil.ReadInt(stream),
					Energy = await BSORUtil.ReadFloat(stream),
					Time = await BSORUtil.ReadFloat(stream),
					SpawnTime = await BSORUtil.ReadFloat(stream)
				});
			}
			return retval;
		}

		private static async Task<List<BSORHeight>> DecodeHeightsV1(Stream stream)
		{
			var retval = new List<BSORHeight>();
			var length = await BSORUtil.ReadInt(stream);
			for (var i = 0; i < length; i++)
			{
				retval.Add(new BSORHeight()
				{
					Height = await BSORUtil.ReadFloat(stream),
					Time = await BSORUtil.ReadFloat(stream)
				});
			}
			return retval;
		}

		private static async Task<List<BSORPause>> DecodePausesV1(Stream stream)
		{
			var retval = new List<BSORPause>();
			var length = await BSORUtil.ReadInt(stream);
			for (var i = 0; i < length; i++)
			{
				retval.Add(new BSORPause()
				{
					Duration = await BSORUtil.ReadLong(stream),
					Time = await BSORUtil.ReadFloat(stream)
				});
			}
			return retval;
		}
	}
}
