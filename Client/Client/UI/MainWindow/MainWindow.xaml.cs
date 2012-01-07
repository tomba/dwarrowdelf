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

namespace Dwarrowdelf.Client.UI
{
	sealed partial class MainWindow : Window, INotifyPropertyChanged
	{
		MovableObject m_followObject;
		bool m_closing;

		bool m_serverInAppDomain = true;

		bool m_autoConnect = true;
		bool m_autoEnterGame = true;

		// Stores previous user values for setTerrainData
		SetTerrainData m_setTerrainData;

		ServerInAppDomain m_server;

		LogOnDialog m_logOnDialog;

		MainWindowCommandHandler m_cmdHandler;

		DispatcherTimer m_focusDebugTimer;

		public MainWindow()
		{
			InitializeComponent();

			this.MapControl.GotSelection += MapControl_GotSelection;
			this.mainWindowTools.ToolModeChanged += MainWindowTools_ToolModeChanged;

			this.mainWindowTools.ToolMode = ClientToolMode.Info;

			m_focusDebugTimer = new DispatcherTimer();
			m_focusDebugTimer.Interval = TimeSpan.FromMilliseconds(250);
			m_focusDebugTimer.Tick += (o, ea) =>
			{
				this.FocusedElement = Keyboard.FocusedElement as UIElement;
			};
			m_focusDebugTimer.Start();

			// for some reason this prevents the changing of focus from mapcontrol with cursor keys
			KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.Once);
		}

		UIElement m_focusedElement;
		public UIElement FocusedElement
		{
			get { return m_focusedElement; }
			set
			{
				if (m_focusedElement == value)
					return;

				m_focusedElement = value;
				Notify("FocusedElement");
			}
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
				case ClientToolMode.DesignationFellTree:
				case ClientToolMode.DesignationRemove:
				case ClientToolMode.SetTerrain:
				case ClientToolMode.CreateLiving:
					this.MapControl.SelectionMode = MapSelectionMode.Cuboid;
					break;

				case ClientToolMode.CreateStockpile:
				case ClientToolMode.ConstructBuilding:
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

			switch (this.mainWindowTools.ToolMode)
			{
				case ClientToolMode.DesignationRemove:
					env.Designations.RemoveArea(selection.SelectionCuboid);
					break;

				case ClientToolMode.DesignationMine:
					env.Designations.AddArea(selection.SelectionCuboid, DesignationType.Mine);
					break;

				case ClientToolMode.DesignationStairs:
					env.Designations.AddArea(selection.SelectionCuboid, DesignationType.CreateStairs);
					break;

				case ClientToolMode.DesignationFellTree:
					env.Designations.AddArea(selection.SelectionCuboid, DesignationType.FellTree);
					break;

				case ClientToolMode.CreateStockpile:
					{
						var stockpile = new Stockpile(env, selection.SelectionIntRectZ);
						env.AddMapElement(stockpile);

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
						dlg.DataContext = env;

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

							var args = new Dictionary<string, object>()
							{
								{ "envID", map.Environment.ObjectID },
								{ "cube", selection.SelectionCuboid },
								{ "terrainID", data.TerrainID },
								{ "terrainMaterialID", data.TerrainMaterialID },
								{ "interiorID", data.InteriorID },
								{ "interiorMaterialID", data.InteriorMaterialID },
								{ "grass", data.Grass },
								{ "waterLevel", data.Water.HasValue ? (data.Water == true ? (byte?)TileData.MaxWaterLevel : (byte?)0) : null },
							};

							var script =
@"env = world.GetObject(envID)
for p in cube.Range():
	td = env.GetTileData(p)

	if terrainID != None:
		td.TerrainID = terrainID
	if terrainMaterialID != None:
		td.TerrainMaterialID = terrainMaterialID

	if interiorID != None:
		td.InteriorID = interiorID
	if interiorMaterialID != None:
		td.InteriorMaterialID = interiorMaterialID

	if grass != None:
		if grass:
			td.Flags |= Dwarrowdelf.TileFlags.Grass
		else:
			td.Flags &= ~Dwarrowdelf.TileFlags.Grass

	if waterLevel != None:
		td.WaterLevel = waterLevel

	env.SetTileData(p, td)

env.ScanWaterTiles()
";
							var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

							GameData.Data.Connection.Send(msg);
						}
					}
					break;

				case ClientToolMode.CreateItem:
					{
						var dialog = new CreateItemDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionCuboid);
						var res = dialog.ShowDialog();

						if (res == true)
						{
							var args = new Dictionary<string, object>()
							{
								{ "envID", dialog.Environment.ObjectID },
								{ "area", dialog.Area },
								{ "itemID", dialog.ItemID },
								{ "materialID", dialog.MaterialID },
							};

							var script =
@"env = world.GetObject(envID)
for p in area.Range():
	builder = Dwarrowdelf.Server.ItemObjectBuilder(itemID, materialID)
	item = builder.Create(world)
	item.MoveTo(env, p)";

							var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

							GameData.Data.Connection.Send(msg);
						}
					}
					break;

