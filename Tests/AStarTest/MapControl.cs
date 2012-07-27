using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;

using Dwarrowdelf;
using Dwarrowdelf.Client;
using AStarTest;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Dwarrowdelf.Client.TileControl;
using Dwarrowdelf.AStar;

/*
 * Benchmark pitkän palkin oikeasta alareunasta vasempaan:
 * BinaryHeap 3D: mem 12698760, ticks 962670
 * BinaryHeap 3D: mem 10269648, ticks 1155656 (short IntPoint3)
 * SimpleList 3D: mem 12699376, ticks 88453781
 * 
 * 1.1.2010		mem 12698760, ticks 962670
 */
namespace AStarTest
{
	class MapControl : Dwarrowdelf.Client.TileControl.TileControlCore, INotifyPropertyChanged, Dwarrowdelf.AStar.IAStarEnvironment
	{
		public class TileInfo
		{
			public IntPoint3 Location { get; set; }
		}

		Map m_map;

		const int MapWidth = 400;
		const int MapHeight = 400;
		const int MapDepth = 10;

		int m_z;

		int m_state;
		IntPoint3 m_from, m_to;

		bool m_removing;

		public TileInfo CurrentTileInfo { get; private set; } // used to inform the UI

		public DirectionSet SrcPos { get; set; }
		public DirectionSet DstPos { get; set; }

		public event Action SomethingChanged;

		RenderView m_renderView;

		public MapControl()
		{
			this.SrcPos = this.DstPos = DirectionSet.Exact;

			//this.UseLayoutRounding = false;

			this.CurrentTileInfo = new TileInfo();

			this.Focusable = true;

			this.TileSize = 32;

			m_map = new Map(MapWidth, MapHeight, MapDepth);

			ClearMap();

			this.DragStarted += OnDragStarted;
			this.DragEnded += OnDragEnded;
			this.Dragging += OnDragging;
			this.DragAborted += OnDragAborted;
			this.MouseClicked += OnMouseClicked;
		}

		protected override void OnInitialized(EventArgs e)
		{
			m_renderView = new RenderView();

			var renderer = new Renderer(m_renderView);

			SetRenderer(renderer);

			this.TileLayoutChanged += OnTileArrangementChanged;
			this.AboutToRender += OnAboutToRender;

			base.OnInitialized(e);

			base.CenterPos = new Point(10, 10);
		}

		void ClearMap()
		{
			m_path = null;
			m_result = null;
			m_nodes = null;
			InvalidateTileData();
		}

		void OnTileArrangementChanged(IntSize2 gridSize, double tileSize, Point centerPos)
		{
			if (SomethingChanged != null)
				SomethingChanged();

			m_renderView.SetGridSize(gridSize);
		}

		void OnAboutToRender()
		{
			var width = m_renderView.Width;
			var height = m_renderView.Height;
			var grid = m_renderView.Grid;

			for (int y = 0; y < height; ++y)
			{
				for (int x = 0; x < width; ++x)
				{
					var tile = grid[y, x];

					var _ml = ScreenTileToMapTile(new Point(x, y));
					var ml = new IntPoint3(PointToIntPoint(_ml), m_z);

					UpdateTile(tile, ml);
				}
			}
		}

		IntPoint2 PointToIntPoint(Point p)
		{
			return new IntPoint2((int)Math.Round(p.X), (int)Math.Round(p.Y));
		}


		void UpdateTile(RenderTileData tile, IntPoint3 ml)
		{
			tile.ClearTile();

			if (!m_map.Bounds.Contains(ml))
			{
				tile.Brush = Brushes.DarkBlue;
			}
			else
			{
				tile.Weight = m_map.GetWeight(ml);
				tile.Stairs = m_map.GetStairs(ml);

				if (m_nodes != null && m_nodes.ContainsKey(ml))
				{
					var node = m_nodes[ml];
					tile.G = node.G;
					tile.H = node.H;

					if (node.Parent == null)
						tile.From = Direction.None;
					else
						tile.From = (node.Parent.Loc - node.Loc).ToDirection();

					if (m_path != null && m_path.Contains(ml))
						tile.Brush = Brushes.DarkGray;
				}

				if (m_map.GetBlocked(ml))
				{
					tile.Brush = Brushes.Blue;
				}
				else if (m_state > 0 && ml == m_from)
				{
					tile.Brush = Brushes.Green;
				}
				else if (m_state > 1 && ml == m_to)
				{
					tile.Brush = Brushes.Red;
				}
			}
		}

