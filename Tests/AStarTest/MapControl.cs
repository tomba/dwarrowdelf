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

/*
 * Benchmark pitkän palkin oikeasta alareunasta vasempaan:
 * BinaryHeap 3D: mem 12698760, ticks 962670
 * BinaryHeap 3D: mem 10269648, ticks 1155656 (short IntPoint3D)
 * SimpleList 3D: mem 12699376, ticks 88453781
 * 
 * 1.1.2010		mem 12698760, ticks 962670
 */
namespace AStarTest
{
	class MapControl : MapControlBase<MapControlTile>, INotifyPropertyChanged, Dwarrowdelf.AStar.IAStarEnvironment
	{
		public class TileInfo
		{
			public IntPoint3D Location { get; set; }
		}

		Map m_map;

		const int MapWidth = 400;
		const int MapHeight = 400;
		const int MapDepth = 10;

		int m_z;

		int m_state;
		IntPoint3D m_from, m_to;

		bool m_removing;

		public TileInfo CurrentTileInfo { get; private set; } // used to inform the UI

		public Positioning SrcPos { get; set; }
		public Positioning DstPos { get; set; }

		public event Action SomethingChanged;

		public MapControl()
		{
			this.UseLayoutRounding = false;

			this.CurrentTileInfo = new TileInfo();

			this.Focusable = true;

			this.TileSize = 32;

			m_map = new Map(MapWidth, MapHeight, MapDepth);

			base.CenterPos = new IntPoint(10, 10);
			ClearMap();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);
			/*
			m_from = new IntPoint(6, 0);
			m_to = new IntPoint(4, 0);
			DoAStar(m_from, m_to);
			Application.Current.Shutdown();*/
		}

		void ClearMap()
		{
			m_result = null;
			InvalidateTiles();
		}

		protected override void UpdateTilesOverride(MapControlTile[,] tileArray)
		{
			if (SomethingChanged != null)
				SomethingChanged();

			for (int y = 0; y < this.Rows; ++y)
			{
				for (int x = 0; x < this.Columns; ++x)
				{
					var tile = tileArray[y, x];

					var ml = ScreenLocationToMapLocation(new IntPoint(x, y));

					UpdateTile(tile, ml);
				}
			}
		}

