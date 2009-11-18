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
using System.Diagnostics;
using AStarTest;

namespace MyGame
{
	class MapControl : MapControlBase
	{
		struct MapTile
		{
			public bool Blocked;
		}

		Grid2D<MapTile> m_realMap;

		Grid2D<Square> m_map;

		const int MapWidth = 400;
		const int MapHeight = 400;

		int m_state;
		IntPoint m_from, m_to;

		public MapControl()
		{
			this.Focusable = true;

			this.TileSize = 32;

			m_realMap = new Grid2D<MapTile>(MapWidth, MapHeight);
			for (int y = 0; y < 350; ++y)
				m_realMap[5, y] = new MapTile() { Blocked = true };

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
			m_map = new Grid2D<Square>(MapWidth, MapHeight);

			InvalidateTiles();
		}

		protected override UIElement CreateTile()
		{
			return new MapControlTile();
		}

		protected override void UpdateTile(UIElement _tile, IntPoint ml)
		{
			MapControlTile tile = (MapControlTile)_tile;

			if (!m_map.Bounds.Contains(ml))
				return;

			if (m_realMap[ml].Blocked)
			{
				tile.Color = Colors.Blue;
			}
			else if (m_state > 0 && ml == m_from)
			{
				tile.Color = Colors.Green;
			}
			else if (m_state > 1 && ml == m_to)
			{
				tile.Color = Colors.Red;
			}
			else
			{
				Square s = m_map[ml];
				tile.Color = s.Color;
				tile.G = s.G;
				tile.H = s.H;
				tile.From = s.From;
			}

			tile.InvalidateVisual();
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			IntPoint ml = ScreenPointToMapLocation(e.GetPosition(this));

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
			else
			{
				var s = m_realMap[ml];
				s.Blocked = !s.Blocked;
				m_realMap[ml] = s;
			}

			InvalidateTiles();
		}

		bool LocValid(IntPoint p)
		{
			if (!m_realMap.Bounds.Contains(p))
				return false;

			if (m_realMap[p].Blocked)
				return false;

			return true;
		}


		void DoAStar(IntPoint src, IntPoint dst)
		{
			long startBytes, stopBytes;
			
			startBytes = System.GC.GetTotalMemory(true);
			IEnumerable<AStar.Node> list = AStar.FindPathNodes(src, dst, LocValid);
			stopBytes = System.GC.GetTotalMemory(true);
			GC.KeepAlive(list);
			Console.WriteLine("mem {0}", stopBytes - startBytes);
			
			DrawNodes(src, dst, list);
		}



		void DrawNodes(IntPoint src, IntPoint dst, IEnumerable<AStar.Node> list)
		{
			m_map[dst] = new Square() { Color = Colors.Red };

			foreach (var n in list)
			{
				if (n.Parent == null)
					continue;

				Direction from = (n.Parent.Loc - n.Loc).ToDirection();
				m_map[n.Loc] = new Square() { From = from, G = n.G, H = n.H };
			}

			{
				var n = list.First(x => x.Loc == dst);
				while (n.Parent != null)
				{
					Square s = m_map[n.Loc];
					s.Color = Colors.DarkGray;
					m_map[n.Loc] = s;
					n = n.Parent;
				}
			}
		}

		struct Square
		{
			public Color Color;
			public Direction From;
			public int G;
			public int H;
		}
	}

	class MapControlTile : UIElement
	{
		public MapControlTile()
		{
			this.IsHitTestVisible = false;
		}

		public Color Color;
		public int G;
		public int H;
		public Direction From;

		protected override void OnRender(DrawingContext dc)
		{
			dc.DrawRectangle(new SolidColorBrush(this.Color), new Pen(Brushes.Gray, 1), new Rect(this.RenderSize));

			if (From != Direction.None)
			{
				var iv = IntVector.FromDirection(From);
				iv *= (int)Math.Round(this.RenderSize.Width / 3);
				Point mp = new Point(this.RenderSize.Width / 2, this.RenderSize.Height / 2);
				dc.DrawEllipse(Brushes.White, null, mp, 3, 3);
				dc.DrawLine(new Pen(Brushes.White, 2), mp, mp + new Vector(iv.X, -iv.Y));
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