		public int Z
		{
			get { return m_z; }

			set
			{
				if (m_z == value)
					return;

				m_z = value;
				InvalidateTileData();
				var old = this.CurrentTileInfo.Location;
				this.CurrentTileInfo.Location = new IntPoint3(old.X, old.Y, m_z);
				Notify("CurrentTileInfo");
			}
		}

		void OnMouseClicked(object sender, MouseButtonEventArgs e)
		{
			var pos = e.GetPosition(this);
			var ml = ScreenPointToMapLocation(pos);

			if (!m_map.Bounds.Contains(ml))
			{
				Console.Beep();
				return;
			}

			if (e.ChangedButton == MouseButton.Left)
			{
				if (m_state == 0 || m_state == 3)
				{
					m_from = ml;
					m_state = 1;
					ClearMap();
				}
				else
				{
					m_to = ml;
					DoAStar(m_from, ml);
					m_state = 3;
				}
			}
			else if (e.ChangedButton == MouseButton.Right)
			{
				m_removing = m_map.GetBlocked(ml);
				m_map.SetBlocked(ml, !m_removing);
				InvalidateTileData();
			}
		}

		public IntPoint3 ScreenPointToMapLocation(Point p)
		{
			var ml = ScreenPointToMapTile(p);
			return new IntPoint3((int)Math.Round(ml.X), (int)Math.Round(ml.Y), this.Z);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			var pos = e.GetPosition(this);
			var ml = ScreenPointToMapLocation(pos);

			if (this.CurrentTileInfo.Location != ml)
			{
				this.CurrentTileInfo.Location = ml;
				Notify("CurrentTileInfo");
			}

			if (e.RightButton == MouseButtonState.Pressed)
			{
				if (!m_map.Bounds.Contains(ml))
				{
					Console.Beep();
					return;
				}

				m_map.SetBlocked(ml, !m_removing);

				InvalidateTileData();
			}
		}

		public void Signal()
		{
			m_contEvent.Set();
		}

		long m_memUsed;
		public long MemUsed
		{
			get { return m_memUsed; }
			set { m_memUsed = value; Notify("MemUsed"); }
		}

		long m_ticksUsed;
		public long TicksUsed
		{
			get { return m_ticksUsed; }
			set { m_ticksUsed = value; Notify("TicksUsed"); }
		}

		AStarStatus m_astarStatus;
		public AStarStatus Status
		{
			get { return m_astarStatus; }
			set { m_astarStatus = value; Notify("Status"); }
		}

		int m_pathLength;
		public int PathLength
		{
			get { return m_pathLength; }
			set { m_pathLength = value; Notify("PathLength"); }
		}

		public event Action<AStarResult> AStarDone;
		IEnumerable<IntPoint3> m_path;
		AStarResult m_result;
		IDictionary<IntPoint3, AStarNode> m_nodes;