		protected void UpdateTile(UIElement _tile, IntPoint _ml)
		{
			MapControlTile tile = (MapControlTile)_tile;
			IntPoint3D ml = new IntPoint3D(_ml, m_z);

			tile.ClearTile();

			if (!m_map.Bounds.Contains(ml))
			{
				tile.Brush = Brushes.DarkBlue;
			}
			else
			{
				tile.Weight = m_map.GetWeight(ml);
				tile.Stairs = m_map.GetStairs(ml);

				if (m_result != null && m_result.Nodes.ContainsKey(ml))
				{
					var node = m_result.Nodes[ml];
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

			tile.InvalidateVisual();
		}

		public int Z
		{
			get { return m_z; }

			set
			{
				if (m_z == value)
					return;

				m_z = value;
				InvalidateTiles();
				var old = this.CurrentTileInfo.Location;
				this.CurrentTileInfo.Location = new IntPoint3D(old.X, old.Y, m_z);
				Notify("CurrentTileInfo");
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			IntPoint _ml = ScreenPointToMapLocation(e.GetPosition(this));
			IntPoint3D ml = new IntPoint3D(_ml, m_z);

			if (!m_map.Bounds.Contains(ml))
			{
				Console.Beep();
				return;
			}

			if (e.LeftButton == MouseButtonState.Pressed)
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
					m_state = 2;
					DoAStar(m_from, ml);
					m_state = 3;
				}
			}
			else if (e.RightButton == MouseButtonState.Pressed)
			{
				m_removing = m_map.GetBlocked(ml);
				m_map.SetBlocked(ml, !m_removing);
			}

			InvalidateTiles();
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			IntPoint _ml = ScreenPointToMapLocation(e.GetPosition(this));
			IntPoint3D ml = new IntPoint3D(_ml, m_z);

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

				InvalidateTiles();
			}
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

		Dwarrowdelf.AStar.AStarStatus m_astarStatus;
		public Dwarrowdelf.AStar.AStarStatus Status
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

		public event Action<Dwarrowdelf.AStar.AStarResult> AStarDone;
		IEnumerable<IntPoint3D> m_path;
		Dwarrowdelf.AStar.AStarResult m_result;

		void DoAStar(IntPoint3D src, IntPoint3D dst)
		{
			long startBytes, stopBytes;
			Stopwatch sw = new Stopwatch();
			startBytes = GC.GetTotalMemory(true);
			sw.Start();
			var result = Dwarrowdelf.AStar.AStar.Find(this, src, this.SrcPos, dst, this.DstPos);
			sw.Stop();
			stopBytes = GC.GetTotalMemory(true);

			this.MemUsed = stopBytes - startBytes;
			this.TicksUsed = sw.ElapsedTicks;

			m_result = result;

			this.Status = m_result.Status;

			if (m_result.Status != Dwarrowdelf.AStar.AStarStatus.Found)
			{
				m_path = null;
				this.PathLength = 0;
				return;
			}

			var pathList = new List<IntPoint3D>();
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
		}

		IEnumerable<Direction> Dwarrowdelf.AStar.IAStarEnvironment.GetValidDirs(IntPoint3D p)
		{
			return GetTileDirs(p);
		}

		int Dwarrowdelf.AStar.IAStarEnvironment.GetTileWeight(IntPoint3D p)
		{
			return m_map.GetWeight(p);
		}

		bool Dwarrowdelf.AStar.IAStarEnvironment.CanEnter(IntPoint3D p)
		{
			return m_map.Bounds.Contains(p) && !m_map.GetBlocked(p);
		}


		IEnumerable<Direction> GetTileDirs(IntPoint3D p)
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

		void Notify(string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	class MapControlTile : UIElement
	{
		static Pen s_edgePen;

		static MapControlTile()
		{
			s_edgePen = new Pen(Brushes.Gray, 1);
			s_edgePen.Freeze();
		}

		public MapControlTile()
		{
			this.IsHitTestVisible = false;
		}

		public Brush Brush;
		public int G;
		public int H;
		public Direction From;
		public int Weight;
		public Stairs Stairs;

		public void ClearTile()
		{
			this.Brush = Brushes.Black;
			this.G = 0;
			this.H = 0;
			this.From = Direction.None;
			this.Weight = 0;
			this.Stairs = Stairs.None;
		}

		protected override void OnRender(DrawingContext dc)
		{
			dc.DrawRectangle(this.Brush, s_edgePen, new Rect(this.RenderSize));

			if (this.Stairs == Stairs.Down)
			{
				double tri = this.RenderSize.Width / 3;
				dc.DrawLine(new Pen(Brushes.White, 2), new Point(tri, tri), new Point(tri * 2, this.RenderSize.Height / 2));
				dc.DrawLine(new Pen(Brushes.White, 2), new Point(tri * 2, this.RenderSize.Height / 2), new Point(tri, tri * 2));
			}
			else if (this.Stairs == Stairs.Up)
			{
				double tri = this.RenderSize.Width / 3;
				dc.DrawLine(new Pen(Brushes.White, 2), new Point(tri * 2, tri), new Point(tri, this.RenderSize.Height / 2));
				dc.DrawLine(new Pen(Brushes.White, 2), new Point(tri, this.RenderSize.Height / 2), new Point(tri * 2, tri * 2));
			}

			if (this.RenderSize.Width < 32)
				return;

			if (From != Direction.None)
			{
				var iv = IntVector.FromDirection(From);
				iv *= (int)Math.Round(this.RenderSize.Width / 3);
				Point mp = new Point(this.RenderSize.Width / 2, this.RenderSize.Height / 2);
				dc.DrawEllipse(Brushes.White, null, mp, 3, 3);
				dc.DrawLine(new Pen(Brushes.White, 2), mp, mp + new Vector(iv.X, -iv.Y));
			}

			if (this.Weight != 0)
			{
				var ft = new FormattedText(this.Weight.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 8, Brushes.White);
				dc.DrawText(ft, new Point(this.RenderSize.Width - ft.Width - 2, 2));
			}

			if (G != 0 || H != 0)
			{
				var ft = new FormattedText(G.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 8, Brushes.White);
				dc.DrawText(ft, new Point(2, this.RenderSize.Height - ft.Height - 2));

				ft = new FormattedText(H.ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 8, Brushes.White);
				dc.DrawText(ft, new Point(this.RenderSize.Width - ft.Width - 2, this.RenderSize.Height - ft.Height - 2));

				ft = new FormattedText((G + H).ToString(), System.Globalization.CultureInfo.CurrentCulture,
					FlowDirection.LeftToRight, new Typeface("Verdana"), 8, Brushes.White);
				dc.DrawText(ft, new Point(2, 2));
			}
		}
	}
}
