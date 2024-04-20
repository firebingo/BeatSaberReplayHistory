using System.Buffers;
using System.Buffers.Binary;
using System.Text;

namespace BeatSaberReplayHistory
{
	internal static class BSORUtil
	{
		internal static byte ReadByte(Stream stream)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(1);
			try
			{
				stream.ReadAsync(buffer, 0, 1);
				return buffer[0];
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		internal static int ReadInt(Stream stream)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(4);
			try
			{
				stream.ReadAsync(buffer, 0, 4);
				var val = BitConverter.ToInt32(buffer, 0);
				if (BitConverter.IsLittleEndian)
					return val;
				else
					return BinaryPrimitives.ReverseEndianness(val);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		internal static long ReadLong(Stream stream)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(8);
			try
			{
				stream.ReadAsync(buffer, 0, 8);
				var val = BitConverter.ToInt64(buffer, 0);
				if (BitConverter.IsLittleEndian)
					return val;
				else
					return BinaryPrimitives.ReverseEndianness(val);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		internal static float ReadFloat(Stream stream)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(4);
			try
			{
				stream.ReadAsync(buffer, 0, 4);
				if (BitConverter.IsLittleEndian)
					return BitConverter.ToSingle(buffer);
				else
				{
					(buffer[3], buffer[0], buffer[1], buffer[2]) = (buffer[0], buffer[3], buffer[2], buffer[1]);
					return BitConverter.ToSingle(buffer);
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		internal static bool ReadBool(Stream stream) => ReadByte(stream) != 0;

		internal static string ReadString(Stream stream)
		{
			var length = ReadInt(stream);
			stream.Seek(-4, SeekOrigin.Current);
			//Some weird Beatleader unicode fix thing.
			if (length > 300 || length < 0)
			{
				stream.Seek(1, SeekOrigin.Current);
				return ReadString(stream);
			}
			var bytes = ArrayPool<byte>.Shared.Rent(length);
			stream.Seek(4, SeekOrigin.Current);
			stream.Read(bytes, 0, length);
			return Encoding.UTF8.GetString(bytes, 0, length);
		}

		internal static string ReadName(Stream stream)
		{
			int length = ReadInt(stream);
			int lengthOffset = 0;
			if (length > 0)
			{
				//BeatLeader is doing this to fix a unicode issue? This is some wacky shit.
				//Save start position of stream
				var pos = stream.Position;
				//Seek to end of string length
				stream.Seek(length, SeekOrigin.Current);
				var i = ReadInt(stream);
				stream.Seek(-4, SeekOrigin.Current);
				//Check if int read after end of string is 6, 5, or 8??
				// I have to assume these are expected values that would appear after this string
				// and if its not its some weird unicode thing that is longer than expected?
				while (i != 6
					&& i != 5
					&& i != 8)
				{
					lengthOffset++;
					stream.Seek(1, SeekOrigin.Current);
					i = ReadInt(stream);
					stream.Seek(-4, SeekOrigin.Current);
					if (lengthOffset > 50)
						break;
				}
				//return stream to start of string to read it.
				stream.Seek(pos, SeekOrigin.Begin);
			}
			var bytes = ArrayPool<byte>.Shared.Rent(length + lengthOffset);
			stream.Read(bytes, 0, length + lengthOffset);
			var s = Encoding.UTF8.GetString(bytes, 0, length + lengthOffset);
			return s;
		}

		//BeatSaber-Web-Replays uses this as some sort of fix for names with emojis?
		//im not sure the c# StreamReader has the same problem.
		//TODO Fix if I ever get a real example of this happening.
		//internal static async Task<string> ReadName(Stream stream)
		//{
		//const length = dataView.getInt32(dataView.pointer, true);
		//var enc = new TextDecoder('utf-8');
		//let lengthOffset = 0;
		//if (length > 0) {
		//	while (
		//		dataView.getInt32(length + dataView.pointer + 4 + lengthOffset, true) != 6 &&
		//		dataView.getInt32(length + dataView.pointer + 4 + lengthOffset, true) != 5 &&
		//		dataView.getInt32(length + dataView.pointer + 4 + lengthOffset, true) != 8
		//	) {
		//		lengthOffset++;
		//	}
		//}
		//
		//const string = enc.decode(new Int8Array(dataView.buffer.slice(dataView.pointer + 4, length + dataView.pointer + 4 + lengthOffset)));
		//dataView.pointer += length + 4 + lengthOffset;
		//return string;
		//}

		internal static BSORVector3 ReadVector3(Stream stream) =>
			new BSORVector3()
			{
				X = ReadFloat(stream),
				Y = ReadFloat(stream),
				Z = ReadFloat(stream)
			};


		internal static BSORQuaternion ReadQuaternion(Stream stream) =>
			new BSORQuaternion()
			{
				X = ReadFloat(stream),
				Y = ReadFloat(stream),
				Z = ReadFloat(stream),
				W = ReadFloat(stream)
			};

		internal static BSOREuler ReadEuler(Stream stream) =>
			new BSOREuler()
			{
				Position = ReadVector3(stream),
				Rotation = ReadQuaternion(stream)
			};

		internal static byte[] ReadByteArray(Stream stream)
		{
			var length = ReadInt(stream);
			var result = new byte[length];
			stream.Read(result, 0, length);
			return result;
		}
	}
}
