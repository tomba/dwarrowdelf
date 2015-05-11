using Dwarrowdelf.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Dwarrowdelf.Client
{
	public class MapControl3D : SharpDXHost, IMapControl
	{
		MyGame m_game;

		public MapControl3D()
		{
		}

		EnvironmentObject m_env;
		public EnvironmentObject Environment
		{
			get { return m_env; }
			set
			{
				m_env = value;

				if (m_game != null)
				{
					m_game.Stop();
					m_game.Dispose();
					m_game = null;
				}

				if (value != null)
				{
					m_game = new MyGame(this);
					m_game.Environment = value;
					m_game.Start();

					var dbg = new DebugWindow();
					dbg.Owner = System.Windows.Window.GetWindow(this);
					dbg.SetGame(m_game);
					dbg.Show();
				}
			}
		}

		public void GoTo(MovableObject ob)
		{
			if (ob == null)
			{
				this.Environment = null;
			}
			else
			{
				var env = ob.Environment;
				GoTo(env, ob.Location);
			}
		}

		public void GoTo(EnvironmentObject env, IntVector3 p)
		{
			this.Environment = env;
			m_game.GoTo(p);
		}

		public void ScrollTo(MovableObject ob)
		{
			var env = ob.Environment;
			GoTo(env, ob.Location);
		}

		public void ScrollTo(EnvironmentObject env, IntVector3 p)
		{
			this.Environment = env;
			m_game.GoTo(p);
		}

		public void ShowObjectsPopup(IntVector3 p)
		{
			// XXX
		}


		public event Action<MapSelection> GotSelection;

		MapSelectionMode m_selectionMode;
		public MapSelectionMode SelectionMode
		{
			get
			{
				return m_selectionMode;
			}
			set
			{
				m_selectionMode = value;
			}
		}

		MapSelection m_selection;
		public MapSelection Selection
		{
			get
			{
				return m_selection;
			}
			set
			{
				m_selection = value;
			}
		}

		TileAreaView m_hoverTileView = new TileAreaView();
		public TileAreaView HoverTileView
		{
			get { return m_hoverTileView; }
		}

		TileAreaView m_selectionTileView = new TileAreaView();
		public TileAreaView SelectionTileAreaView
		{
			get { return m_selectionTileView; }
		}
	}
}
