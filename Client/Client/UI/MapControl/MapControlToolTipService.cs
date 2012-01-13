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

		ToolTip m_popup;
		TileToolTipControl m_content;

		public MapControlToolTipService(MapControl mapControl, TileView tileView)
		{
			m_mapControl = mapControl;
			m_hoverTileView = tileView;

			m_content = new TileToolTipControl();
			m_content.DataContext = m_hoverTileView;

			var popup = new ToolTip();
			popup.Content = m_content;
			popup.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
			popup.HorizontalOffset = 4;
			popup.PlacementTarget = m_mapControl;
			m_popup = popup;

			// Disable the animations, because we lose datacontext during fade-out animation.
			// We need to override the default values in the PlacementTarget control
			m_mapControl.Resources.Add(SystemParameters.ToolTipAnimationKey, false);
			m_mapControl.Resources.Add(SystemParameters.ToolTipFadeKey, false);
			m_mapControl.Resources.Add(SystemParameters.ToolTipPopupAnimationKey, PopupAnimation.None);
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
				case "AreaElement":
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
			var hasElement = m_hoverTileView.AreaElement != null;

			if (!hasObjects && !hasElement)
			{
				CloseToolTip();
				return;
			}

			m_content.objectBox.Visibility = hasObjects ? Visibility.Visible : Visibility.Collapsed;
			m_content.elementBox.Visibility = hasElement ? Visibility.Visible : Visibility.Collapsed;
			m_content.separator.Visibility = hasObjects && hasElement ? Visibility.Visible : Visibility.Collapsed;

			var ml = m_hoverTileView.Location;

			var rect = m_mapControl.MapRectToScreenPointRect(new IntRect(ml.ToIntPoint(), new IntSize2(1, 1)));

			m_popup.PlacementRectangle = rect;
			m_popup.IsOpen = true;
		}

		void CloseToolTip()
		{
			m_popup.IsOpen = false;
		}
	}
}
