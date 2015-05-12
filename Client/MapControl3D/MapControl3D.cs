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
			this.HoverTileView = new TileAreaView();
			this.SelectionTileAreaView = new TileAreaView();
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
					m_game.MousePositionService.MouseLocationChanged -= OnCursorMoved;

					m_game.SelectionService.SelectionChanged -= OnSelectionChanged;
					m_game.SelectionService.GotSelection -= OnGotSelection;

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

					m_game.MousePositionService.MouseLocationChanged += OnCursorMoved;

					m_game.SelectionService.SelectionChanged += OnSelectionChanged;
					m_game.SelectionService.GotSelection += OnGotSelection;
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

		public TileAreaView HoverTileView { get; private set; }

		void OnCursorMoved()
		{
			if (m_game.MousePositionService.MouseLocation.HasValue == false)
			{
				this.HoverTileView.ClearTarget();
			}
			else
			{
				var ml = m_game.MousePositionService.MouseLocation.Value;

				if (this.Environment != null && this.Environment.Contains(ml))
				{
					this.HoverTileView.SetTarget(this.Environment, ml);
				}
				else
				{
					this.HoverTileView.ClearTarget();
				}
			}
		}

		public TileAreaView SelectionTileAreaView { get; private set; }

		void OnSelectionChanged(MapSelection selection)
		{
			if (!selection.IsSelectionValid)
			{
				this.SelectionTileAreaView.ClearTarget();
			}
			else
			{
				this.SelectionTileAreaView.SetTarget(this.Environment, selection.SelectionBox);
			}
		}

		void OnGotSelection(MapSelection selection)
		{
		}
	}
}
