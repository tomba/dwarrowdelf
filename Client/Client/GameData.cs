using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Windows.Threading;

namespace Dwarrowdelf.Client
{
	sealed class GameEvent
	{
		public string Message { get; private set; }
		public EnvironmentObject Environment { get; private set; }
		public IntPoint3D Location { get; private set; }

		public GameEvent(string str)
		{
			this.Message = str;
		}

		public GameEvent(string str, EnvironmentObject env, IntPoint3D location)
		{
			this.Message = str;
			this.Environment = env;
			this.Location = location;
		}
	}

	sealed class GameData : DependencyObject
	{
		public static readonly GameData Data = new GameData();

		public GameData()
		{
			this.Jobs = new ObservableCollection<Dwarrowdelf.Jobs.IJob>();
			this.SymbolDrawingCache = new Dwarrowdelf.Client.Symbols.SymbolDrawingCache("SymbolInfosChar.xaml");
			m_gameEvents = new ObservableCollection<GameEvent>();
			this.GameEvents = new ReadOnlyObservableCollection<GameEvent>(m_gameEvents);

			m_ipMessages = new ObservableCollection<Messages.IPOutputMessage>();
			this.IPMessages = new ReadOnlyObservableCollection<Messages.IPOutputMessage>(m_ipMessages);

			m_timer = new DispatcherTimer(DispatcherPriority.Background);
			m_timer.Interval = TimeSpan.FromMilliseconds(500);
			m_timer.Tick += delegate { if (_Blink != null) _Blink(); MainWindow.MapControl.InvalidateTileData(); };
		}

		event Action _Blink;

		DispatcherTimer m_timer;

		public event Action Blink
		{
			add
			{
				if (DesignerProperties.GetIsInDesignMode(this))
					return;

				if (_Blink == null)
					m_timer.IsEnabled = true;

				_Blink = (Action)Delegate.Combine(_Blink, value);
			}

			remove
			{
				if (DesignerProperties.GetIsInDesignMode(this))
					return;

				_Blink = (Action)Delegate.Remove(_Blink, value);

				if (_Blink == null)
					m_timer.IsEnabled = false;
			}
		}

		public UI.MainWindow MainWindow { get { return (UI.MainWindow)Application.Current.MainWindow; } }

		public Dwarrowdelf.Client.Symbols.SymbolDrawingCache SymbolDrawingCache { get; private set; }

		bool m_previousWasTickEvent = true;

		public void AddGameEvent(EnvironmentObject env, IntPoint3D location, string format, params object[] args)
		{
			AddGameEventInternal(env, location, String.Format(format, args));
			m_previousWasTickEvent = false;
		}

		public void AddGameEvent(EnvironmentObject env, IntPoint3D location, string message)
		{
			AddGameEventInternal(env, location, message);
			m_previousWasTickEvent = false;
		}

		public void AddGameEvent(MovableObject ob, string format, params object[] args)
		{
			AddGameEvent(ob.Environment, ob.Location, String.Format(format, args));
		}

		public void AddGameEvent(MovableObject ob, string message)
		{
			AddGameEvent(ob.Environment, ob.Location, message);
		}

		public void AddGameEvent(IAreaObject ob, string format, params object[] args)
		{
			AddGameEvent((EnvironmentObject)ob.Environment, ob.Area.Center, format, args);
		}

		public void AddGameEvent(IAreaObject ob, string message)
		{
			AddGameEvent((EnvironmentObject)ob.Environment, ob.Area.Center, message);
		}

		public void AddTickGameEvent()
		{
			if (m_previousWasTickEvent)
				return;

			AddGameEventInternal(null, new IntPoint3D(), "---");

			m_previousWasTickEvent = true;
		}

		void AddGameEventInternal(EnvironmentObject env, IntPoint3D location, string message)
		{
			if (m_gameEvents.Count > 100)
				m_gameEvents.RemoveAt(0);

			//Trace.TraceInformation(message);

			m_gameEvents.Add(new GameEvent(message, env, location));
		}

		ObservableCollection<GameEvent> m_gameEvents;
		public ReadOnlyObservableCollection<GameEvent> GameEvents { get; private set; }


		public void AddIPMessage(Dwarrowdelf.Messages.IPOutputMessage msg)
		{
			if (m_ipMessages.Count > 100)
				m_ipMessages.RemoveAt(0);

			m_ipMessages.Add(msg);
		}

		ObservableCollection<Dwarrowdelf.Messages.IPOutputMessage> m_ipMessages;
		public ReadOnlyObservableCollection<Dwarrowdelf.Messages.IPOutputMessage> IPMessages { get; private set; }

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
			DependencyProperty.Register("User", typeof(ClientUser), typeof(GameData), new UIPropertyMetadata(null, (o, v) => { GameData.Data.IsUserConnected = v.NewValue != null; }));


		public bool IsUserConnected
		{
			get { return (bool)GetValue(IsUserConnectedProperty); }
			set { SetValue(IsUserConnectedProperty, value); }
		}

		public static readonly DependencyProperty IsUserConnectedProperty =
			DependencyProperty.Register("IsUserConnected", typeof(bool), typeof(GameData), new UIPropertyMetadata(false));



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
