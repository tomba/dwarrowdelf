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

			Action<ClientToolMode, string, Key, string> add = (i, n, k, g) => ToolDatas[i] = new ToolData(i, n, k, g);

			add(ClientToolMode.Info, "Info", Key.Escape, "");

			add(ClientToolMode.DesignationMine, "Mine", Key.M, "Designate");
			add(ClientToolMode.DesignationStairs, "Mine stairs", Key.S, "Designate");
			add(ClientToolMode.DesignationChannel, "Channel", Key.C, "Designate");
			add(ClientToolMode.DesignationFellTree, "Fell tree", Key.F, "Designate");
			add(ClientToolMode.DesignationRemove, "Remove", Key.R, "Designate");

			add(ClientToolMode.CreateStockpile, "Create stockpile", Key.P, "");
			add(ClientToolMode.InstallFurniture, "Install furniture", Key.I, "");

			add(ClientToolMode.CreateLiving, "Create living", Key.L, "");
			add(ClientToolMode.CreateItem, "Create item", Key.Z, "");
			add(ClientToolMode.SetTerrain, "Set terrain", Key.T, "");
			add(ClientToolMode.ConstructBuilding, "Create building", Key.B, "");

			add(ClientToolMode.ConstructWall, "Wall", Key.W, "Construct");
			add(ClientToolMode.ConstructFloor, "Floor", Key.O, "Construct");
			add(ClientToolMode.ConstructPavement, "Pavement", Key.A, "Construct");
			add(ClientToolMode.ConstructRemove, "Remove", Key.E, "Construct");
		}

		public void InstallKeyBindings(Window mw)
		{
			foreach (var kvp in ClientTools.ToolDatas)
			{
				mw.CommandBindings.Add(new CommandBinding(kvp.Value.Command, (s, e) => this.ToolMode = (ClientToolMode)e.Parameter));
				mw.InputBindings.Add(kvp.Value.InputBinding);
			}
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

	/* KeyGesture class doesn't like gestures without modifiers, so we need our own */
	public sealed class GameKeyGesture : InputGesture
	{
		public GameKeyGesture(Key key)
		{
			this.Key = key;
		}

		public Key Key { get; private set; }

		public override bool Matches(object targetElement, InputEventArgs inputEventArgs)
		{
			KeyEventArgs args = inputEventArgs as KeyEventArgs;
			return args != null && Keyboard.Modifiers == ModifierKeys.None && this.Key == args.Key;
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
		SetTerrain,
		CreateStockpile,
		CreateItem,
		CreateLiving,
		ConstructBuilding,
		InstallFurniture,
		ConstructWall,
		ConstructFloor,
		ConstructPavement,
		ConstructRemove,
	}

	sealed class ToolData
	{
		public ToolData(ClientToolMode mode, string name, Key key, string groupName)
		{
			this.Mode = mode;
			this.Name = name;
			this.ToolTip = String.Format("{0} ({1})", this.Name, key);
			this.GroupName = groupName;

			this.Command = new RoutedUICommand(name, name, typeof(MapToolBar));
			this.InputBinding = new InputBinding(this.Command, new GameKeyGesture(key));
			this.InputBinding.CommandParameter = mode;
		}

		public ClientToolMode Mode { get; private set; }
		public string Name { get; private set; }
		public string GroupName { get; private set; }
		public string ToolTip { get; private set; }
		public RoutedUICommand Command { get; private set; }
		public InputBinding InputBinding { get; private set; }
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
