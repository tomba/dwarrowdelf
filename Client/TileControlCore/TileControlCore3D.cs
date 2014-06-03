using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D; // XXX remove when Point3D not used

namespace Dwarrowdelf.Client.TileControl
{
	public abstract class TileControlCore3D : TileControlCore
	{
		/// <summary>
		/// Offset between screen based tiles and content based tiles
		/// </summary>
		Vector m_contentOffset;

		public event Action<object, DoublePoint3, IntVector3> ScreenCenterPosChanged;

		protected TileControlCore3D()
		{
			base.TileLayoutChanged += TileControlCore3D_TileLayoutChanged;
		}

		void TileControlCore3D_TileLayoutChanged(IntSize2 gridSize, double tileSize)
		{
			var iscp = this.ScreenCenterPos.ToIntPoint3();

			m_contentOffset = new Vector(iscp.X - gridSize.Width / 2, iscp.Y - gridSize.Height / 2);
		}

		public double ScreenZ
		{
			get { return this.ScreenCenterPos.Z; }
		}

		public DoublePoint3 ScreenCenterPos
		{
			get { var p = (Point3D)GetValue(ScreenCenterPosProperty); return new DoublePoint3(p.X, p.Y, p.Z); }
			set { var p = new Point3D(value.X, value.Y, value.Z); SetValue(ScreenCenterPosProperty, p); }
		}

		public static readonly DependencyProperty ScreenCenterPosProperty =
			DependencyProperty.Register("ScreenCenterPos", typeof(Point3D), typeof(TileControlCore3D),
			new PropertyMetadata(new Point3D(), OnScreenCenterPosChanged));

		static void OnScreenCenterPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var mc = (TileControlCore3D)d;

			mc.HandleScreenCenterPosChange(e);
		}

		void HandleScreenCenterPosChange(DependencyPropertyChangedEventArgs e)
		{
			var _oldVal = (Point3D)e.OldValue;
			var oldscp = new DoublePoint3(_oldVal.X, _oldVal.Y, _oldVal.Z);
			var ioldscp = oldscp.ToIntPoint3();

			var _newVal = (Point3D)e.NewValue;
			var scp = new DoublePoint3(_newVal.X, _newVal.Y, _newVal.Z);
			var iscp = scp.ToIntPoint3();

			var diff = iscp - ioldscp;

			if (diff.IsNull == false)
				base.InvalidateTileData();

			m_contentOffset = new Vector(iscp.X - this.GridSize.Width / 2,
				iscp.Y - this.GridSize.Height / 2);

			this.TileOffset = new Vector(scp.X - iscp.X, scp.Y - iscp.Y);

			if (this.ScreenCenterPosChanged != null)
				this.ScreenCenterPosChanged(this, scp, diff);
		}

		public Point RenderTileToScreen(Point st)
		{
			return st + m_contentOffset;
		}

		public Point ScreenToRenderTile(Point mt)
		{
			return mt - m_contentOffset;
		}

		public Point RenderPointToScreen(Point p)
		{
			var st = RenderPointToRenderTile(p);
			return RenderTileToScreen(st);
		}

		public Point ScreenToRenderPoint(Point mt)
		{
			var st = ScreenToRenderTile(mt);
			return RenderTileToRenderPoint(st);
		}

		public DoublePoint3 RenderTileToScreen3(Point st)
		{
			var p = RenderTileToScreen(st);
			return new DoublePoint3(p.X, p.Y, this.ScreenZ);
		}
	}
}
