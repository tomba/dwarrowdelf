using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace Dwarrowdelf.NetSerializer
{
	static class Primitives
	{
		public static MethodInfo GetWritePrimitive(Type type)
		{
			if (type.IsEnum)
				type = type.GetEnumUnderlyingType();

			var mi = typeof(Primitives).GetMethod("WritePrimitive", BindingFlags.Static | BindingFlags.Public, null,
				new Type[] { typeof(GameNetStream), type }, null);

			return mi;
		}

		public static MethodInfo GetReadPrimitive(Type type)
		{
			if (!type.IsByRef)
				throw new Exception();

			if (type.GetElementType().IsEnum)
				type = type.GetElementType().GetEnumUnderlyingType().MakeByRefType();

			MethodInfo mi = typeof(Primitives).GetMethod("ReadPrimitive", BindingFlags.Static | BindingFlags.Public, null,
				new Type[] { typeof(GameNetStream), type }, null);

			return mi;
		}

		static uint EncodeZigZag32(int n)
		{
			return (uint)((n << 1) ^ (n >> 31));
		}

		static ulong EncodeZigZag64(long n)
		{
			return (ulong)((n << 1) ^ (n >> 63));
		}

		static int DecodeZigZag32(uint n)
		{
			return (int)(n >> 1) ^ -(int)(n & 1);
		}

		static long DecodeZigZag64(ulong n)
		{
			return (long)(n >> 1) ^ -(long)(n & 1);
		}

		static uint ReadVarint32(GameNetStream stream)
		{
			int result = 0;
			int offset = 0;

			for (; offset < 32; offset += 7)
			{
				int b = stream.ReadByte();
				if (b == -1)
					throw new Exception();

				result |= (b & 0x7f) << offset;

				if ((b & 0x80) == 0)
					return (uint)result;
			}

			throw new Exception();
		}

		static void WriteVarint32(GameNetStream stream, uint value)
		{
			while (true)
			{
				if ((value & ~0x7F) == 0)
				{
					stream.WriteByte((byte)value);
					return;
				}
				else
				{
					stream.WriteByte((byte)((value & 0x7F) | 0x80));
					value >>= 7;
				}
			}
		}

		static ulong ReadVarint64(GameNetStream stream)
		{
			long result = 0;
			int offset = 0;

			for (; offset < 64; offset += 7)
			{
				int b = stream.ReadByte();
				if (b == -1)
					throw new Exception();

				result |= ((long)(b & 0x7f)) << offset;

				if ((b & 0x80) == 0)
					return (ulong)result;
			}

			throw new Exception();
		}

		static void WriteVarint64(GameNetStream stream, ulong value)
		{
			while (true)
			{
				if ((value & ~0x7Ful) == 0)
				{
					stream.WriteByte((byte)value);
					return;
				}
				else
				{
					stream.WriteByte((byte)((value & 0x7F) | 0x80));
					value >>= 7;
				}
			}
		}


		public static void WritePrimitive(GameNetStream stream, bool value)
		{
			stream.WriteByte(value ? (byte)1 : (byte)0);
		}

		public static void ReadPrimitive(GameNetStream stream, out bool value)
		{
			var b = stream.ReadByte();
			if (b == -1)
				throw new Exception();
			value = b != 0;
		}

		public static void WritePrimitive(GameNetStream stream, byte value)
		{
			stream.WriteByte(value);
		}

		public static void ReadPrimitive(GameNetStream stream, out byte value)
		{
			var b = stream.ReadByte();
			if (b == -1)
				throw new Exception();
			value = (byte)b;
		}

		public static void WritePrimitive(GameNetStream stream, char value)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(GameNetStream stream, out char value)
		{
			value = (char)ReadVarint32(stream);
		}

		public static void WritePrimitive(GameNetStream stream, ushort value)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(GameNetStream stream, out ushort value)
		{
			value = (ushort)ReadVarint32(stream);
		}

		public static void WritePrimitive(GameNetStream stream, short value)
		{
			WriteVarint32(stream, EncodeZigZag32(value));
		}

		public static void ReadPrimitive(GameNetStream stream, out short value)
		{
			value = (short)DecodeZigZag32(ReadVarint32(stream));
		}

		public static void WritePrimitive(GameNetStream stream, uint value)
		{
			WriteVarint32(stream, value);
		}

		public static void ReadPrimitive(GameNetStream stream, out uint value)
		{
			value = ReadVarint32(stream);
		}

		public static void WritePrimitive(GameNetStream stream, int value)
		{
			WriteVarint32(stream, EncodeZigZag32(value));
		}

		public static void ReadPrimitive(GameNetStream stream, out int value)
		{
			value = DecodeZigZag32(ReadVarint32(stream));
		}

		public static void WritePrimitive(GameNetStream stream, ulong value)
		{
			WriteVarint64(stream, value);
		}

		public static void ReadPrimitive(GameNetStream stream, out ulong value)
		{
			value = ReadVarint64(stream);
		}

		public static void WritePrimitive(GameNetStream stream, long value)
		{
			WriteVarint64(stream, EncodeZigZag64(value));
		}

		public static void ReadPrimitive(GameNetStream stream, out long value)
		{
			value = DecodeZigZag64(ReadVarint32(stream));
		}

		public static void WritePrimitive(GameNetStream stream, string value)
		{
			if (value == null)
			{
				WritePrimitive(stream, 0xffffffffU);
				return;
			}

			WritePrimitive(stream, (uint)value.Length);

			foreach (char c in value)
				WritePrimitive(stream, c);
		}

		public static void ReadPrimitive(GameNetStream stream, out string value)
		{
			uint len;
			ReadPrimitive(stream, out len);

			if (len == 0xffffffffU)
			{
				value = null;
				return;
			}

			var arr = new char[len];
			for (uint i = 0; i < len; ++i)
				ReadPrimitive(stream, out arr[i]);

			value = new string(arr);
		}
	}
}
