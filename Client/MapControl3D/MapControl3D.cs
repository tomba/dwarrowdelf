using Dwarrowdelf.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Dwarrowdelf.Client
{
	public class MapControlConfig
	{
		public MapControlPickMode PickMode { get; set; }
	}

	public enum MapControlPickMode
	{
		Underground,
		AboveGroud,
		Constant,
	}

	public class MapControl3D : SharpDXHost, IMapControl
	{
		MyGame m_game;
		ToolTipService m_toolTipService;

		public MapControlConfig Config { get; private set; }

		public MapControl3D()
		{
			this.Config = new MapControlConfig();

			this.HoverTileView = new TileAreaView();
			this.SelectionTileAreaView = new TileAreaView();

			m_game = new MyGame(this);

			m_game.CursorService.LocationChanged += OnCursorMoved;
			m_game.SelectionService.SelectionChanged += OnSelectionChanged;
			m_game.SelectionService.GotSelection += OnGotSelection;

			m_game.Start();

			m_toolTipService = new ToolTipService(this);
		}

		protected override void Dispose(bool disposing)
		{
			m_game.Stop();
			m_game.Dispose();
			m_game = null;

			base.Dispose(disposing);
		}

		public EnvironmentObject Environment
		{
			get { return m_game.Environment; }
			set { m_game.Environment = value; }
		}

		public void OpenDebugWindow()
		{
			var dbg = new DebugWindow();
			dbg.Owner = System.Windows.Window.GetWindow(this);
			dbg.SetGame(m_game);
			dbg.Show();
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
			if (ob == null)
			{
				this.Environment = null;
			}
			else
			{
				var env = ob.Environment;
				ScrollTo(env, ob.Location);
			}
		}

		public void ScrollTo(EnvironmentObject env, IntVector3 p)
		{
			this.Environment = env;
			m_game.ScrollTo(p);
		}

		public Rect GetPlacementRect(IntVector3 ml)
		{
			// XXX
			var view = m_game.Surfaces[0].Views[0];

			SharpDX.Matrix matrix = view.Camera.View * view.Camera.Projection;

			var p1 = ml.ToVector3();
			var p2 = p1 + new SharpDX.Vector3(1, 1, 0);
			SharpDX.Vector3 out1, out2;

			var vp = view.ViewPort;

			vp.Project(ref p1, ref matrix, out out1);
			vp.Project(ref p2, ref matrix, out out2);

			Rect rect = new Rect(new System.Windows.Point(out1.X, out1.Y), new System.Windows.Point(out2.X, out2.Y));

			return rect;
		}

		public event Action<MapSelection> GotSelection;

		public MapSelectionMode SelectionMode
		{
			get
			{
				return m_game.SelectionService.SelectionMode;
			}
			set
			{
				m_game.SelectionService.SelectionMode = value;
			}
		}

		public MapSelection Selection
		{
			get
			{
				return m_game.SelectionService.Selection;
			}
			set
			{
				m_game.SelectionService.Selection = value;
			}
		}

		public TileAreaView HoverTileView { get; private set; }

		void OnCursorMoved(IntVector3? pos)
		{
			if (pos.HasValue == false)
			{
				this.HoverTileView.ClearTarget();
			}
			else
			{
				var ml = pos.Value;

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
			if (this.GotSelection != null)
				this.GotSelection(selection);
		}
	}
}
