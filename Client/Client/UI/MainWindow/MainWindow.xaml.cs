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
using Dwarrowdelf.Messages;
using Dwarrowdelf.Jobs;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace Dwarrowdelf.Client.UI
{
	sealed partial class MainWindow : Window, INotifyPropertyChanged
	{
		MovableObject m_followObject;
		LivingObject m_focusedObject;

		enum CloseStatus
		{
			None,
			ShuttingDown,
			Ready,
		}

		CloseStatus m_closeStatus;

		// Stores previous user values for setTerrainData
		SetTerrainData m_setTerrainData;

		MainWindowCommandHandler m_cmdHandler;

		public ClientTools ClientTools { get; private set; }

		public MainWindow()
		{
			InitializeComponent();

			this.ClientTools = new ClientTools();
			this.ClientTools.ToolModeChanged += MainWindowTools_ToolModeChanged;
			this.ClientTools.ToolMode = ClientToolMode.Info;

			this.MapControl.GotSelection += MapControl_GotSelection;

			// for some reason this prevents the changing of focus from mapcontrol with cursor keys
			KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.Once);

			mainWindowTools.SetClientTools(this.ClientTools);
		}

		void MainWindowTools_ToolModeChanged(ClientToolMode toolMode)
		{
			switch (toolMode)
			{
				case ClientToolMode.Info:
					this.MapControl.SelectionMode = MapSelectionMode.None;
					break;

				case ClientToolMode.CreateItem:
				case ClientToolMode.DesignationMine:
				case ClientToolMode.DesignationStairs:
				case ClientToolMode.DesignationChannel:
				case ClientToolMode.DesignationFellTree:
				case ClientToolMode.DesignationRemove:
				case ClientToolMode.SetTerrain:
				case ClientToolMode.CreateLiving:
					this.MapControl.SelectionMode = MapSelectionMode.Box;
					break;

				case ClientToolMode.CreateStockpile:
					this.MapControl.SelectionMode = MapSelectionMode.Rectangle;
					break;


				case ClientToolMode.ConstructWall:
				case ClientToolMode.ConstructFloor:
				case ClientToolMode.ConstructPavement:
				case ClientToolMode.ConstructRemove:
					this.MapControl.SelectionMode = MapSelectionMode.Rectangle;
					break;

				case ClientToolMode.InstallFurniture:
					this.MapControl.SelectionMode = MapSelectionMode.Point;
					break;

				default:
					throw new Exception();
			}
		}

		void MapControl_GotSelection(MapSelection selection)
		{
			var env = this.Map;

			switch (this.ClientTools.ToolMode)
			{
				case ClientToolMode.DesignationRemove:
					env.Designations.RemoveArea(selection.SelectionBox);
					break;

				case ClientToolMode.DesignationMine:
					env.Designations.AddArea(selection.SelectionBox, DesignationType.Mine);
					break;

				case ClientToolMode.DesignationStairs:
					env.Designations.AddArea(selection.SelectionBox, DesignationType.CreateStairs);
					break;

				case ClientToolMode.DesignationChannel:
					env.Designations.AddArea(selection.SelectionBox, DesignationType.Channel);
					break;

				case ClientToolMode.DesignationFellTree:
					env.Designations.AddArea(selection.SelectionBox, DesignationType.FellTree);
					break;

				case ClientToolMode.CreateStockpile:
					{
						var stockpile = new Stockpile(env, selection.SelectionIntRectZ);
						env.AddAreaElement(stockpile);

						var dlg = new ObjectEditDialog();
						dlg.DataContext = stockpile;
						dlg.Owner = this;
						dlg.Show();
					}
					break;

				case ClientToolMode.InstallFurniture:
					{
						var p = selection.SelectionPoint;

						var dlg = new InstallFurnitureDialog();
						dlg.SetContext(env, p);

						var res = dlg.ShowDialog();

						if (res == true)
						{
							var item = dlg.SelectedItem;

							if (item != null)
								env.InstallFurnitureManager.AddInstallJob(item, p);
						}
					}
					break;

				case ClientToolMode.SetTerrain:
					{
						var dialog = new SetTerrainDialog();
						dialog.Owner = this;
						if (m_setTerrainData != null)
							dialog.Data = m_setTerrainData;
						var res = dialog.ShowDialog();

						if (res == true)
						{
							var data = dialog.Data;
							m_setTerrainData = data;

							DebugScriptMessages.SendSetTerrains(dialog, map.Environment, selection.SelectionBox);
						}
					}
					break;

				case ClientToolMode.CreateItem:
					{
						var dialog = new CreateItemDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionBox);
						var res = dialog.ShowDialog();

						if (res == true)
							DebugScriptMessages.SendCreateItem(dialog);
					}
					break;

				case ClientToolMode.CreateLiving:
					{
						var dialog = new CreateLivingDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionIntRectZ);
						var res = dialog.ShowDialog();

						if (res == true)
							DebugScriptMessages.SendCreateLiving(dialog);
					}
					break;

				case ClientToolMode.ConstructRemove:
					env.ConstructManager.RemoveArea(selection.SelectionIntRectZ);
					break;

				case ClientToolMode.ConstructWall:
				case ClientToolMode.ConstructFloor:
				case ClientToolMode.ConstructPavement:
					{
						ConstructMode mode;

						switch (this.ClientTools.ToolMode)
						{
							case ClientToolMode.ConstructWall:
								mode = ConstructMode.Wall;
								break;

							case ClientToolMode.ConstructFloor:
								mode = ConstructMode.Floor;
								break;

							case ClientToolMode.ConstructPavement:
								mode = ConstructMode.Pavement;
								break;

							default:
								throw new Exception();
						}

						var dialog = new ConstructDialog();
						dialog.Owner = this;
						dialog.ConstructMode = mode;
						var res = dialog.ShowDialog();

						if (res == true)
						{
							var area = selection.SelectionIntRectZ;
							var filter = dialog.GetItemFilter();

							env.ConstructManager.AddConstructJob(mode, area, filter);
						}
					}
					break;

				default:
					throw new Exception();
			}

			this.MapControl.Selection = new MapSelection();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			m_cmdHandler = new MainWindowCommandHandler(this);

			AddHandler(UI.MapControl.MouseClickedEvent, new MouseButtonEventHandler(OnMouseClicked));

			var dpd = DependencyPropertyDescriptor.FromProperty(GameData.WorldProperty, typeof(GameData));
			dpd.AddValueChanged(GameData.Data, OnWorldChanged);
		}

		void OnWorldChanged(object sender, EventArgs ev)
		{
			var world = GameData.Data.World;

			this.CommandBindings.Clear();

			if (world == null)
				return;

			m_cmdHandler.AddCommandBindings(world.GameMode);
		}

		void OnMouseClicked(object sender, MouseButtonEventArgs ev)
		{
			if (this.ClientTools.ToolMode == ClientToolMode.Info)
			{
				var env = this.MapControl.Environment;

				if (env == null)
					return;

				var ml = this.MapControl.ScreenPointToMapLocation(ev.GetPosition(this.MapControl));

				var obs = new List<object>();

				var elem = env.GetElementAt(ml);
				if (elem != null)
					obs.Add(env.GetElementAt(ml));

				obs.AddRange(env.GetContents(ml));

				if (obs.Count == 1)
				{
					object ob = obs[0];
					ShowObjectInDialog(ob);
				}
				else if (obs.Count > 0)
				{
					var ctxMenu = (ContextMenu)this.FindResource("objectSelectorContextMenu");

					ctxMenu.ItemsSource = obs;
					ctxMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
					ctxMenu.PlacementTarget = this.MapControl;
					var rect = this.MapControl.MapRectToScreenPointRect(new IntGrid2(ml.ToIntPoint(), new IntSize2(1, 1)));
					ctxMenu.PlacementRectangle = rect;

					ctxMenu.IsOpen = true;
				}

				ev.Handled = true;
			}
		}

		void OnObjectSelectContextMenuClick(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)e.OriginalSource;
			var ob = item.Header;
			ShowObjectInDialog(ob);
		}

		void ShowObjectInDialog(object ob)
		{
			var dlg = new ObjectEditDialog();
			dlg.DataContext = ob;
			dlg.Owner = this;
			dlg.Show();
		}

		protected override async void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var p = (Win32.WindowPlacement)Properties.Settings.Default.MainWindowPlacement;
			if (p != null)
				Win32.Helpers.LoadWindowPlacement(this, p);

			if (ClientConfig.AutoConnect)
			{
				try
				{
					await GameData.Data.ConnectManager.StartServerAsync();
				}
				catch (Exception exc)
				{
					MessageBox.Show(this, exc.ToString(), "Failed to start server");
					return;
				}

				bool stopServer = false;

				try
				{
					await GameData.Data.ConnectManager.ConnectPlayerAsync();
				}
				catch (Exception exc)
				{
					MessageBox.Show(this, exc.ToString(), "Connect failed");
					stopServer = true;
				}

				if (stopServer)
					await GameData.Data.ConnectManager.StopServerAsync();
			}
		}

		public MasterMapControl MapControl { get { return map; } }

		public GameData Data { get { return GameData.Data; } }

		protected override async void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			switch (m_closeStatus)
			{
				case CloseStatus.None:
					m_closeStatus = CloseStatus.ShuttingDown;

					e.Cancel = true;

					var p = Win32.Helpers.SaveWindowPlacement(this);
					Properties.Settings.Default.MainWindowPlacement = p;
					Properties.Settings.Default.Save();

					try
					{
						await GameData.Data.ConnectManager.DisconnectAsync();
						await GameData.Data.ConnectManager.StopServerAsync();
					}
					catch (Exception exc)
					{
						MessageBox.Show(exc.ToString(), "Error closing down");
					}

					m_closeStatus = CloseStatus.Ready;
					await this.Dispatcher.InvokeAsync(Close);

					break;

				case CloseStatus.ShuttingDown:
					e.Cancel = true;
					break;

				case CloseStatus.Ready:
					break;
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			map.Dispose();

			base.OnClosed(e);
		}

		private void FilterItems(object sender, FilterEventArgs e)
		{
			if (e.Item is ItemObject)
				e.Accepted = true;
			else
				e.Accepted = false;
		}

		private void FilterLivings(object sender, FilterEventArgs e)
		{
			if (e.Item is LivingObject)
				e.Accepted = true;
			else
				e.Accepted = false;
		}

		public LivingObject FocusedObject
		{
			get { return m_focusedObject; }

			set
			{
				if (m_focusedObject == value)
					return;

				m_focusedObject = value;

				if (value != null)
				{
					value.ObjectMoved += OnFocusedControllableMoved;
					this.MapControl.FocusedTileView.SetTarget(value.Environment, value.Location);
				}
				else
				{
					this.MapControl.FocusedTileView.ClearTarget();
				}

				// always follow the focused ob for now
				this.FollowObject = value;

				Notify("FocusedObject");
			}
		}

		void OnFocusedControllableMoved(MovableObject ob, ContainerObject dst, IntPoint3 loc)
		{
			this.MapControl.FocusedTileView.SetTarget(ob.Environment, ob.Location);
		}


		public MovableObject FollowObject
		{
			get { return m_followObject; }

			set
			{
				if (m_followObject == value)
					return;

				if (m_followObject != null)
					m_followObject.ObjectMoved -= FollowedObjectMoved;

				m_followObject = value;

				if (m_followObject != null)
				{
					m_followObject.ObjectMoved += FollowedObjectMoved;
					map.ScrollTo(m_followObject.Environment, m_followObject.Location);
				}

				Notify("FollowObject");
			}
		}

		void FollowedObjectMoved(MovableObject ob, ContainerObject dst, IntPoint3 loc)
		{
			EnvironmentObject env = dst as EnvironmentObject;
			if (env != null)
			{
				map.CenterPos = new Point(loc.X, loc.Y);
				map.Z = loc.Z;
			}
		}

		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
				map.Focus();

			base.OnPreviewKeyDown(e);
		}

		internal EnvironmentObject Map
		{
			get { return map.Environment; }
			set { map.Environment = value; }
		}

		private void MenuItem_Click_JobTreeView(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			string tag = (string)item.Tag;
			IJob job = (IJob)jobTreeView.SelectedValue;

			if (job == null)
				return;

			switch (tag)
			{
				case "Abort":
					job.Abort();
					break;

				default:
					throw new Exception();
			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		void Notify(string info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}

		private void ObjectsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count == 0)
				return;

			if (e.AddedItems.Count != 1)
				throw new Exception();

			var movable = e.AddedItems[0] as MovableObject;

			this.FollowObject = null;

			if (movable == null || movable.Environment == null)
				return;

			map.ScrollTo(movable.Environment, movable.Location);
		}

		private void MessageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (e.AddedItems.Count != 1)
				return;

			var msg = (GameEvent)e.AddedItems[0];

			if (msg.Environment == null)
				return;

			map.ScrollTo(msg.Environment, msg.Location);
		}
	}
}
