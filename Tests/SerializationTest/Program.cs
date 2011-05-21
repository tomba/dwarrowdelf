#pragma warning disable 169

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Dwarrowdelf;
using Dwarrowdelf.Server;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Reflection;

namespace SerializationTest
{
	class MyConv : IGameConverter
	{
		public object ConvertToSerializable(object parent, object value)
		{
			return String.Format("X{0}X", value);
		}

		public object ConvertFromSerializable(object parent, object value)
		{
			string str = (string)value;
			str = str.Substring(1, str.Length - 2);
			return Int32.Parse(str);
		}

		public Type OutputType
		{
			get { return typeof(string); }
		}
	}

	public class ClassMainBase
	{
		[DataMember]
		int privatebase;
	}

	[GameObject(UseRef = true)]
	public class ClassMain : ClassMainBase
	{
		[GameProperty("AA", Converter = typeof(MyConv))]
		public int a;

		public float b;

		[GameProperty]
		public Rect MyRect { get; set; }
		[GameProperty]
		public ClassA BorC;
		[GameProperty]
		public ClassMain Self;
		[GameProperty]
		int privint;

		[OnGameDeserialized]
		void OnDeserialized()
		{
		}
	}

	public interface IClassA
	{
	}

	public interface IClassB
	{
	}

	public interface IClassC
	{
	}

	[Serializable]
	public class ClassA : IClassA
	{
		public int IntA;
		int privA;
	}

	[Serializable]
	public class ClassB : ClassA, IClassB
	{
		public int IntB;
		int privB;
	}


	[Serializable]
	public class ClassC : ClassA, IClassC
	{
		public int IntC;
		public ClassB classB;
		int privC;
	}

	class Program
	{
		static void Main(string[] args)
		{
			Debug.Listeners.Add(new ConsoleTraceListener());

#if asd
			var p1 = new ClassMain()
			{
				a = 5,
				b = 4.5f,

				MyRect = new Rect(1, 4, 2, 3),

				BorC = new ClassB()
				{
					IntB = 4,
				},
			};
			p1.Self = p1;

			var p2 = new ClassB()
			{
				IntB = 4,
			};

			var p3 = new ClassC()
			{
				IntC = 7,
				classB = p2,
			};

			var arr = new object[] { p1, p2, p3 };
#else
			var p = new Rect(1, 4, 2, 3);
#endif

			var stream = new MemoryStream();


			JsonSerializer ser;
			JsonDeserializer deser;

			ser = new JsonSerializer(stream);
			var l = new List<ClassB>(); l.Add(new ClassB() { IntA = 3 });
			//var l = new ClassB[] { new ClassB() { IntA = 3 } };
			//var l = new Dictionary<string, ClassB>(); l.Add("kala", new ClassB() { IntA = 3 }); l.Add("kala2", new ClassB() { IntB = 5 });
			ser.Serialize(l);

			Debug.Print("\n---------");

			stream.Position = 0;
			stream.CopyTo(Console.OpenStandardOutput());

			Debug.Print("\n---------");

			stream.Position = 0;

			deser = new JsonDeserializer(stream);
			var ob = deser.Deserialize<List<ClassB>>();

			Debug.Print("\n---------");

			Debug.Print("Deserialized {0}", ob.GetType());

			stream = new MemoryStream();
			ser = new JsonSerializer(stream);
			ser.Serialize(ob);

			stream.Position = 0;
			stream.CopyTo(Console.OpenStandardOutput());
		}
	}
}
