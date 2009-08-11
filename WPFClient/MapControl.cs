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

namespace MyGame
{
	class MapControl : MapControlBase, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		SymbolBitmapCache m_bitmapCache;

		ClientGameObject m_followObject;
		MapLevel m_mapLevel;

		public MapControl()
		{
			m_bitmapCache = new SymbolBitmapCache();
			m_bitmapCache.SymbolDrawings = GameData.Data.SymbolDrawings.Drawings;

			this.Focusable = true;

			base.SelectionChanged += OnSelectionChanged;
		}

		protected override void OnTileSizeChanged(double newSize)
		{
			m_bitmapCache.TileSize = newSize;
		}

		protected override UIElement CreateTile()
		{
			return new MapControlTile();
		}

		protected override void UpdateTile(UIElement _tile, IntPoint ml)
		{
			BitmapSource bmp = null;
			MapControlTile tile = (MapControlTile)_tile;
			bool lit = false;

			if (m_mapLevel == null) // || !m_mapLevel.Bounds.Contains(ml))
			{
				tile.Bitmap = null;
				tile.ObjectBitmap = null;
				return;
			}

			if (m_followObject != null)
			{
				if (m_mapLevel.GetTerrainType(ml) == 0)
				{
					// unknown locations always unlit
					lit = false;
				}
				else if (m_followObject.Location == ml)
				{
					// current location always lit
					lit = true;
				}
				else if (Math.Abs(m_followObject.Location.X - ml.X) > m_followObject.VisionRange ||
					Math.Abs(m_followObject.Location.Y - ml.Y) > m_followObject.VisionRange)
				{
					// out of vision range
					lit = false;
				}
				else if (m_followObject.VisionMap[ml - (IntVector)m_followObject.Location] == false)
				{
					// can't see
					lit = false;
				}
				else
				{
					// else in range, not blocked
					lit = true;
				}
			}

			bmp = GetBitmap(ml, lit);
			tile.Bitmap = bmp;

			if(GameData.Data.DisableLOS)
				lit = true; // lit always so we see what server sends

			if (lit)
				bmp = GetObjectBitmap(ml, lit);
			else
				bmp = null;
			tile.ObjectBitmap = bmp;
		}

		BitmapSource GetBitmap(IntPoint ml, bool lit)
		{
			int terrainID = this.Map.GetTerrainType(ml);
			return m_bitmapCache.GetBitmap(terrainID, !lit);
		}

		BitmapSource GetObjectBitmap(IntPoint ml, bool lit)
		{
			IList<ClientGameObject> obs = this.Map.GetContents(ml);
			if (obs != null && obs.Count > 0)
			{
				int id = obs[0].SymbolID;
				return m_bitmapCache.GetBitmap(id, !lit);
			}
			else
				return null;
		}

		internal MapLevel Map
		{
			get { return m_mapLevel; }

			set
			{
				if (m_mapLevel != null)
					m_mapLevel.MapChanged -= MapChangedCallback;
				m_mapLevel = value;
				if (m_mapLevel != null)
					m_mapLevel.MapChanged += MapChangedCallback;

				InvalidateTiles();
			}
		}

		void MapChangedCallback(IntPoint l)
		{
			InvalidateTiles();
		}

		public ClientGameObject FollowObject
		{
			get
			{
				return m_followObject;
			}

			set
			{
				if (m_followObject != null)
					m_followObject.ObjectMoved -= FollowedObjectMoved;
				m_followObject = value;
				m_followObject.ObjectMoved += FollowedObjectMoved;

				if (m_followObject.Environment != null)
					FollowedObjectMoved(m_followObject.Environment, m_followObject.Location);

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("FollowObject"));
			}

		}

		void FollowedObjectMoved(MapLevel e, IntPoint l)
		{
			if (e != m_mapLevel)
			{
				this.Map = m_mapLevel;
//				m_center = new Location(-1, -1);
			}

			int xd = this.Columns / 2;
			int yd = this.Rows / 2;
			int x = l.X - xd;
			int y = l.Y - yd;
			//Location newPos = new Location(((x+xd/2) / xd) * xd, ((y+yd/2) / yd) * yd);
			IntPoint newPos = new IntPoint(x, y);

			this.Pos = newPos;
		}

		TileInfo m_tileInfo;
		public TileInfo SelectedTile
		{
			get { return m_tileInfo; }
			set
			{
				if (m_tileInfo != null)
					m_tileInfo.StopObserve();
				m_tileInfo = value;
				if (m_tileInfo != null)
					m_tileInfo.StartObserve();
				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("SelectedTile"));
			}
		}

		void OnSelectionChanged()
		{
			IntRect sel = this.SelectionRect;

			if (sel.Width != 1 || sel.Height != 1)
			{
				this.SelectedTile = null;
				return;
			}

			this.SelectedTile = new TileInfo(this.m_mapLevel, sel.TopLeft);
		}
	}

	class TileInfo : INotifyPropertyChanged
	{
		MapLevel m_mapLevel;
		IntPoint m_location;
		public TileInfo(MapLevel mapLevel, IntPoint location)
		{
			m_mapLevel = mapLevel;
			m_location = location;
		}

		public void StartObserve()
		{
			m_mapLevel.MapChanged += MapChanged;
		}

		public void StopObserve()
		{
			m_mapLevel.MapChanged -= MapChanged;
		}

		void MapChanged(IntPoint l)
		{
			if (l == m_location)
			{
				Notify("TerrainType");
				Notify("Objects");
			}
		}

		public IntPoint Location
		{
			get { return m_location; }
		}

		public int TerrainType
		{
			get { return m_mapLevel.GetTerrainType(m_location); }
		}

		public IList<ClientGameObject> Objects
		{
			get { return m_mapLevel.GetContents(m_location); }
		}

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	public class MapControlTile : UIElement
	{
		BitmapSource m_bmp;
		BitmapSource m_objectBmp;

		public MapControlTile()
		{
			this.IsHitTestVisible = false;
		}

		public BitmapSource Bitmap
		{
			get { return m_bmp; }

			set
			{
				if (m_bmp != value)
				{
					m_bmp = value;
					this.InvalidateVisual();
				}
			}
		}

		public BitmapSource ObjectBitmap
		{
			get { return m_objectBmp; }

			set
			{
				if (m_objectBmp != value)
				{
					m_objectBmp = value;
					this.InvalidateVisual();
				}
			}
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			if (m_bmp != null)
				drawingContext.DrawImage(m_bmp, new Rect(this.RenderSize));

			if (m_objectBmp != null)
				drawingContext.DrawImage(m_objectBmp, new Rect(this.RenderSize));
		}
	}
}
