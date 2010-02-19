using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GameSerializer;
using System.IO;
using MyGame;
using System.Reflection;

namespace SerializerTest
{
	[Serializable]
	public struct MyPoint
	{
		public int m_x;
		public int m_y;

		public MyPoint(int x, int y)
		{
			m_x = x;
			m_y = y;
		}

		public override string ToString()
		{
			return String.Format("P({0},{1})", m_x, m_y);
		}
	}

	[Serializable]
	public struct MyRect
	{
		public MyPoint m_p1;
		public MyPoint m_p2;

		public MyRect(MyPoint p1, MyPoint p2)
		{
			m_p1 = p1;
			m_p2 = p2;
		}

		public override string ToString()
		{
			return String.Format("R({0}, {1})", m_p1, m_p2);
		}
	}


	[Serializable]
	public abstract class BaseMsg
	{
	}

	[Serializable]
	public class Msg : BaseMsg
	{
	}

	[Serializable]
	public class Msg0 : Msg
	{
		public int Value;
		public BaseMsg[] arr;

		public Msg0(int val)
		{
			this.Value = val;
		}

		public override string ToString()
		{
			return String.Format("Msg0({0})", Value);
		}
	}

	[Serializable]
	public class Msg1 : Msg
	{
		public MyRect Rect;

		public Msg1()
		{
			this.Rect = new MyRect();
		}

		public override string ToString()
		{
			return String.Format("Msg1({0})", Rect);
		}
	}

	[Serializable]
	public class Msg2 : Msg
	{
		public int[] IntArr;
		public MyPoint[] PointArr;

		public Msg2()
		{
		}

		public override string ToString()
		{
			return String.Format("Msg2({0}) ({1})",
				IntArr == null ? "<null>" : string.Join(", ", IntArr),
				PointArr == null ? "<null>" : string.Join(", ", PointArr));
		}
	}

	[Serializable]
	public class Msg3 : Msg
	{
		public Msg Message;

		public override string ToString()
		{
			return String.Format("Msg3( {0} )", Message);
		}
	}


	class Program
	{
		static void Main(string[] args)
		{
			Test1();
			//Test2();

			Console.WriteLine("done, press enter");
			Console.ReadLine();
		}

		class A
		{
			public int a; // { get; set; }
		}

		class B : A
		{
			public int b { get; set; }
		}

		static void Test1()
		{
			Console.WriteLine("MySerializer");

			var testTypes = new Type[] {
				typeof(Msg0),
				typeof(Msg1),
				typeof(Msg2),
				typeof(Msg3),
				typeof(Msg[]),
			};


			IEnumerable<Type> rootTypes = testTypes;

			var messageTypes = typeof(MyGame.ClientMsgs.Message).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(MyGame.ClientMsgs.Message)));
			var eventTypes = typeof(Event).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(Event)));
			var actionTypes = typeof(GameAction).Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(GameAction)));

			rootTypes = rootTypes.Concat(messageTypes);
			rootTypes = rootTypes.Concat(eventTypes);
			rootTypes = rootTypes.Concat(actionTypes);

			var ser = new GameSerializer.Serializer(rootTypes.ToArray());

			Stream stream = new MemoryStream(4096);


			stream.Seek(0, SeekOrigin.Begin);

			object[] obs = new object[] {
				//new MyPoint(1, 2),
				//new MyPoint(3, 4),
				//new MyRect(new MyPoint(1, 2), new MyPoint(3, 4)),
				//new Msg1() { Rect = new MyRect(new MyPoint(1, 2), new MyPoint(3, 4)) },
				//new Msg2() { IntArr = new int[] { 1, 2, 3}, PointArr = new MyPoint[] { new MyPoint(3, 2) } },
				//new Msg3() { Message = new Msg0(4) },
				//(Msg)new Msg0(123)  { arr = new Msg[] { new Msg1() { Rect = new MyRect(new MyPoint(12, 13), new MyPoint(14, 15)) } } },
				//new MyGame.ClientMsgs.MapDataTerrainsList() { Environment = new ObjectID(123456), TileDataList = new Tuple<IntPoint3D,TileData>[] {
				//	new Tuple<IntPoint3D, TileData>(new IntPoint3D(5, 6, 7), new TileData() { FloorID = FloorID.NaturalFloor })
				//} },

				new MoveAction(Direction.East) { TransactionID = 99 },
			};

			foreach (var o in obs)
			{
				Console.WriteLine("Serializing {0}", o);
				ser.Serialize(stream, o);
			}

			{
				stream.Seek(0, SeekOrigin.Begin);
				var arr = new byte[stream.Length];
				stream.Read(arr, 0, (int)stream.Length);
				for (int i = 0; i < arr.Length; ++i)
					Console.Write("{0:x2} ", arr[i]);
				Console.WriteLine();
			}


			stream.Seek(0, SeekOrigin.Begin);

			int idx = 0;
			while (stream.Position < stream.Length)
			{
				object o = ser.Deserialize(stream);
				Console.WriteLine("Deserialized {0}", o);

				if (obs[idx].ToString() != o.ToString())
					Console.WriteLine("FAIL!!!");
				idx++;
			}
		}

	}
}
