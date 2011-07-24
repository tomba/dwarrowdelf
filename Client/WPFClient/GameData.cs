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
			this.SymbolDrawingCache = new SymbolDrawingCache("SymbolInfosChar.xaml");
			m_messages = new ObservableCollection<string>();
			this.Messages = new ReadOnlyObservableCollection<string>(m_messages);
		}

		public MainWindow MainWindow { get { return (MainWindow)Application.Current.MainWindow; } }

		public SymbolDrawingCache SymbolDrawingCache { get; private set; }

		public void AddMessage(string format, params object[] args)
		{
			AddMessage(String.Format(format, args));
		}

		public void AddMessage(string message)
		{
			if (m_messages.Count > 100)
				m_messages.RemoveAt(0);

			//Trace.TraceInformation(message);

			m_messages.Add(message);
		}

		ObservableCollection<string> m_messages;
		public ReadOnlyObservableCollection<string> Messages { get; private set; }

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
