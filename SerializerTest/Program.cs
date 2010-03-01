using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetSerializer;
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

		interface IFoo
		{
			void Test();
		}

		[Serializable]
		class B : IFoo
		{
			public int asd;

			public void Test()
			{
				throw new NotImplementedException();
			}

			public override string ToString()
			{
				return String.Format("B({0})", asd);
			}
		}

		[Serializable]
		class A
		{
			public int kala;
			public IFoo Ob;
			public IList<int> List;

			public override string ToString()
			{
				return String.Format("A({0}, {1})", kala, Ob);
			}
		}

		static void Test()
		{
			var rootTypes = new Type[] { typeof(A), typeof(B), typeof(List<int>) };
			NetSerializer.Serializer.Initialize(rootTypes.ToArray());

			Stream stream = new MemoryStream(1024 * 1024);

			{
				var ob = new A() { kala = 5, Ob = new B() { asd = 10 }, List = new List<int>() { 1, 2, 3 } };
				Console.WriteLine("Serializing: {0}", ob.ToString());
				NetSerializer.Serializer.Serialize(stream, ob);
			}

			{
				stream.Position = 0;
				while (true)
				{
					int b = stream.ReadByte();
					if (b == -1)
						break;

					Console.Write("{0:x2} ", (byte)b);
				}
				Console.WriteLine();
			}

			{
				stream.Position = 0;

				var ob = NetSerializer.Serializer.Deserialize(stream);

				Console.WriteLine("Deserialized: {0}", ob.ToString());
			}

			Console.ReadLine();
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

			NetSerializer.Serializer.Initialize(rootTypes.ToArray());

			maxsize = 0;

			var sw = Stopwatch.StartNew();

			for (int l = 0; l < loops; ++l)
			{
				stream.Seek(0, SeekOrigin.Begin);

				foreach (var o in obs)
					NetSerializer.Serializer.Serialize(stream, o);

				maxsize = (int)stream.Position;

				stream.Seek(0, SeekOrigin.Begin);

				while (stream.Position < stream.Length)
				{
					var ob = (Message)NetSerializer.Serializer.Deserialize(stream);
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
