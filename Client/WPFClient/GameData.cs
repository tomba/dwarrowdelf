using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.Client
{
	static class MyExtensions
	{
		public static System.Windows.Media.Color ToWindowsColor(this GameColor color)
		{
			var rgb = color.ToGameColorRGB();
			return System.Windows.Media.Color.FromRgb(rgb.R, rgb.G, rgb.B);
		}
	}

	class GameData : DependencyObject
	{
		public static readonly GameData Data = new GameData();

		public GameData()
		{
			this.Jobs = new ObservableCollection<Dwarrowdelf.Jobs.IJob>();
		}

		public MainWindow MainWindow { get { return (MainWindow)Application.Current.MainWindow; } }

		public ClientConnection Connection
		{
			get { return (ClientConnection)GetValue(ConnectionProperty); }
			set { SetValue(ConnectionProperty, value); }
		}

		public static readonly DependencyProperty ConnectionProperty =
			DependencyProperty.Register("Connection", typeof(ClientConnection), typeof(GameData), new UIPropertyMetadata(null));


		public ClientUser User
		{
			get { return (ClientUser)GetValue(UserProperty); }
			set { SetValue(UserProperty, value); }
		}

		public static readonly DependencyProperty UserProperty =
			DependencyProperty.Register("User", typeof(ClientUser), typeof(GameData), new UIPropertyMetadata(null));



		public Living CurrentObject
		{
			get { return (Living)GetValue(CurrentObjectProperty); }
			set { SetValue(CurrentObjectProperty, value); }
		}

		public static readonly DependencyProperty CurrentObjectProperty =
			DependencyProperty.Register("CurrentObject", typeof(Living), typeof(GameData), new UIPropertyMetadata(null));



		public World World
		{
			get { return (World)GetValue(WorldProperty); }
			set { SetValue(WorldProperty, value); }
		}

		public static readonly DependencyProperty WorldProperty =
			DependencyProperty.Register("World", typeof(World), typeof(GameData), new UIPropertyMetadata(null));



		public bool DisableLOS
		{
			get { return (bool)GetValue(DisableLOSProperty); }
			set { SetValue(DisableLOSProperty, value); }
		}

		public static readonly DependencyProperty DisableLOSProperty =
			DependencyProperty.Register("DisableLOS", typeof(bool), typeof(GameData), new UIPropertyMetadata(false));



		public bool IsAutoAdvanceTurn
		{
			get { return (bool)GetValue(IsAutoAdvanceTurnProperty); }
			set { SetValue(IsAutoAdvanceTurnProperty, value); }
		}

		public static readonly DependencyProperty IsAutoAdvanceTurnProperty =
			DependencyProperty.Register("IsAutoAdvanceTurn", typeof(bool), typeof(GameData), new UIPropertyMetadata(false, IsAutoAdvanceTurnChanged));

		static void IsAutoAdvanceTurnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (GameData.Data.User != null && (bool)e.NewValue == true)
				GameData.Data.User.SendProceedTurn();
		}

		public ObservableCollection<Dwarrowdelf.Jobs.IJob> Jobs { get; private set; }

		public Action SaveEvent;

		class GameObjectConverter : ISaveGameConverter
		{
			#region ISaveGameConverter Members

			public object ConvertToSerializable(object value)
			{
				var ob = (IBaseGameObject)value;
				return ob.ObjectID;
			}

			public object ConvertFromSerializable(object value)
			{
				var oid = (ObjectID)value;
				var ob = GameData.Data.World.FindObject(oid);
				if (ob == null)
					throw new Exception();
				return ob;
			}

			public Type InputType { get { return typeof(IBaseGameObject); } }

			public Type OutputType { get { return typeof(ObjectID); } }

			#endregion
		}

		internal void Save(Guid id)
		{
			var saveData = new SaveData()
			{
			};

			Trace.TraceInformation("Saving client data");
			var watch = Stopwatch.StartNew();

			string data;

			using (var stream = new System.IO.MemoryStream())
			{
				using (var serializer = new Dwarrowdelf.SaveGameSerializer(stream, new [] { new GameObjectConverter() }))
				{
					serializer.Serialize(saveData);

					stream.Position = 0;

					using (StreamReader reader = new StreamReader(stream))
						data = reader.ReadToEnd();
				}
			}

			watch.Stop();
			Trace.TraceInformation("Saving client data took {0}", watch.Elapsed);

			var msg = new Messages.SaveClientDataReplyMessage() { ID = id, Data = data };
			this.Connection.Send(msg);

			if (SaveEvent != null)
				SaveEvent();
		}

		internal void Load(string dataStr)
		{
			Trace.TraceInformation("Loading client data");
			var watch = Stopwatch.StartNew();

			var reader = new StringReader(dataStr);

			var deserializer = new Dwarrowdelf.SaveGameDeserializer(reader, new [] { new GameObjectConverter() });
			var data = deserializer.Deserialize<SaveData>();

			// XXX restore state

			watch.Stop();
			Trace.TraceInformation("Loading game took {0}", watch.Elapsed);
		}

		[Serializable]
		class SaveData
		{
		}
	}
}
