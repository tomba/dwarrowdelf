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

		public MasterMapControl MapControl { get { return map; } }

		public GameData Data { get { return GameData.Data; } }

		public MainWindow()
		{
			this.Initialized += MainWindow_Initialized;
			this.SourceInitialized += MainWindow_SourceInitialized;
			this.Closing += MainWindow_Closing;
			this.Closed += MainWindow_Closed;

			InitializeComponent();
		}

		void MainWindow_Initialized(object sender, EventArgs e)
		{
			m_cmdHandler = new MainWindowCommandHandler(this);

			AddHandler(UI.MapControl.MouseClickedEvent, new MouseButtonEventHandler(OnMouseClicked));

			GameData.Data.GameModeChanged += OnGameModeChanged;
			GameData.Data.FocusedObjectChanged += OnFocusedObjectChanged;

			this.ClientTools = new ClientTools();
			this.ClientTools.ToolModeChanged += MainWindowTools_ToolModeChanged;
			this.ClientTools.ToolMode = ClientToolMode.Info;

			this.MapControl.GotSelection += MapControl_GotSelection;

			// for some reason this prevents the changing of focus from mapcontrol with cursor keys
			KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.Once);

			mainWindowTools.SetClientTools(this.ClientTools);
		}

		void MainWindow_SourceInitialized(object sender, EventArgs e)
		{
			var p = (Win32.WindowPlacement)Properties.Settings.Default.MainWindowPlacement;
			if (p != null)
				Win32.Helpers.LoadWindowPlacement(this, p);
		}

		async void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			switch (m_closeStatus)
			{
				case CloseStatus.None:
					m_closeStatus = CloseStatus.ShuttingDown;

					e.Cancel = true;

					var p = Win32.Helpers.SaveWindowPlacement(this);
					Properties.Settings.Default.MainWindowPlacement = p;
					Properties.Settings.Default.Save();

					var dlg = OpenLogOnDialog();

					try
					{
						var prog = new Progress<string>(str => dlg.AppendText(str));
						await GameData.Data.ConnectManager.DisconnectAsync(prog);
						await GameData.Data.ConnectManager.StopServerAsync(prog);
					}
					catch (Exception exc)
					{
						MessageBox.Show(exc.ToString(), "Error closing down");
					}

					dlg.Close();

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

		void MainWindow_Closed(object sender, EventArgs e)
		{
			map.Dispose();
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

				case ClientToolMode.InstallItem:
				case ClientToolMode.BuildItem:
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
						var stockpile = Stockpile.CreateStockpile(env, selection.SelectionIntRectZ);

						var dlg = new ObjectEditDialog();
						dlg.DataContext = stockpile;
						dlg.Owner = this;
						dlg.Show();
					}
					break;

				case ClientToolMode.InstallItem:
					{
						var p = selection.SelectionPoint;

						var dlg = new InstallItemDialog();
						dlg.SetContext(env, p);

						var res = dlg.ShowDialog();

						if (res == true)
						{
							var item = dlg.SelectedItem;

							if (item != null)
								env.InstallItemManager.AddInstallJob(item, p);
						}
					}
					break;

				case ClientToolMode.BuildItem:
					{
						var p = selection.SelectionPoint;

						var workbench = env.GetContents(p).OfType<ItemObject>()
							.SingleOrDefault(i => i.IsInstalled && i.ItemCategory == ItemCategory.Workbench);

						if (workbench == null)
							break;

						var ctrl = new BuildingEditControl();
						ctrl.DataContext = BuildItemManager.FindOrCreateBuildItemManager(workbench);

						var dlg = new Window();
						dlg.Content = ctrl;

						var res = dlg.ShowDialog();

						if (res == true)
						{
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

		void OnGameModeChanged(GameMode mode)
		{
			this.CommandBindings.Clear();

			if (mode == GameMode.Undefined)
				return;

			m_cmdHandler.AddCommandBindings(mode);
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

		public LogOnDialog OpenLogOnDialog()
		{
			this.IsEnabled = false;

			var logOnDialog = new LogOnDialog();
			logOnDialog.Owner = this;
			logOnDialog.Closed += (s, e) => { this.IsEnabled = true; Focus(); };
			logOnDialog.Focusable = false;
			logOnDialog.ShowActivated = false;
			logOnDialog.Show();

			return logOnDialog;
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

		void OnFocusedObjectChanged(LivingObject oldOb, LivingObject newOb)
		{
			if (oldOb != null)
				oldOb.ObjectMoved -= OnFocusedControllableMoved;

			if (newOb != null)
			{
				newOb.ObjectMoved += OnFocusedControllableMoved;
				this.MapControl.FocusedTileView.SetTarget(newOb.Environment, newOb.Location);
			}
			else
			{
				this.MapControl.FocusedTileView.ClearTarget();
			}

			// always follow the focused ob for now
			this.FollowObject = newOb;
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

			var msg = (ClientEvent)e.AddedItems[0];

			if (msg.Environment == null)
				return;

			map.ScrollTo(msg.Environment, msg.Location);
		}
	}
}
