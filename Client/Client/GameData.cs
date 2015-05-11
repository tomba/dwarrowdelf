using System;
using System.Linq;
using System.Windows.Threading;

namespace Dwarrowdelf.Client
{
	sealed class GameData : GameDataBase
	{
		public static GameData Data { get; private set; }

		public static void Create()
		{
			GameData.Data = new GameData();
		}

		public GameData()
		{
			this.TileSet = new TileSet(new Uri("/Dwarrowdelf.Client;component/TileSet/TileSet.png", UriKind.Relative));

			m_timer = new DispatcherTimer(DispatcherPriority.Background);
			m_timer.Interval = TimeSpan.FromMilliseconds(500);
			m_timer.Tick += delegate
			{
				if (_Blink != null)
					_Blink();
			};
		}

		DispatcherTimer m_timer;

		event Action _Blink;

		public event Action Blink
		{
			add
			{
				//if (DesignerProperties.GetIsInDesignMode(this))
				//	return;

				if (_Blink == null)
					m_timer.IsEnabled = true;

				_Blink = (Action)Delegate.Combine(_Blink, value);
			}

			remove
			{
				//if (DesignerProperties.GetIsInDesignMode(this))
				//	return;

				_Blink = (Action)Delegate.Remove(_Blink, value);

				if (_Blink == null)
					m_timer.IsEnabled = false;
			}
		}

		public event Action TileSetChanged;

		TileSet m_tileSet;
		public TileSet TileSet
		{
			get { return m_tileSet; }
			set { m_tileSet = value; if (this.TileSetChanged != null) this.TileSetChanged(); }
		}
	}
}