using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace MyGame
{
	class MapControl : MapControlBase, INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		SymbolBitmapCache m_bitmapCache;

		Environment m_env;
		public int Z { get; set; }

		public MapControl()
		{
			m_bitmapCache = new SymbolBitmapCache();
			m_bitmapCache.SymbolDrawings = GameData.Data.SymbolDrawings;

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

		protected override void UpdateTile(UIElement _tile, IntPoint _ml)
		{
			BitmapSource bmp = null;
			MapControlTile tile = (MapControlTile)_tile;
			bool lit = false;
			IntPoint3D ml = new IntPoint3D(_ml.X, _ml.Y, this.Z);

			if (this.Environment == null)
			{
				tile.Bitmap = null;
				tile.ObjectBitmap = null;
				return;
			}

			lit = TileVisible(ml);

			bmp = GetBitmap(ml, lit);
			tile.Bitmap = bmp;

			if (GameData.Data.DisableLOS)
				lit = true; // lit always so we see what server sends

			if (lit)
				bmp = GetObjectBitmap(ml, lit);
			else
				bmp = null;
			tile.ObjectBitmap = bmp;
		}

		bool TileVisible(IntPoint3D ml)
		{
			if (this.Environment.VisibilityMode == VisibilityMode.AllVisible)
				return true;

			if (this.Environment.GetTerrainID(ml) == 0)
				return false;

			var controllables = GameData.Data.Controllables;

			if (this.Environment.VisibilityMode == VisibilityMode.LOS)
			{
				foreach (var l in controllables)
				{
					if (l.Environment != this.Environment)
						continue;

					IntPoint vp = new IntPoint(ml.X - l.Location.X, ml.Y - l.Location.Y);

					if (Math.Abs(vp.X) <= l.VisionRange && Math.Abs(vp.Y) <= l.VisionRange &&
						l.VisionMap[vp] == true)
						return true;
				}
			}
			else if (this.Environment.VisibilityMode == VisibilityMode.SimpleFOV)
			{
				foreach (var l in controllables)
				{
					if (l.Environment != this.Environment)
						continue;

					IntPoint vp = new IntPoint(ml.X - l.Location.X, ml.Y - l.Location.Y);

					if (Math.Abs(vp.X) <= l.VisionRange && Math.Abs(vp.Y) <= l.VisionRange)
						return true;
				}
			}
			else
			{
				throw new Exception();
			}

			return false;
		}

		BitmapSource GetBitmap(IntPoint3D ml, bool lit)
		{
			int terrainID = this.Environment.GetTerrainID(ml);
			int id = this.Environment.World.AreaData.Terrains[terrainID].SymbolID;
			Color c = Colors.Black;
			return m_bitmapCache.GetBitmap(id, c, !lit);
		}

		BitmapSource GetObjectBitmap(IntPoint3D ml, bool lit)
		{
			IList<ClientGameObject> obs = this.Environment.GetContents(ml);
			if (obs != null && obs.Count > 0)
			{
				int id = obs[0].SymbolID;
				Color c = obs[0].Color;
				return m_bitmapCache.GetBitmap(id, c, !lit);
			}
			else
				return null;
		}

		internal Environment Environment
		{
			get { return m_env; }

			set
			{
				if (m_env == value)
					return;

				if (m_env != null)
					m_env.MapChanged -= MapChangedCallback;
				m_env = value;
				if (m_env != null)
					m_env.MapChanged += MapChangedCallback;

				InvalidateTiles();
			}
		}

		void MapChangedCallback(IntPoint3D l)
		{
			InvalidateTiles();
		}

		TileInfo m_selectedTileInfo;
		public TileInfo SelectedTileInfo
		{
			get { return m_selectedTileInfo; }
			set
			{
				if (m_selectedTileInfo == value)
					return;

				m_selectedTileInfo = value;

				if (PropertyChanged != null)
					PropertyChanged(this, new PropertyChangedEventArgs("SelectedTileInfo"));
			}
		}

		void OnSelectionChanged()
		{
			IntRect sel = this.SelectionRect;

			if (sel.Width != 1 || sel.Height != 1)
			{
				if (this.SelectedTileInfo != null)
					this.SelectedTileInfo.StopObserve();
				this.SelectedTileInfo = null;
				return;
			}

			if (this.SelectedTileInfo == null)
			{
				this.SelectedTileInfo = new TileInfo(this.Environment, new IntPoint3D(sel.TopLeft, this.Z));
			}
			else
			{
				this.SelectedTileInfo.Environment = this.Environment;
				this.SelectedTileInfo.Location = new IntPoint3D(sel.TopLeft, this.Z);
			}
		}
	}

	class TileInfo : INotifyPropertyChanged
	{
		Environment m_env;
		IntPoint3D m_location;

		public TileInfo()
		{
		}

		public TileInfo(Environment mapLevel, IntPoint3D location)
		{
			m_env = mapLevel;
			m_location = location;
			if (m_env != null)
				m_env.MapChanged += MapChanged;
		}

		public void StopObserve()
		{
			if (m_env != null)
				m_env.MapChanged -= MapChanged;
		}

		void MapChanged(IntPoint3D l)
		{
			if (l == m_location)
			{
				Notify("TerrainType");
				Notify("Objects");
			}
		}

		public Environment Environment
		{
			get { return m_env; }
			set
			{
				if (m_env != null)
					m_env.MapChanged -= MapChanged;

				m_env = value;

				if (m_env != null)
					m_env.MapChanged += MapChanged;

				Notify("Environment");
				Notify("TerrainType");
				Notify("Objects");
			}
		}

		public IntPoint3D Location
		{
			get { return m_location; }
			set
			{
				m_location = value;
				Notify("Location");
				Notify("TerrainType");
				Notify("Objects");
			}
		}

		public int TerrainType
		{
			get
			{
				if (m_env == null)
					return 0;
				return m_env.GetTerrainID(m_location);
			}
		}

		public IList<ClientGameObject> Objects
		{
			get
			{
				if (m_env == null)
					return null;
				return m_env.GetContents(m_location);
			}
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

	class MapControlTile : UIElement
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
