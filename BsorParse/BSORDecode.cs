using BsorParse.Util;
using System.Collections.Generic;

namespace BeatSaberReplayHistory
{
	public static class BSORDecode
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="skipFrames">If you don't need frame data this can save a lot of decoding time</param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public static BSORReplay DecodeBSORV1(Stream stream, bool skipFrames = true)
		{
			var retval = new BSORReplay();

			if (!stream.CanRead)
				throw new Exception("Stream is not readable");

			if (!stream.CanSeek)
				throw new Exception("Stream is not seekable");
			stream.Seek(0, SeekOrigin.Begin);

			int magic = BSORUtil.ReadInt(stream);
			byte version = BSORUtil.ReadByte(stream);
			
			//Ive seen files that have some extra data at the end?
			// So im checking these otherwise it can attempt to read a bad section and do bad things.
			var readInfo = false;
			var readFrames = false;
			var readNotes = false;
			var readWalls = false;
			var readHeights = false;
			var readPauses = false;
			var readSaberOffsets = false;
			var readCustomData = false;
			if (version == 1 && magic == 0x442d3d69)
			{
				for (var i = 0; i < (int)V1BSORStructType.CustomData && stream.Position < stream.Length; ++i)
				{
					var type = (V1BSORStructType)BSORUtil.ReadByte(stream);
					switch (type)
					{
						case V1BSORStructType.Info:
							if (!readInfo)
							{
								retval.Info = DecodeInfoV1(stream);
								readInfo = true;
							}
							break;
						case V1BSORStructType.Frames:
							if (!readFrames)
							{
								retval.Frames = DecodeFramesV1(stream, skipFrames);
								readFrames = true;
							}
							break;
						case V1BSORStructType.Notes:
							if (!readNotes)
							{
								retval.Notes = DecodeNotesV1(stream);
								readNotes = true;
							}
							break;
						case V1BSORStructType.Walls:
							if (!readWalls)
							{
								retval.Walls = DecodeWallsV1(stream);
								readWalls = true;
							}
							break;
						case V1BSORStructType.Heights:
							if (!readHeights)
							{
								retval.Heights = DecodeHeightsV1(stream);
								readHeights = true;
							}
							break;
						case V1BSORStructType.Pauses:
							if (!readPauses)
							{
								retval.Pauses = DecodePausesV1(stream);
								readPauses = true;
							}
							break;
						case V1BSORStructType.SaberOffsets:
							if (!readSaberOffsets)
							{
								retval.SaberOffsets = DecodeSaberOffsetsV1(stream);
								readSaberOffsets = true;
							}
							break;
						case V1BSORStructType.CustomData:
							if (!readCustomData)
							{
								retval.CustomData = DecodeCustomData(stream);
								readCustomData = true;
							}
							break;
						default:
							//If we have read all the main sections and found invalid data just let it continue
							if(readInfo && readFrames && readNotes && readWalls && readHeights && readPauses)
							{
								break;
							}
							throw new Exception("Found invalid struct type in file.");
					}
				}
			}

			return retval;
		}

		private static BSORInfo DecodeInfoV1(Stream stream)
		{
			var retval = new BSORInfo();
			
			retval.Version = BSORUtil.ReadString(stream);
			retval.GameVersion = BSORUtil.ReadString(stream);
			retval.Timestamp = BSORUtil.ReadString(stream);
			retval.PlayerId = BSORUtil.ReadString(stream);
			retval.PlayerName = BSORUtil.ReadName(stream);
			retval.Platform = BSORUtil.ReadString(stream);
			retval.TrackingSystem = BSORUtil.ReadString(stream);
			retval.HMD = BSORUtil.ReadString(stream);
			retval.Controller = BSORUtil.ReadString(stream);
			retval.Hash = BSORUtil.ReadString(stream);
			retval.SongName = BSORUtil.ReadString(stream);
			retval.Mapper = BSORUtil.ReadString(stream);
			retval.Difficulty = BSORUtil.ReadString(stream);
			retval.Score = BSORUtil.ReadInt(stream);
			retval.Mode = BSORUtil.ReadString(stream);
			retval.Environment = BSORUtil.ReadString(stream);
			retval.Modifiers = BSORUtil.ReadString(stream);
			retval.JumpDistance = BSORUtil.ReadFloat(stream);
			retval.LeftHanded = BSORUtil.ReadBool(stream);
			retval.Height = BSORUtil.ReadFloat(stream);
			retval.StartTime = BSORUtil.ReadFloat(stream);
			retval.FailTime = BSORUtil.ReadFloat(stream);
			retval.Speed = BSORUtil.ReadFloat(stream);
			return retval;
		}