				case ClientToolMode.CreateLiving:
					{
						var dialog = new CreateLivingDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionIntRectZ);
						var res = dialog.ShowDialog();

						if (res == true)
						{
							GameData.Data.Connection.Send(new CreateLivingMessage()
							{
								EnvironmentID = dialog.Environment.ObjectID,
								Area = dialog.Area,

								Name = dialog.LivingName,
								LivingID = dialog.LivingID,

								IsControllable = dialog.IsControllable,
								IsHerd = dialog.IsHerd,
							});
						}
					}
					break;

				case ClientToolMode.ConstructBuilding:
					{
						var dialog = new ConstructBuildingDialog();
						dialog.Owner = this;
						dialog.SetContext(env, selection.SelectionIntRectZ);
						var res = dialog.ShowDialog();

						if (res == true)
						{
							var id = dialog.BuildingID;
							var site = new ConstructionSite(env, id, selection.SelectionIntRectZ);
							env.AddMapElement(site);
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
		}

		void OnMouseClicked(object sender, MouseButtonEventArgs ev)
		{
			if (this.mainWindowTools.ToolMode == ClientToolMode.Info)
			{
				var ml = this.MapControl.MapControl.ScreenPointToMapLocation(ev.GetPosition(this.MapControl));

				var env = this.MapControl.Environment;

				if (env == null)
					return;

				List<object> obs = new List<object>();
				obs.AddRange(env.GetContents(ml));
				obs.AddRange(env.GetElementsAt(ml));

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
					ctxMenu.PlacementTarget = this.MapControl.MapControl;
					var rect = this.MapControl.MapControl.MapRectToScreenPointRect(new IntRect(ml.ToIntPoint(), new IntSize(1, 1)));
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

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var p = (Win32.WindowPlacement)Properties.Settings.Default.MainWindowPlacement;
			if (p != null)
				Win32.Helpers.LoadWindowPlacement(this, p);

			if (m_autoConnect)
				Connect();
		}

		public MasterMapControl MapControl { get { return map; } }

		public GameData Data { get { return GameData.Data; } }

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (m_closing)
				return;

			var p = Win32.Helpers.SaveWindowPlacement(this);
			Properties.Settings.Default.MainWindowPlacement = p;
			Properties.Settings.Default.Save();

			if (GameData.Data.Connection != null)
			{
				e.Cancel = true;
				m_closing = true;

				Disconnect();
			}

			this.FollowObject = null;
			map.Environment = null;
			map.Dispose();
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
					FollowedObjectMoved(m_followObject, m_followObject.Environment, m_followObject.Location);
				}

				Notify("FollowObject");
			}
		}

		void FollowedObjectMoved(MovableObject ob, ContainerObject dst, IntPoint3D loc)
		{
			EnvironmentObject env = dst as EnvironmentObject;

			map.ScrollTo(env, loc);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			var toolData = MainWindowToolBar.ToolDatas.Select(kvp => kvp.Value).FirstOrDefault(td => td.Key == e.Key);

			if (toolData != null)
			{
				this.mainWindowTools.ToolMode = toolData.Mode;

				if (e.Key == Key.Escape)
					map.Focus();

				e.Handled = true;
				base.OnKeyDown(e);
				return;
			}

			switch (e.Key)
			{
				case Key.OemPeriod:
					if (GameData.Data.User != null)
						GameData.Data.User.SendProceedTurn();
					break;

				case Key.Space:
					ClientCommands.AutoAdvanceTurnCommand.Execute(null, this);
					break;

				default:
					base.OnKeyDown(e);
					return;
			}

			e.Handled = true;
			base.OnKeyDown(e);
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

		void SetLogOnText(string text)
		{
			if (m_logOnDialog == null)
			{
				this.IsEnabled = false;

				m_logOnDialog = new LogOnDialog();
				m_logOnDialog.Owner = this;
				m_logOnDialog.SetText(text);
				m_logOnDialog.Show();
			}
			else
			{
				m_logOnDialog.SetText(text);
			}
		}

		void CloseLoginDialog()
		{
			if (m_logOnDialog != null)
			{
				m_logOnDialog.Close();
				m_logOnDialog = null;

				this.IsEnabled = true;
				this.Focus();
			}
		}


		public void Connect()
		{
			if (m_serverInAppDomain)
			{
				SetLogOnText("Starting server");

				m_server = new ServerInAppDomain();
				m_server.Started += () => this.Dispatcher.BeginInvoke(new Action(OnServerStarted));
				m_server.StatusChanged += (str) => this.Dispatcher.BeginInvoke(new Action<string>(SetLogOnText), str);
				m_server.Start();
			}
			else
			{
				OnServerStarted();
			}
		}

		void OnServerStarted()
		{
			var world = new World();
			GameData.Data.World = world;

			GameData.Data.Connection = new ClientConnection(world);
			GameData.Data.Connection.DisconnectEvent += OnDisconnected;

			SetLogOnText("Connecting");

			GameData.Data.Connection.BeginLogOn("tomba", OnConnected);
		}

		void OnConnected(ClientUser user, string error)
		{
			GameData.Data.Connection.LogOutEvent += OnLoggedOut;

			if (error != null)
			{
				CloseLoginDialog();
				MessageBox.Show(error, "Connection Failed");
				return;
			}

			GameData.Data.User = user;

			if (user.IsPlayerInGame || !m_autoEnterGame)
			{
				CloseLoginDialog();
				return;
			}

			EnterGame();
		}



		public void Disconnect()
		{
			if (GameData.Data.User.IsPlayerInGame)
			{
				SetLogOnText("Saving");

				ClientSaveManager.SaveEvent += OnGameSaved;

				GameData.Data.Connection.Send(new SaveRequestMessage());
			}
			else
			{
				GameData.Data.User = null;
				SetLogOnText("Logging Out");
				GameData.Data.Connection.SendLogOut();
			}
		}

		void OnGameSaved()
		{
			GameData.Data.User = null;

			ClientSaveManager.SaveEvent -= OnGameSaved;

			SetLogOnText("Logging Out");
			GameData.Data.Connection.SendLogOut();
		}

		void OnLoggedOut()
		{
			CloseLoginDialog();
			GameData.Data.Connection.LogOutEvent -= OnLoggedOut;
			GameData.Data.Connection.DisconnectEvent -= OnDisconnected;
			GameData.Data.Connection = null;

			if (m_server != null)
			{
				m_server.Stop();
				m_server = null;
			}

			if (m_closing)
				Close();
		}

		void OnDisconnected()
		{
			GameData.Data.Connection.LogOutEvent -= OnLoggedOut;
			GameData.Data.Connection.DisconnectEvent -= OnDisconnected;
			GameData.Data.Connection = null;

			if (m_server != null)
			{
				m_server.Stop();
				m_server = null;
			}
		}








		public void EnterGame()
		{
			SetLogOnText("Entering Game");

			GameData.Data.User.SendEnterGame(OnEnteredGame);
		}

		void OnEnteredGame()
		{
			CloseLoginDialog();
			GameData.Data.User.ExitedGameEvent += OnExitedGame;
		}

		public void ExitGame()
		{
			GameData.Data.User.SendExitGame();
		}

		void OnExitedGame()
		{
			GameData.Data.User.ExitedGameEvent -= OnExitedGame;
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
