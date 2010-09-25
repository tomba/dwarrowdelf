using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;

namespace Dwarrowdelf.Client
{
	static class MyExtensions
	{
		public static System.Windows.Media.Color ToWindowsColor(this GameColor color)
		{
			var rgb = new GameColorRGB(color);
			return System.Windows.Media.Color.FromRgb(rgb.R, rgb.G, rgb.B);
		}
	}

	class GameData : DependencyObject
	{
		public static readonly GameData Data = new GameData();

		public GameData()
		{
		}

		public MainWindow MainWindow { get { return (MainWindow)Application.Current.MainWindow; } }

		public ClientConnection Connection
		{
			get { return (ClientConnection)GetValue(ConnectionProperty); }
			set { SetValue(ConnectionProperty, value); }
		}

		public static readonly DependencyProperty ConnectionProperty =
			DependencyProperty.Register("Connection", typeof(ClientConnection), typeof(GameData), new UIPropertyMetadata(null));


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


		public bool IsSeeAll
		{
			get { return (bool)GetValue(IsSeeAllProperty); }
			set { SetValue(IsSeeAllProperty, value); }
		}

		public static readonly DependencyProperty IsSeeAllProperty =
			DependencyProperty.Register("IsSeeAll", typeof(bool), typeof(GameData), new UIPropertyMetadata(false));



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
			if (GameData.Data.Connection != null && (bool)e.NewValue == true)
				GameData.Data.Connection.SendProceedTurn();
		}

	}
}
