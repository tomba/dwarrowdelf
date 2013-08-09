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
	sealed class GameData : DependencyObject
	{
		public static GameData Data;

		public GameData()
		{
			this.TileSet = new TileSet(new Uri("/Dwarrowdelf.Client;component/TileSet/TileSet.png", UriKind.Relative));

			m_ipMessages = new ObservableCollection<Messages.IPOutputMessage>();
			this.IPMessages = new ReadOnlyObservableCollection<Messages.IPOutputMessage>(m_ipMessages);

			m_timer = new DispatcherTimer(DispatcherPriority.Background);
			m_timer.Interval = TimeSpan.FromMilliseconds(500);
			m_timer.Tick += delegate { if (_Blink != null) _Blink(); MainWindow.MapControl.InvalidateTileData(); };

			this.ConnectManager = new ConnectManager();

			this.NetStats = new ClientNetStatistics();
		}

		public ConnectManager ConnectManager { get; private set; }

		public ClientNetStatistics NetStats { get; private set; }

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

		TileSet m_tileSet;

		public TileSet TileSet
		{
			get { return m_tileSet; }
			set { m_tileSet = value; if (this.TileSetChanged != null) this.TileSetChanged(); }
		}

		public event Action TileSetChanged;


		public void AddIPMessage(Dwarrowdelf.Messages.IPOutputMessage msg)
		{
			if (m_ipMessages.Count > 100)
				m_ipMessages.RemoveAt(0);

			m_ipMessages.Add(msg);
		}

		ObservableCollection<Dwarrowdelf.Messages.IPOutputMessage> m_ipMessages;
		public ReadOnlyObservableCollection<Dwarrowdelf.Messages.IPOutputMessage> IPMessages { get; private set; }

		public ClientUser User
		{
			get { return (ClientUser)GetValue(UserProperty); }
			set { SetValue(UserProperty, value); }
		}

		public static readonly DependencyProperty UserProperty =
			DependencyProperty.Register("User", typeof(ClientUser), typeof(GameData), new UIPropertyMetadata(null));


		public World World
		{
			get { return (World)GetValue(WorldProperty); }
			set { SetValue(WorldProperty, value); }
		}

		public static readonly DependencyProperty WorldProperty =
			DependencyProperty.Register("World", typeof(World), typeof(GameData), new UIPropertyMetadata(null));



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
			{
				if (App.GameWindow.FocusedObject == null || App.GameWindow.FocusedObject.HasAction)
					GameData.Data.User.SendProceedTurn();
			}
		}
	}
}
