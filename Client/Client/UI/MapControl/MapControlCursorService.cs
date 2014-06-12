using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.UI
{
	sealed class MapControlCursorService
	{
		MasterMapControl m_mapControl;
		Canvas m_canvas;

		Rectangle m_cursorRect;

		public IntPoint3 Position { get; private set; }

		public MapControlCursorService(MasterMapControl mapControl, Canvas canvas)
		{
			m_canvas = canvas;
			m_mapControl = mapControl;

			m_cursorRect = new Rectangle();
			m_cursorRect.Visibility = Visibility.Hidden;
			m_cursorRect.Stroke = Brushes.Yellow;
			m_cursorRect.Stroke.Freeze();
			m_cursorRect.IsHitTestVisible = false;
			m_canvas.Children.Add(m_cursorRect);
		}

		bool m_isEnabled;

		public bool IsEnabled
		{
			get { return m_isEnabled; }
			set
			{
				if (value == m_isEnabled)
					return;

				if (m_isEnabled)
				{
					m_mapControl.TileLayoutChanged -= OnTileLayoutChanged;
					m_mapControl.ScreenCenterPosChanged -= OnScreenCenterPosChanged;
					m_mapControl.KeyDown -= OnKeyDown;
				}

				m_isEnabled = value;

				if (m_isEnabled)
				{
					this.Position = m_mapControl.MapCenterPos.ToIntPoint3();

					m_mapControl.TileLayoutChanged += OnTileLayoutChanged;
					m_mapControl.ScreenCenterPosChanged += OnScreenCenterPosChanged;
					m_mapControl.KeyDown += OnKeyDown;
				}
				else
				{
					this.Position = new IntPoint3();
				}

				UpdateCursorRectangle();
			}
		}

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			var key = e.Key;

			if (KeyHelpers.KeyIsDir(key))
			{
				m_mapControl.ScrollStop();
				var dir = KeyHelpers.KeyToDir(key);
				this.Position += dir;
				m_mapControl.KeepOnScreen(this.Position);
				UpdateCursorRectangle();
				e.Handled = true;
			}
		}

		void OnScreenCenterPosChanged(object control, DoublePoint3 centerPos, IntVector3 diff)
		{
			UpdateCursorRectangle();
		}

		void OnTileLayoutChanged(IntSize2 gridSize, double tileSize)
		{
			UpdateCursorRectangle();
		}

		void UpdateCursorRectangle()
		{
			if (!this.IsEnabled)
			{
				m_cursorRect.Visibility = Visibility.Hidden;
				return;
			}

			var thickness = Math.Max(2, m_mapControl.TileSize / 8);

			var p = m_mapControl.MapLocationToScreenTile(this.Position);
			p -= new Vector(0.5, 0.5);
			p = m_mapControl.ScreenToRenderPoint(p);
			p -= new Vector(thickness, thickness);

			Canvas.SetLeft(m_cursorRect, p.X);
			Canvas.SetTop(m_cursorRect, p.Y);
			m_cursorRect.Width = m_mapControl.TileSize + thickness * 2;
			m_cursorRect.Height = m_mapControl.TileSize + thickness * 2;

			m_cursorRect.StrokeThickness = thickness;

			m_cursorRect.Visibility = Visibility.Visible;
		}
	}
}
