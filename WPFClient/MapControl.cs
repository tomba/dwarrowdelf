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
		Environment m_env;

		public MapControl()
		{
			m_bitmapCache = new SymbolBitmapCache();
			m_bitmapCache.SymbolDrawings = GameData.Data.SymbolDrawings;

			this.Focusable = true;

			base.SelectionChanged += OnSelectionChanged;

			var dpd = DependencyPropertyDescriptor.FromProperty(MapControlBase.TileSizeProperty,
				typeof(MapControlBase));
			dpd.AddValueChanged(this, OnTileSizeChanged);
		}

		void OnTileSizeChanged(object ob, EventArgs e)
		{
			m_bitmapCache.TileSize = this.TileSize;
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

			if (m_env == null) // || !m_mapLevel.Bounds.Contains(ml))
			{
				tile.Bitmap = null;
				tile.ObjectBitmap = null;
				return;
			}

			if (m_env.VisibilityMode == VisibilityMode.AllVisible)
			{
				lit = true;
			}
			else if (m_followObject != null)
			{
				if (m_env.GetTerrainID(ml) == 0)
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
				else if (m_env.VisibilityMode == VisibilityMode.LOS &&
					m_followObject.VisionMap[ml - (IntVector)m_followObject.Location] == false)
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
			int terrainID = this.Map.GetTerrainID(ml);
			int id = this.Map.World.AreaData.Terrains[terrainID].SymbolID;
			Color c = Colors.Black;
			return m_bitmapCache.GetBitmap(id, c, !lit);
		}

		BitmapSource GetObjectBitmap(IntPoint ml, bool lit)
		{
			IList<ClientGameObject> obs = this.Map.GetContents(ml);
			if (obs != null && obs.Count > 0)
			{
				int id = obs[0].SymbolID;
				Color c = obs[0].Color;
				return m_bitmapCache.GetBitmap(id, c, !lit);
			}
			else
				return null;
		}

		internal Environment Map
		{
			get { return m_env; }

			set
			{
				if (m_env != null)
					m_env.MapChanged -= MapChangedCallback;
				m_env = value;
				if (m_env != null)
					m_env.MapChanged += MapChangedCallback;

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

		void FollowedObjectMoved(ClientGameObject e, IntPoint l)
		{
			Environment env = e as Environment;

			if (env != m_env)
			{
				this.Map = env;
//				m_center = new Location(-1, -1);
			}

			int xd = this.Columns / 2;
			int yd = this.Rows / 2;
			int x = l.X;
			int y = l.Y;
			IntPoint newPos = new IntPoint(((x + xd / 2) / xd) * xd, ((y + yd / 2) / yd) * yd);

			this.CenterPos = newPos;
		}

		TileInfo m_tileInfo;
		public TileInfo SelectedTileInfo
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
					PropertyChanged(this, new PropertyChangedEventArgs("SelectedTileInfo"));
			}
		}

		void OnSelectionChanged()
		{
			IntRect sel = this.SelectionRect;

			if (sel.Width != 1 || sel.Height != 1)
			{
				this.SelectedTileInfo = null;
				return;
			}

			this.SelectedTileInfo = new TileInfo(this.m_env, sel.TopLeft);
		}
	}

	class TileInfo : INotifyPropertyChanged
	{
		Environment m_env;
		IntPoint m_location;
		public TileInfo(Environment mapLevel, IntPoint location)
		{
			m_env = mapLevel;
			m_location = location;
		}

		public void StartObserve()
		{
			if (m_env != null)
				m_env.MapChanged += MapChanged;
		}

		public void StopObserve()
		{
			if (m_env != null)
				m_env.MapChanged -= MapChanged;
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
			get { return m_env.GetTerrainID(m_location); }
		}

		public IList<ClientGameObject> Objects
		{
			get { return m_env.GetContents(m_location); }
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
		public MapControlTile()
		{
			this.IsHitTestVisible = false;
		}

		public static readonly DependencyProperty BitmapProperty = DependencyProperty.Register(
			"Bitmap", typeof(BitmapSource), typeof(MapControlTile),
			new PropertyMetadata(null, ValueChangedCallback));

		public BitmapSource Bitmap
		{
			get { return (BitmapSource)GetValue(BitmapProperty); }
			set { SetValue(BitmapProperty, value); }
		}

		public static readonly DependencyProperty ObjectBitmapProperty = DependencyProperty.Register(
			"ObjectBitmap", typeof(BitmapSource), typeof(MapControlTile),
			new PropertyMetadata(null, ValueChangedCallback));

		public BitmapSource ObjectBitmap
		{
			get { return (BitmapSource)GetValue(ObjectBitmapProperty); }
			set { SetValue(ObjectBitmapProperty, value); }
		}

		static void ValueChangedCallback(DependencyObject ob, DependencyPropertyChangedEventArgs e)
		{
			((MapControlTile)ob).InvalidateVisual();
		}
		
		protected override void OnRender(DrawingContext drawingContext)
		{
			if (this.Bitmap != null)
				drawingContext.DrawImage(this.Bitmap, new Rect(this.RenderSize));

			if (this.ObjectBitmap != null)
				drawingContext.DrawImage(this.ObjectBitmap, new Rect(this.RenderSize));
		}
	}
}
