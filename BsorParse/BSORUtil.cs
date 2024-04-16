using System.Buffers;
using System.Buffers.Binary;

namespace BeatSaberReplayHistory
{
	internal static class BSORUtil
	{
		internal static byte ReadByte(Stream stream) => (byte)stream.ReadByte();

		internal static async Task<int> ReadInt(Stream stream)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(4);
			try
			{
				await stream.ReadAsync(buffer, 0, 4);
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

		internal static async Task<long> ReadLong(Stream stream)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(8);
			try
			{
				await stream.ReadAsync(buffer, 0, 8);
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

		internal static async Task<float> ReadFloat(Stream stream)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(4);
			try
			{
				await stream.ReadAsync(buffer, 0, 4);
				if (BitConverter.IsLittleEndian)
					return BitConverter.ToSingle(buffer);
				else
					return BitConverter.ToSingle(buffer.Reverse().ToArray());
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		internal static bool ReadBool(Stream stream) => ReadByte(stream) != 0;

		internal static async Task<string> ReadString(Stream stream)
		{
			var length = await ReadInt(stream);
			//BeatSaber-Web-Replays has a check for this and im not sure how trying to slice with a negative number would work?
			//TODO Fix if I ever get a real example of this happening.
			if (length < 0)
				throw new Exception($"Invalid string length ({length})");
			var bytes = new byte[length];
			await stream.ReadAsync(bytes);
			using var rdrStream = new MemoryStream(bytes);
			using var rdr = new StreamReader(rdrStream, System.Text.Encoding.UTF8);
			return await rdr.ReadToEndAsync();
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

		internal static async Task<BSORVector3> ReadVector3(Stream stream) =>
			new BSORVector3()
			{
				X = await ReadFloat(stream),
				Y = await ReadFloat(stream),
				Z = await ReadFloat(stream)
			};


		internal static async Task<BSORQuaternion> ReadQuaternion(Stream stream) =>
			new BSORQuaternion()
			{
				X = await ReadFloat(stream),
				Y = await ReadFloat(stream),
				Z = await ReadFloat(stream),
				W = await ReadFloat(stream)
			};

		internal static async Task<BSOREuler> ReadEuler(Stream stream) =>
			new BSOREuler()
			{
				Position = await ReadVector3(stream),
				Rotation = await ReadQuaternion(stream)
			};
	}
}