		void DoAStar(IntPoint3 src, IntPoint3 dst)
		{
			long startBytes, stopBytes;
			Stopwatch sw = new Stopwatch();
			startBytes = GC.GetTotalMemory(true);
			sw.Start();

			if (!this.Step)
			{
				m_result = AStarFinder.Find(this, src, this.SrcPos, dst, this.DstPos);

				sw.Stop();
				stopBytes = GC.GetTotalMemory(true);

				this.MemUsed = stopBytes - startBytes;
				this.TicksUsed = sw.ElapsedTicks;

				this.Status = m_result.Status;
				m_nodes = m_result.Nodes;

				if (m_result.Status != AStarStatus.Found)
				{
					m_path = null;
					this.PathLength = 0;
					return;
				}

				var pathList = new List<IntPoint3>();
				var n = m_result.LastNode;
				while (n.Parent != null)
				{
					pathList.Add(n.Loc);
					n = n.Parent;
				}
				m_path = pathList;

				this.PathLength = m_result.GetPathReverse().Count();

				if (AStarDone != null)
					AStarDone(m_result);

				InvalidateTileData();
			}
			else
			{
				m_contEvent.Reset();

				Task.Factory.StartNew(() => m_result = AStarFinder.Find(this, src, this.SrcPos, dst, this.DstPos))
					.ContinueWith((task) =>
						{
							sw.Stop();
							stopBytes = GC.GetTotalMemory(true);

							this.MemUsed = stopBytes - startBytes;
							this.TicksUsed = sw.ElapsedTicks;

							this.Status = m_result.Status;
							m_nodes = m_result.Nodes;

							if (m_result.Status != AStarStatus.Found)
							{
								m_path = null;
								this.PathLength = 0;
								return;
							}

							var pathList = new List<IntPoint3>();
							var n = m_result.LastNode;
							while (n.Parent != null)
							{
								pathList.Add(n.Loc);
								n = n.Parent;
							}
							m_path = pathList;

							this.PathLength = m_result.GetPathReverse().Count();

							if (AStarDone != null)
								AStarDone(m_result);

							InvalidateTileData();
						}, TaskScheduler.FromCurrentSynchronizationContext());
			}
		}

		IEnumerable<Direction> Dwarrowdelf.AStar.IAStarEnvironment.GetValidDirs(IntPoint3 p)
		{
			return GetTileDirs(p);
		}

		int Dwarrowdelf.AStar.IAStarEnvironment.GetTileWeight(IntPoint3 p)
		{
			return m_map.GetWeight(p);
		}

		bool Dwarrowdelf.AStar.IAStarEnvironment.CanEnter(IntPoint3 p)
		{
			return m_map.Bounds.Contains(p) && !m_map.GetBlocked(p);
		}

		void IAStarEnvironment.Callback(IDictionary<IntPoint3, Dwarrowdelf.AStar.AStarNode> nodes)
		{
			if (!this.Step)
				return;

			Dispatcher.Invoke(new Action(delegate
			{
				m_nodes = nodes;
				InvalidateTileData();
				UpdateLayout();
			}));

			m_contEvent.WaitOne();
		}

		public bool Step { get; set; }
		AutoResetEvent m_contEvent = new AutoResetEvent(false);


		IEnumerable<Direction> GetTileDirs(IntPoint3 p)
		{
			var map = m_map;
			foreach (var d in DirectionExtensions.PlanarDirections)
			{
				var l = p + d;
				if (map.Bounds.Contains(l) && map.GetBlocked(l) == false)
					yield return d;
			}

			var stairs = m_map.GetStairs(p);

			if (stairs == Stairs.Up || stairs == Stairs.UpDown)
				yield return Direction.Up;

			if (stairs == Stairs.Down || stairs == Stairs.UpDown)
				yield return Direction.Down;
		}

		Point m_mapTile;

		void OnDragStarted(Point pos)
		{
			m_mapTile = ScreenPointToMapTile(pos);
			Cursor = Cursors.ScrollAll;
		}

		void OnDragEnded(Point pos)
		{
			ClearValue(UserControl.CursorProperty);
		}

		void OnDragging(Point pos)
		{
			var v = MapTileToScreenPoint(m_mapTile) - pos;

			var sp = MapTileToScreenPoint(this.CenterPos) + v;

			var mt = ScreenPointToMapTile(sp);

			this.CenterPos = mt;
		}

		void OnDragAborted()
		{
			ClearValue(UserControl.CursorProperty);
		}


		void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
