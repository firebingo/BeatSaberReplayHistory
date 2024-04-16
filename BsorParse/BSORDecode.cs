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
		public BSORReplay DecodeBSORV1(Stream stream, bool skipFrames = true)
		{
			var retval = new BSORReplay();

			if (!stream.CanRead)
				throw new Exception("File stream is not readable");

			if (stream.CanSeek)
				stream.Seek(0, SeekOrigin.Begin);

			int magic = BSORUtil.ReadInt(stream);
			byte version = BSORUtil.ReadByte(stream);

			if (version == 1 && magic == 0x442d3d69)
			{
				for (var i = 0; i < V1_STRUCT_COUNT; ++i)
				{
					var type = (V1BSORStructType)BSORUtil.ReadByte(stream);
					switch (type)
					{
						case V1BSORStructType.Info:
							retval.Info = DecodeInfoV1(stream);
							break;
						case V1BSORStructType.Frames:
							retval.Frames = DecodeFramesV1(stream, skipFrames);
							break;
						case V1BSORStructType.Notes:
							retval.Notes = DecodeNotesV1(stream);
							break;
						case V1BSORStructType.Walls:
							retval.Walls = DecodeWallsV1(stream);
							break;
						case V1BSORStructType.Heights:
							retval.Heights = DecodeHeightsV1(stream);
							break;
						case V1BSORStructType.Pauses:
							retval.Pauses = DecodePausesV1(stream);
							break;
						default:
							throw new Exception("Found invalid struct type in file.");
					}
				}
			}

			return retval;
		}

		private static BSORInfo DecodeInfoV1(Stream stream) =>
			new BSORInfo()
			{
				Version = BSORUtil.ReadString(stream),
				GameVersion = BSORUtil.ReadString(stream),
				Timestamp = BSORUtil.ReadString(stream),
				PlayerId = BSORUtil.ReadString(stream),
				PlayerName = BSORUtil.ReadString(stream),
				Platform = BSORUtil.ReadString(stream),
				TrackingSystem = BSORUtil.ReadString(stream),
				HMD = BSORUtil.ReadString(stream),
				Controller = BSORUtil.ReadString(stream),
				Hash = BSORUtil.ReadString(stream),
				SongName = BSORUtil.ReadString(stream),
				Mapper = BSORUtil.ReadString(stream),
				Difficulty = BSORUtil.ReadString(stream),
				Score = BSORUtil.ReadInt(stream),
				Mode = BSORUtil.ReadString(stream),
				Environment = BSORUtil.ReadString(stream),
				Modifiers = BSORUtil.ReadString(stream),
				JumpDistance = BSORUtil.ReadFloat(stream),
				LeftHanded = BSORUtil.ReadBool(stream),
				Height = BSORUtil.ReadFloat(stream),
				StartTime = BSORUtil.ReadFloat(stream),
				FailTime = BSORUtil.ReadFloat(stream),
				Speed = BSORUtil.ReadFloat(stream),
			};

		private static List<BSORFrame> DecodeFramesV1(Stream stream, bool skipFrames = false)
		{
			var retval = new List<BSORFrame>();
			var length = BSORUtil.ReadInt(stream);
			for (var i = 0; i < length; i++)
			{
				if (skipFrames)
				{
					SkipDecodeFrameV1(stream);
				}
				else
				{
					var frame = DecodeFrameV1(stream);
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

		private static BSORFrame DecodeFrameV1(Stream stream) =>
			new BSORFrame()
			{
				Time = BSORUtil.ReadFloat(stream),
				FPS = BSORUtil.ReadInt(stream),
				Head = BSORUtil.ReadEuler(stream),
				Left = BSORUtil.ReadEuler(stream),
				Right = BSORUtil.ReadEuler(stream)
			};

		private static List<BSORNote> DecodeNotesV1(Stream stream)
		{
			var retval = new List<BSORNote>();
			var length = BSORUtil.ReadInt(stream);
			for (var i = 0; i < length; i++)
			{
				retval.Add(DecodeNoteV1(stream));
			}
			return retval;
		}

		private static BSORNote DecodeNoteV1(Stream stream)
		{
			var retVal = new BSORNote()
			{
				NoteId = BSORUtil.ReadInt(stream),
				EventTime = BSORUtil.ReadFloat(stream),
				SpawnTime = BSORUtil.ReadFloat(stream),
				EventType = (V1BSORNoteEventType)BSORUtil.ReadInt(stream)
			};
			if (retVal.EventType == V1BSORNoteEventType.Good || retVal.EventType == V1BSORNoteEventType.Bad)
				retVal.CutInfo = DecodeNoteCutInfoV1(stream);
			return retVal;
		}

		private static BSORNoteCutInfo DecodeNoteCutInfoV1(Stream stream) =>
			new BSORNoteCutInfo()
			{
				SpeedOk = BSORUtil.ReadBool(stream),
				DirectionOk = BSORUtil.ReadBool(stream),
				SaberTypeOk = BSORUtil.ReadBool(stream),
				WasCutTooSoon = BSORUtil.ReadBool(stream),
				SaberSpeed = BSORUtil.ReadFloat(stream),
				SaberDir = BSORUtil.ReadVector3(stream),
				SaberType = BSORUtil.ReadInt(stream),
				TimeDeviation = BSORUtil.ReadFloat(stream),
				CutDirDeviation = BSORUtil.ReadFloat(stream),
				CutPoint = BSORUtil.ReadVector3(stream),
				CutNormal = BSORUtil.ReadVector3(stream),
				CutDistanceToCenter = BSORUtil.ReadFloat(stream),
				CutAngle = BSORUtil.ReadFloat(stream),
				BeforeCutRating = BSORUtil.ReadFloat(stream),
				AfterCutRating = BSORUtil.ReadFloat(stream)
			};

		private static List<BSORWall> DecodeWallsV1(Stream stream)
		{
			var retval = new List<BSORWall>();
			var length = BSORUtil.ReadInt(stream);
			for (var i = 0; i < length; i++)
			{
				retval.Add(new BSORWall()
				{
					WallId = BSORUtil.ReadInt(stream),
					Energy = BSORUtil.ReadFloat(stream),
					Time = BSORUtil.ReadFloat(stream),
					SpawnTime = BSORUtil.ReadFloat(stream)
				});
			}
			return retval;
		}

		private static List<BSORHeight> DecodeHeightsV1(Stream stream)
		{
			var retval = new List<BSORHeight>();
			var length = BSORUtil.ReadInt(stream);
			for (var i = 0; i < length; i++)
			{
				retval.Add(new BSORHeight()
				{
					Height = BSORUtil.ReadFloat(stream),
					Time = BSORUtil.ReadFloat(stream)
				});
			}
			return retval;
		}

		private static List<BSORPause> DecodePausesV1(Stream stream)
		{
			var retval = new List<BSORPause>();
			var length = BSORUtil.ReadInt(stream);
			for (var i = 0; i < length; i++)
			{
				retval.Add(new BSORPause()
				{
					Duration = BSORUtil.ReadLong(stream),
					Time = BSORUtil.ReadFloat(stream)
				});
			}
			return retval;
		}
	}
}
