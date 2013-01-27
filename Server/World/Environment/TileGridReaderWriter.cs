using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	sealed class TileGridReaderWriter : ISaveGameReaderWriter
	{
		public void Write(Newtonsoft.Json.JsonWriter writer, object value)
		{
			var grid = (TileData[, ,])value;

			int w = grid.GetLength(2);
			int h = grid.GetLength(1);
			int d = grid.GetLength(0);

			writer.WriteStartObject();

			writer.WritePropertyName("Width");
			writer.WriteValue(w);
			writer.WritePropertyName("Height");
			writer.WriteValue(h);
			writer.WritePropertyName("Depth");
			writer.WriteValue(d);

			writer.WritePropertyName("TileData");
			writer.WriteStartArray();

			var queue = new BlockingCollection<Tuple<int, byte[]>>();

			var writerTask = Task.Factory.StartNew(() =>
			{
				foreach (var tuple in queue.GetConsumingEnumerable())
				{
					writer.WriteValue(tuple.Item1);
					writer.WriteValue(tuple.Item2);
				}
			});

			Parallel.For(0, d, z =>
			{
				using (var memStream = new MemoryStream())
				{
					using (var compressStream = new DeflateStream(memStream, CompressionMode.Compress, true))
					using (var bufferedStream = new BufferedStream(compressStream))
					using (var streamWriter = new BinaryWriter(bufferedStream))
					{
						var srcArr = grid;

						for (int y = 0; y < h; ++y)
							for (int x = 0; x < w; ++x)
								streamWriter.Write(srcArr[z, y, x].Raw);
					}

					queue.Add(new Tuple<int, byte[]>(z, memStream.ToArray()));
				}
			});

			queue.CompleteAdding();

			writerTask.Wait();

			writer.WriteEndArray();
			writer.WriteEndObject();
		}

		static void ReadAndValidate(Newtonsoft.Json.JsonReader reader, Newtonsoft.Json.JsonToken token)
		{
			reader.Read();
			if (reader.TokenType != token)
				throw new Exception();
		}

		static int ReadIntProperty(Newtonsoft.Json.JsonReader reader, string propertyName)
		{
			reader.Read();
			if (reader.TokenType != Newtonsoft.Json.JsonToken.PropertyName || (string)reader.Value != propertyName)
				throw new Exception();

			reader.Read();
			if (reader.TokenType != Newtonsoft.Json.JsonToken.Integer)
				throw new Exception();

			return (int)(long)reader.Value;
		}

		public object Read(Newtonsoft.Json.JsonReader reader)
		{
			if (reader.TokenType != Newtonsoft.Json.JsonToken.StartObject)
				throw new Exception();

			int w = ReadIntProperty(reader, "Width");
			int h = ReadIntProperty(reader, "Height");
			int d = ReadIntProperty(reader, "Depth");

			var grid = new TileData[d, h, w];

			reader.Read();
			if (reader.TokenType != Newtonsoft.Json.JsonToken.PropertyName || (string)reader.Value != "TileData")
				throw new Exception();

			ReadAndValidate(reader, Newtonsoft.Json.JsonToken.StartArray);

			var queue = new BlockingCollection<Tuple<int, byte[]>>();

			var readerTask = Task.Factory.StartNew(() =>
			{
				for (int i = 0; i < d; ++i)
				{
					reader.Read();
					int z = (int)(long)reader.Value;

					byte[] buf = reader.ReadAsBytes();

					queue.Add(new Tuple<int, byte[]>(z, buf));
				}

				queue.CompleteAdding();
			});

			Parallel.For(0, d, i =>
			{
				var tuple = queue.Take();

				int z = tuple.Item1;
				byte[] arr = tuple.Item2;

				using (var memStream = new MemoryStream(arr))
				{
					using (var decompressStream = new DeflateStream(memStream, CompressionMode.Decompress))
					using (var streamReader = new BinaryReader(decompressStream))
					{
						for (int y = 0; y < h; ++y)
							for (int x = 0; x < w; ++x)
								grid[z, y, x].Raw = streamReader.ReadUInt64();
					}
				}
			});

			readerTask.Wait();

			ReadAndValidate(reader, Newtonsoft.Json.JsonToken.EndArray);
			ReadAndValidate(reader, Newtonsoft.Json.JsonToken.EndObject);

			return grid;
		}
	}
}
