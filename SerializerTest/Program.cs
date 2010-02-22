using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameSerializer;
using System.IO;
using MyGame;
using System.Reflection;
using MyGame.ClientMsgs;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using System.Xml;
using System.Runtime.Serialization;

namespace SerializerTest
{
	class Program
	{
		static void Main(string[] args)
		{
			//Benchmark();
			Test();
		}

		[Serializable]
		class A
		{
			public object Ob;

			public override string ToString()
			{
				return String.Format("{0}", Ob.ToString());
			}
		}

		static void Test()
		{
			var rootTypes = new Type[] { typeof(A) };

			GameSerializer.Serializer.Initialize(rootTypes.ToArray());

			Stream stream = new MemoryStream(1024 * 1024);
			var ob = new A() { Ob = "kala" };
			GameSerializer.Serializer.Serialize(stream, ob);

			stream.Position = 0;

			var ob2 = GameSerializer.Serializer.Deserialize(stream);

			if (ob.ToString() != ob2.ToString())
				throw new Exception();
		}

		static void Benchmark()
		{
			Stream stream = new MemoryStream(1024 * 1024);
			var obs = CreateObjects();
			const int loops = 50;
			int maxsize;
			TimeSpan time;

			Console.WriteLine("Calling my serializer");
			GC.Collect();
			TestMySerializer(stream, obs, loops, out maxsize, out time);
			Console.WriteLine("max size {0}, time {1}", maxsize, time);

			Console.WriteLine("Calling my BinaryFormatter");
			stream.Seek(0, SeekOrigin.Begin);
			GC.Collect();
			TestBinaryFormatter(stream, obs, loops, out maxsize, out time);
			Console.WriteLine("max size {0}, time {1}", maxsize, time);

			Console.WriteLine("done, press enter");
			Console.ReadLine();
		}

		static object[] CreateObjects()
		{
			Message[] actions = new EnqueueActionMessage[50];
			for (int i = 0; i < actions.Length; ++i)
			{
				if (i % 2 == 0)
					actions[i] = new EnqueueActionMessage() { Action = new MoveAction(Direction.NorthEast) { TransactionID = i } };
				else
					actions[i] = new EnqueueActionMessage() { Action = new MoveAction(Direction.West) { TransactionID = i } };
			}

			var tiles = new Tuple<IntPoint3D, TileData>[1000];
			for (int i = 0; i < tiles.Length; ++i)
				tiles[i] = new Tuple<IntPoint3D, TileData>(new IntPoint3D(i, i + 1, i + 2),
					new TileData() { FloorID = FloorID.NaturalFloor, InteriorID = InteriorID.NaturalWall });

			var terrains = new MapDataTerrainsList()
			{
				Environment = new ObjectID(123456),
				TileDataList = tiles,
			};


			return actions.Concat(new Message[] { terrains }).ToArray<object>();
		}

		static void TestMySerializer(Stream stream, object[] obs, int loops, out int maxsize, out TimeSpan time)
		{
			IEnumerable<Type> rootTypes = new Type[0];

			var messageTypes = typeof(MyGame.ClientMsgs.Message).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MyGame.ClientMsgs.Message)));
			var eventTypes = typeof(Event).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Event)));
			var actionTypes = typeof(GameAction).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameAction)));

			rootTypes = rootTypes.Concat(messageTypes);
			rootTypes = rootTypes.Concat(eventTypes);
			rootTypes = rootTypes.Concat(actionTypes);

			GameSerializer.Serializer.Initialize(rootTypes.ToArray());

			maxsize = 0;

			var sw = Stopwatch.StartNew();

			for (int l = 0; l < loops; ++l)
			{
				stream.Seek(0, SeekOrigin.Begin);

				foreach (var o in obs)
					GameSerializer.Serializer.Serialize(stream, o);

				maxsize = (int)stream.Position;

				stream.Seek(0, SeekOrigin.Begin);

				while (stream.Position < stream.Length)
				{
					var ob = (Message)GameSerializer.Serializer.Deserialize(stream);
					if (ob == null)
						throw new Exception();
				}
			}

			sw.Stop();

			time = sw.Elapsed;
		}

		static void TestBinaryFormatter(Stream stream, object[] obs, int loops, out int maxsize, out TimeSpan time)
		{
			var bf = new BinaryFormatter();

			maxsize = 0;

			var sw = Stopwatch.StartNew();

			for (int l = 0; l < loops; ++l)
			{
				stream.Seek(0, SeekOrigin.Begin);

				foreach (var o in obs)
					bf.Serialize(stream, o);

				maxsize = (int)stream.Position;

				stream.Seek(0, SeekOrigin.Begin);

				while (stream.Position < stream.Length)
				{
					var ob = (Message)bf.Deserialize(stream);
					if (ob == null)
						throw new Exception();
				}
			}

			sw.Stop();

			time = sw.Elapsed;
		}
	}
}
