using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Dwarrowdelf.Client.UI
{
	class ClientTools
	{
		public static readonly Dictionary<ClientToolMode, ToolData> ToolDatas;

		static ClientTools()
		{
			ToolDatas = new Dictionary<ClientToolMode, ToolData>();

			Action<ToolData> add = (td) => ToolDatas[td.Mode] = td;

			add(new ToolData(ClientToolMode.Info, "Info", "", Key.Escape));

			add(new ToolData(ClientToolMode.DesignationMine, "Mine", "Designate", Key.M));
			add(new ToolData(ClientToolMode.DesignationStairs, "Mine stairs", "Designate", Key.S));
			add(new ToolData(ClientToolMode.DesignationChannel, "Channel", "Designate", Key.C));
			add(new ToolData(ClientToolMode.DesignationFellTree, "Fell tree", "Designate", Key.F));
			add(new ToolData(ClientToolMode.DesignationRemove, "Remove", "Designate", Key.R));

			add(new ToolData(ClientToolMode.CreateStockpile, "Create stockpile", "", Key.P));
			add(new ToolData(ClientToolMode.InstallItem, "Install item", "", Key.I));
			add(new ToolData(ClientToolMode.BuildItem, "Build item", "", Key.B));

			add(new ToolData(ClientToolMode.ConstructWall, "Wall", "Construct", Key.W));
			add(new ToolData(ClientToolMode.ConstructFloor, "Floor", "Construct", Key.O));
			add(new ToolData(ClientToolMode.ConstructPavement, "Pavement", "Construct", Key.A));
			add(new ToolData(ClientToolMode.ConstructRemove, "Remove", "Construct", Key.E));

			add(new ToolData(ClientToolMode.CreateLiving, "Create living", "", Key.L, ModifierKeys.Control));
			add(new ToolData(ClientToolMode.CreateItem, "Create item", "", Key.I, ModifierKeys.Control));
			add(new ToolData(ClientToolMode.SetTerrain, "Set terrain", "", Key.T, ModifierKeys.Control));
		}

		public event Action<ClientToolMode> ToolModeChanged;

		ClientToolMode m_toolMode;

		public ClientToolMode ToolMode
		{
			get { return m_toolMode; }

			set
			{
				m_toolMode = value;
				if (this.ToolModeChanged != null)
					this.ToolModeChanged(value);
			}
		}
	}

	public enum ClientToolMode
	{
		None = 0,

		Info,

		DesignationRemove,
		DesignationMine,
		DesignationStairs,
		DesignationChannel,
		DesignationFellTree,

		CreateStockpile,
		InstallItem,
		BuildItem,

		ConstructWall,
		ConstructFloor,
		ConstructPavement,
		ConstructRemove,

		// Debug
		SetTerrain,
		CreateItem,
		CreateLiving,
	}

	sealed class ToolData
	{
		public ToolData(ClientToolMode mode, string name, string groupName, Key key, ModifierKeys modifiers = ModifierKeys.None)
		{
			this.Mode = mode;
			this.Name = name;
			this.GroupName = groupName;

			GameKeyGesture keyGesture;
			if (modifiers == ModifierKeys.None)
				keyGesture = new GameKeyGesture(key);
			else
				keyGesture = new GameKeyGesture(key, modifiers);

			this.Command = new RoutedUICommand(name, name, typeof(ClientTools),
				new InputGestureCollection() { keyGesture });

			this.ToolTip = String.Format("{0} ({1})",
				this.Name,
				keyGesture.GetDisplayStringForCulture(System.Globalization.CultureInfo.CurrentCulture));
		}

		public ClientToolMode Mode { get; private set; }
		public string Name { get; private set; }
		public string GroupName { get; private set; }
		public string ToolTip { get; private set; }
		public RoutedUICommand Command { get; private set; }
	}

	sealed class ClientToolModeToToolDataConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return null;

			var mode = (ClientToolMode)value;

			var data = ClientTools.ToolDatas[mode];

			return data;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
