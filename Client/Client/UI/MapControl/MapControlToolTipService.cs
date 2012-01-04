using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Controls.Primitives;
using System.Collections.Specialized;

namespace Dwarrowdelf.Client.UI
{
	sealed class MapControlToolTipService
	{
		MapControl m_mapControl;
		TileView m_hoverTileView;

		bool m_isToolTipEnabled;

		Popup m_popup;
		TileToolTipControl m_content;

		public MapControlToolTipService(MapControl mapControl, TileView tileView)
		{
			m_mapControl = mapControl;
			m_hoverTileView = tileView;

			m_content = new TileToolTipControl();
			m_content.DataContext = m_hoverTileView;

			var popup = new Popup();
			popup.Child = m_content;
			popup.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
			popup.HorizontalOffset = 4;
			popup.PlacementTarget = m_mapControl;
			popup.AllowsTransparency = true;
			m_popup = popup;
		}

		public bool IsToolTipEnabled
		{
			get { return m_isToolTipEnabled; }

			set
			{
				if (value == m_isToolTipEnabled)
					return;

				if (value == true)
				{
					m_hoverTileView.PropertyChanged += OnHoverTileView_PropertyChanged;
					m_hoverTileView.Objects.CollectionChanged += OnHoverTileView_ObjectCollectionChanged;
				}
				else
				{
					m_hoverTileView.PropertyChanged -= OnHoverTileView_PropertyChanged;
					m_hoverTileView.Objects.CollectionChanged -= OnHoverTileView_ObjectCollectionChanged;

					CloseToolTip();
				}

				m_isToolTipEnabled = value;
			}
		}

		void OnHoverTileView_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case "Location":
				case "MapElement":
				case "IsEnabled":
					UpdateToolTip();
					break;
			}
		}

		void OnHoverTileView_ObjectCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			UpdateToolTip();
		}

		void UpdateToolTip()
		{
			if (m_hoverTileView.IsEnabled == false)
			{
				CloseToolTip();
				return;
			}

			bool hasObjects = m_hoverTileView.Objects.Count > 0;
			var hasElement = m_hoverTileView.MapElement != null;

			if (!hasObjects && !hasElement)
			{
				CloseToolTip();
				return;
			}

			m_content.objectBox.Visibility = hasObjects ? Visibility.Visible : Visibility.Collapsed;
			m_content.elementBox.Visibility = hasElement ? Visibility.Visible : Visibility.Collapsed;
			m_content.separator.Visibility = hasObjects && hasElement ? Visibility.Visible : Visibility.Collapsed;

			var ml = m_hoverTileView.Location;

			var rect = m_mapControl.MapRectToScreenPointRect(new IntRect(ml.ToIntPoint(), new IntSize(1, 1)));

			m_popup.PlacementRectangle = rect;
			m_popup.IsOpen = true;
		}

		void CloseToolTip()
		{
			m_popup.IsOpen = false;
		}
	}
}
