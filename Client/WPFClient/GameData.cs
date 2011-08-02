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

	// XXX we need a wrapper for the string, so that ListBox manages to scroll the last item into view.
	// otherwise listbox will compare the strings, and scroll to first occurance of the string...
	class GameInformMessage
	{
		string m_str;
		public Environment Environment { get; private set; }
		public IntPoint3D Location { get; private set; }

		public GameInformMessage(string str)
		{
			m_str = str;
		}

		public GameInformMessage(string str, Environment env, IntPoint3D location)
		{
			m_str = str;
			this.Environment = env;
			this.Location = location;
		}

		public override string ToString()
		{
			return m_str;
		}
	}

	class GameData : DependencyObject
	{
		public static readonly GameData Data = new GameData();

		public GameData()
		{
			this.Jobs = new ObservableCollection<Dwarrowdelf.Jobs.IJob>();
			this.SymbolDrawingCache = new SymbolDrawingCache("SymbolInfosChar.xaml");
			m_messages = new ObservableCollection<GameInformMessage>();
			this.Messages = new ReadOnlyObservableCollection<GameInformMessage>(m_messages);
		}

		public MainWindow MainWindow { get { return (MainWindow)Application.Current.MainWindow; } }

		public SymbolDrawingCache SymbolDrawingCache { get; private set; }

		bool m_previousWasTickMessage = true;

		public void AddMessage(Environment env, IntPoint3D location, string format, params object[] args)
		{
			AddMessageInternal(env, location, String.Format(format, args));
			m_previousWasTickMessage = false;
		}

		public void AddMessage(Environment env, IntPoint3D location, string message)
		{
			AddMessageInternal(env, location, message);
			m_previousWasTickMessage = false;
		}

		public void AddMessage(GameObject ob, string format, params object[] args)
		{
			AddMessageInternal(ob.Environment, ob.Location, String.Format(format, args));
			m_previousWasTickMessage = false;
		}

		public void AddMessage(GameObject ob, string message)
		{
			AddMessageInternal(ob.Environment, ob.Location, message);
			m_previousWasTickMessage = false;
		}

		public void AddTickMessage()
		{
			if (m_previousWasTickMessage)
				return;

			AddMessageInternal(null, new IntPoint3D(), "---");

			m_previousWasTickMessage = true;
		}

		void AddMessageInternal(Environment env, IntPoint3D location, string message)
		{
			if (m_messages.Count > 100)
				m_messages.RemoveAt(0);

			//Trace.TraceInformation(message);

			m_messages.Add(new GameInformMessage(message, env, location));
		}

		ObservableCollection<GameInformMessage> m_messages;
		public ReadOnlyObservableCollection<GameInformMessage> Messages { get; private set; }

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
	}
}