		private static List<BSORFrame> DecodeFramesV1(Stream stream, bool skipFrames = false)
		{
			var length = BSORUtil.ReadInt(stream);
			List<BSORFrame> retval;
			if (skipFrames)
				retval = [];
			else
				retval = new List<BSORFrame>(length);
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
			var length = BSORUtil.ReadInt(stream);
			var retval = new List<BSORNote>(length);
			for (var i = 0; i < length; i++)
			{
				retval.Add(DecodeNoteV1(stream));
			}
			return retval;
		}

		private static BSORNote DecodeNoteV1(Stream stream)
		{
			var retval = BSORNotePool.Rent();
			retval.NoteId = BSORUtil.ReadInt(stream);
			retval.EventTime = BSORUtil.ReadFloat(stream);
			retval.SpawnTime = BSORUtil.ReadFloat(stream);
			retval.EventType = (V1BSORNoteEventType)BSORUtil.ReadInt(stream);
			
			if (retval.EventType == V1BSORNoteEventType.Good || retval.EventType == V1BSORNoteEventType.Bad)
				retval.CutInfo = DecodeNoteCutInfoV1(stream);
			return retval;
		}

		private static BSORNoteCutInfo DecodeNoteCutInfoV1(Stream stream)
		{
			var retval = BSORNoteCutPool.Rent();
			retval.SpeedOk = BSORUtil.ReadBool(stream);
			retval.DirectionOk = BSORUtil.ReadBool(stream);
			retval.SaberTypeOk = BSORUtil.ReadBool(stream);
			retval.WasCutTooSoon = BSORUtil.ReadBool(stream);
			retval.SaberSpeed = BSORUtil.ReadFloat(stream);
			retval.SaberDir = BSORUtil.ReadVector3(stream);
			retval.SaberType = BSORUtil.ReadInt(stream);
			retval.TimeDeviation = BSORUtil.ReadFloat(stream);
			retval.CutDirDeviation = BSORUtil.ReadFloat(stream);
			retval.CutPoint = BSORUtil.ReadVector3(stream);
			retval.CutNormal = BSORUtil.ReadVector3(stream);
			retval.CutDistanceToCenter = BSORUtil.ReadFloat(stream);
			retval.CutAngle = BSORUtil.ReadFloat(stream);
			retval.BeforeCutRating = BSORUtil.ReadFloat(stream);
			retval.AfterCutRating = BSORUtil.ReadFloat(stream);
			return retval;
		}

		private static List<BSORWall> DecodeWallsV1(Stream stream)
		{
			var length = BSORUtil.ReadInt(stream);
			var retval = new List<BSORWall>(length);
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
			var length = BSORUtil.ReadInt(stream);
			var retval = new List<BSORHeight>(length);
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
			var length = BSORUtil.ReadInt(stream);
			var retval = new List<BSORPause>(length);
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

		private static SaberOffsets DecodeSaberOffsetsV1(Stream stream) =>
			new SaberOffsets
			{
				LeftSaberLocalPosition = BSORUtil.ReadVector3(stream),
				LeftSaberLocalRotation = BSORUtil.ReadQuaternion(stream),
				RightSaberLocalPosition = BSORUtil.ReadVector3(stream),
				RightSaberLocalRotation = BSORUtil.ReadQuaternion(stream)
			};

		private static Dictionary<string, byte[]> DecodeCustomData(Stream stream)
		{
			var length = BSORUtil.ReadInt(stream);
			var retval = new Dictionary<string, byte[]>(length);
			for (var i = 0; i < length; i++)
			{
				var key = BSORUtil.ReadString(stream);
				var value = BSORUtil.ReadByteArray(stream);
				retval[key] = value;
			}
			return retval;
		}
	}
}
