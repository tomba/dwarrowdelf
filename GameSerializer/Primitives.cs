using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace GameSerializer
{
	public static class Primitives
	{
		public static MethodInfo GetWritePrimitive(Type type)
		{
			if (type.IsEnum)
				type = type.GetEnumUnderlyingType();

			var mi = typeof(Primitives).GetMethod("WritePrimitive", BindingFlags.Static | BindingFlags.Public, null,
				new Type[] { typeof(Stream), type }, null);

			return mi;
		}

		public static MethodInfo GetReadPrimitive(Type type)
		{
			if (!type.IsByRef)
				throw new Exception();

			if (type.GetElementType().IsEnum)
				type = type.GetElementType().GetEnumUnderlyingType().MakeByRefType();

			MethodInfo mi = typeof(Primitives).GetMethod("ReadPrimitive", BindingFlags.Static | BindingFlags.Public, null,
				new Type[] { typeof(Stream), type }, null);

			return mi;
		}


		public static void WritePrimitive(Stream stream, bool value)
		{
			stream.WriteByte(value ? (byte)1 : (byte)0);
		}

		public static void ReadPrimitive(Stream stream, out bool value)
		{
			Debug.Assert(stream.Length - stream.Position >= 1);

			value = stream.ReadByte() != 0;
		}

		public static void WritePrimitive(Stream stream, byte value)
		{
			stream.WriteByte(value);
		}

		public static void ReadPrimitive(Stream stream, out byte value)
		{
			Debug.Assert(stream.Length - stream.Position >= 1);

			value = (byte)stream.ReadByte();
		}

		public static void WritePrimitive(Stream stream, char value)
		{
			byte b1 = (byte)((value >> 8) & 0xff);
			byte b2 = (byte)((value >> 0) & 0xff);

			stream.WriteByte(b1);
			stream.WriteByte(b2);
		}

		public static void ReadPrimitive(Stream stream, out char value)
		{
			Debug.Assert(stream.Length - stream.Position >= 2);

			byte b1 = (byte)stream.ReadByte();
			byte b2 = (byte)stream.ReadByte();

			value = (char)((b1 << 8) | (b2 << 0));
		}

		public static void WritePrimitive(Stream stream, ushort value)
		{
			byte b1 = (byte)((value >> 8) & 0xff);
			byte b2 = (byte)((value >> 0) & 0xff);

			stream.WriteByte(b1);
			stream.WriteByte(b2);
		}

		public static void ReadPrimitive(Stream stream, out ushort value)
		{
			Debug.Assert(stream.Length - stream.Position >= 2);

			byte b1 = (byte)stream.ReadByte();
			byte b2 = (byte)stream.ReadByte();

			value = (ushort)((b1 << 8) | (b2 << 0));
		}

		public static void WritePrimitive(Stream stream, short value)
		{
			byte b1 = (byte)((value >> 8) & 0xff);
			byte b2 = (byte)((value >> 0) & 0xff);

			stream.WriteByte(b1);
			stream.WriteByte(b2);
		}

		public static void ReadPrimitive(Stream stream, out short value)
		{
			Debug.Assert(stream.Length - stream.Position >= 2);

			byte b1 = (byte)stream.ReadByte();
			byte b2 = (byte)stream.ReadByte();

			value = (short)((b1 << 8) | (b2 << 0));
		}

		public static void WritePrimitive(Stream stream, uint value)
		{
			byte b1 = (byte)((value >> 24) & 0xff);
			byte b2 = (byte)((value >> 16) & 0xff);
			byte b3 = (byte)((value >> 8) & 0xff);
			byte b4 = (byte)((value >> 0) & 0xff);

			stream.WriteByte(b1);
			stream.WriteByte(b2);
			stream.WriteByte(b3);
			stream.WriteByte(b4);
		}

		public static void ReadPrimitive(Stream stream, out uint value)
		{
			Debug.Assert(stream.Length - stream.Position >= 4);

			byte b1 = (byte)stream.ReadByte();
			byte b2 = (byte)stream.ReadByte();
			byte b3 = (byte)stream.ReadByte();
			byte b4 = (byte)stream.ReadByte();

			value = (uint)((b1 << 24) | (b2 << 16) | (b3 << 8) | (b4 << 0));
		}

		public static void WritePrimitive(Stream stream, int value)
		{
			byte b1 = (byte)((value >> 24) & 0xff);
			byte b2 = (byte)((value >> 16) & 0xff);
			byte b3 = (byte)((value >> 8) & 0xff);
			byte b4 = (byte)((value >> 0) & 0xff);

			stream.WriteByte(b1);
			stream.WriteByte(b2);
			stream.WriteByte(b3);
			stream.WriteByte(b4);
		}

		public static void ReadPrimitive(Stream stream, out int value)
		{
			Debug.Assert(stream.Length - stream.Position >= 4);

			byte b1 = (byte)stream.ReadByte();
			byte b2 = (byte)stream.ReadByte();
			byte b3 = (byte)stream.ReadByte();
			byte b4 = (byte)stream.ReadByte();

			value = (b1 << 24) | (b2 << 16) | (b3 << 8) | (b4 << 0);
		}

		public static void WritePrimitive(Stream stream, string value)
		{
			if (value == null)
			{
				WritePrimitive(stream, 0xffffffffU);
				return;
			}

			WritePrimitive(stream, (uint)value.Length);

			foreach (char c in value)
			{
				WritePrimitive(stream, c);
			}
		}

		public static void ReadPrimitive(Stream stream, out string value)
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
