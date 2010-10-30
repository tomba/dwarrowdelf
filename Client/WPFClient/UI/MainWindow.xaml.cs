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

namespace Dwarrowdelf.Client
{
	partial class MainWindow : Window, INotifyPropertyChanged
	{
		ClientGameObject m_followObject;
		bool m_closing;
		DispatcherTimer m_timer;

		public MainWindow()
		{
			Application.Current.MainWindow = this;
			//this.WindowState = WindowState.Maximized;

			this.CurrentTileInfo = new TileInfo();

			InitializeComponent();

			//this.Width = 1024;
			//this.Height = 600;

			this.PreviewKeyDown += Window_PreKeyDown;
			this.PreviewTextInput += Window_PreTextInput;
			map.MouseDown += MapControl_MouseDown;

			GameData.Data.Connection.LogOnEvent += OnLoggedOn;
			GameData.Data.Connection.LogOffEvent += OnLoggedOff;
			GameData.Data.Connection.LogOnCharEvent += OnCharLoggedOn;
			GameData.Data.Connection.LogOffCharEvent += OnCharLoggedOff;
		}

		void OnTileSizeChanged(object ob, EventArgs e)
		{
			RecalcCenterPos();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			foreach (var content in dockingManager.DockableContents)
			{
				var item = new MenuItem()
				{
					Tag = content,
					Header = content.Title,
					IsChecked = true,
				};

				item.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Click_ShowWindow));
				contentMenu.Items.Add(item);

				content.StateChanged += new RoutedEventHandler(dockableContent_StateChanged);
			}

			foreach (var id in Enum.GetValues(typeof(InteriorID)))
			{
				var item = new MenuItem()
				{
					Tag = id,
					Header = id.ToString(),
				};
				item.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Click_SetInterior));
				setInteriorMenu.Items.Add(item);
			}

			foreach (var id in Enum.GetValues(typeof(FloorID)))
			{
				var item = new MenuItem()
				{
					Tag = id,
					Header = id.ToString(),
				};
				item.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Click_SetFloor));
				setFloorMenu.Items.Add(item);
			}

			foreach (var id in Enum.GetValues(typeof(MaterialID)))
			{
				var item = new MenuItem()
				{
					Tag = id,
					Header = id.ToString(),
				};
				item.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Click_SetInteriorMaterial));
				setInteriorMaterialMenu.Items.Add(item);
			}

			foreach (var id in Enum.GetValues(typeof(MaterialID)))
			{
				var item = new MenuItem()
				{
					Tag = id,
					Header = id.ToString(),
				};
				item.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Click_SetFloorMaterial));
				setFloorMaterialMenu.Items.Add(item);
			}

			foreach (var id in Enum.GetValues(typeof(BuildingID)))
			{
				var item = new MenuItem()
				{
					Tag = id,
					Header = id.ToString(),
				};
				item.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Click_Build));
				createBuildingMenu.Items.Add(item);
			}

			foreach (var id in Enum.GetValues(typeof(DesignationType)))
			{
				var item = new MenuItem()
				{
					Tag = id,
					Header = id.ToString(),
				};
				item.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(MenuItem_Click_Designate));
				designateMenu.Items.Add(item);
			}

			var dpd = DependencyPropertyDescriptor.FromProperty(GameData.CurrentObjectProperty, typeof(GameData));
			dpd.AddValueChanged(GameData.Data, (ob, ev) => this.FollowObject = GameData.Data.CurrentObject);

			CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
			m_timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnTimerCallback, this.Dispatcher);
		}

		void dockableContent_StateChanged(object sender, RoutedEventArgs e)
		{
			var content = (AvalonDock.DockableContent)e.Source;
			MenuItem item = null;
			foreach (MenuItem i in contentMenu.Items)
			{
				if (i.Tag == content)
				{
					item = i;
					break;
				}
			}

			if (item == null)
				throw new Exception();

			if (content.State == AvalonDock.DockableContentState.Hidden)
				item.IsChecked = false;
			else
				item.IsChecked = true;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var p = (Win32.WindowPlacement)Properties.Settings.Default.MainWindowPlacement;
			if (p != null)
				Win32.Helpers.LoadWindowPlacement(this, p);
		}

		private void dockingManager_Loaded(object sender, RoutedEventArgs e)
		{
			RestoreLayout();
		}

		public AvalonDock.DockingManager Dock { get { return dockingManager; } }

		public MasterMapControl MapControl { get { return map; } }

		public void OnServerStarted()
		{
			// xxx autologin
			LogOn_Button_Click(null, null);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (m_closing)
				return;

			var p = Win32.Helpers.SaveWindowPlacement(this);
			Properties.Settings.Default.MainWindowPlacement = p;
			Properties.Settings.Default.Save();

			SaveLayout();

			var conn = GameData.Data.Connection;

			if (conn.IsCharConnected)
			{
				e.Cancel = true;
				m_closing = true;
				conn.Send(new Messages.LogOffCharRequestMessage());
			}
			else if (conn.IsUserConnected)
			{
				e.Cancel = true;
				m_closing = true;
				conn.Send(new Messages.LogOffRequestMessage());
			}
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			GameData.Data.Connection.LogOnEvent -= OnLoggedOn;
			GameData.Data.Connection.LogOffEvent -= OnLoggedOff;
			GameData.Data.Connection.LogOnCharEvent -= OnLoggedOn;
			GameData.Data.Connection.LogOffCharEvent -= OnLoggedOff;
		}

		int m_fpsCounter;
		void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			m_fpsCounter++;
		}

		void OnTimerCallback(object ob, EventArgs args)
		{
			fpsTextBlock.Text = ((double)m_fpsCounter).ToString();
			m_fpsCounter = 0;
		}

		private void FilterItems(object sender, FilterEventArgs e)
		{
			if (e.Item is ItemObject)
				e.Accepted = true;
			else
				e.Accepted = false;
		}

		public ClientGameObject FollowObject
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
				else
				{
					//map.InvalidateTiles(); ??? XXX

					this.CurrentTileInfo.Environment = null;
					this.CurrentTileInfo.Location = new IntPoint3D();
				}

				Notify("FollowObject");
			}
		}

		void FollowedObjectMoved(ClientGameObject ob, ClientGameObject dst, IntPoint3D loc)
		{
			Environment env = dst as Environment;

			map.Environment = env;
			map.Z = loc.Z;

			RecalcCenterPos();

			this.CurrentTileInfo.Environment = env;
			this.CurrentTileInfo.Location = loc;
		}

		void RecalcCenterPos()
		{
			if (this.FollowObject == null)
				return;

			var loc = this.FollowObject.Location.ToIntPoint();

			int xd = map.Columns / 2;
			int yd = map.Rows / 2;
			int x = loc.X;
			int y = loc.Y;
			IntPoint newPos = new IntPoint(((x + xd / 2) / xd) * xd, ((y + yd / 2) / yd) * yd);

			map.CenterPos = newPos;
		}

		public TileInfo CurrentTileInfo { get; private set; }

		static Direction KeyToDir(Key key)
		{
			Direction dir;

			switch (key)
			{
				case Key.Up: dir = Direction.North; break;
				case Key.Down: dir = Direction.South; break;
				case Key.Left: dir = Direction.West; break;
				case Key.Right: dir = Direction.East; break;
				case Key.Home: dir = Direction.NorthWest; break;
				case Key.End: dir = Direction.SouthWest; break;
				case Key.PageUp: dir = Direction.NorthEast; break;
				case Key.PageDown: dir = Direction.SouthEast; break;
				default:
					throw new Exception();
			}

			return dir;
		}

		static bool KeyIsDir(Key key)
		{
			switch (key)
			{
				case Key.Up: break;
				case Key.Down: break;
				case Key.Left: break;
				case Key.Right: break;
				case Key.Home: break;
				case Key.End: break;
				case Key.PageUp: break;
				case Key.PageDown: break;
				default:
					return false;
			}
			return true;
		}

		void Window_PreKeyDown(object sender, KeyEventArgs e)
		{
			//MyDebug.WriteLine("OnMyKeyDown");
			if (GameData.Data.Connection == null)
				return;

			if (inputTextBox.IsFocused)
				return;

			var currentOb = GameData.Data.CurrentObject;

			if (KeyIsDir(e.Key))
			{
				e.Handled = true;
				Direction dir = KeyToDir(e.Key);
				if (currentOb != null)
				{
					if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
					{
						currentOb.RequestAction(new MineAction(dir, MineActionType.Mine, ActionPriority.Normal));
					}
					else
					{
						var env = currentOb.Environment;
						var curFloorId = env.GetFloor(currentOb.Location).ID;
						var destInterId = env.GetInterior(currentOb.Location + dir).ID;
						var destDownFloorId = env.GetFloor(currentOb.Location + dir + Direction.Down).ID;

						if (dir.IsCardinal())
						{
							if (curFloorId.IsSlope() && curFloorId == dir.ToSlope())
								dir |= Direction.Up;
							else if (destInterId == InteriorID.Empty && destDownFloorId.IsSlope() && destDownFloorId == dir.Reverse().ToSlope())
								dir |= Direction.Down;
						}

						currentOb.RequestAction(new MoveAction(dir, ActionPriority.Normal));
					}
				}
				else
				{
					var v = IntVector.FromDirection(dir);
					var m = ((map.Columns + map.Rows) / 2) / 10;
					if (m < 1)
						m = 1;
					v = v * m;
					map.CenterPos += v;
				}
			}
			else if (e.Key == Key.Space)
			{
				e.Handled = true;
				GameData.Data.Connection.SendProceedTurn();
			}
			else if (e.Key == Key.Add)
			{
				e.Handled = true;
				map.TileSize *= 2;
			}

			else if (e.Key == Key.Subtract)
			{
				e.Handled = true;
				map.TileSize /= 2;
			}

		}

		void Window_PreTextInput(object sender, TextCompositionEventArgs e)
		{
			if (inputTextBox.IsFocused)
				return;

			string text = e.Text;
			Direction dir;

			if (text == ">")
			{
				dir = Direction.Down;
			}
			else if (text == "<")
			{
				dir = Direction.Up;
			}
			else
			{
				return;
			}

			e.Handled = true;
			var currentOb = GameData.Data.CurrentObject;
			if (currentOb != null)
			{
				currentOb.RequestAction(new MoveAction(dir, ActionPriority.Normal));
			}
			else
			{
				map.Z += new IntVector3D(dir).Z;
			}
		}

		void MapControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.RightButton == MouseButtonState.Pressed)
			{
				var ml = new IntPoint3D(map.ScreenPointToMapLocation(e.GetPosition(map)), map.Z);

				if (map.Selection.SelectionCuboid.Contains(ml))
					return;

				map.Selection = new MapSelection(ml, ml);
			}
		}

		internal Environment Map
		{
			get { return map.Environment; }
			set { map.Environment = value; }
		}

		void MenuItem_Click_SetInterior(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			var inter = (InteriorID)item.Tag;

			GameData.Data.Connection.Send(new SetTilesMessage()
			{
				MapID = map.Environment.ObjectID,
				Cube = map.Selection.SelectionCuboid,
				InteriorID = inter,
			});
		}

		void MenuItem_Click_SetFloor(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			var floor = (FloorID)item.Tag;

			GameData.Data.Connection.Send(new SetTilesMessage()
			{
				MapID = map.Environment.ObjectID,
				Cube = map.Selection.SelectionCuboid,
				FloorID = floor,
			});
		}

		void MenuItem_Click_SetInteriorMaterial(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			var material = (MaterialID)item.Tag;

			GameData.Data.Connection.Send(new SetTilesMessage()
			{
				MapID = map.Environment.ObjectID,
				Cube = map.Selection.SelectionCuboid,
				InteriorMaterialID = material,
			});
		}

		void MenuItem_Click_SetFloorMaterial(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			var material = (MaterialID)item.Tag;

			GameData.Data.Connection.Send(new SetTilesMessage()
			{
				MapID = map.Environment.ObjectID,
				Cube = map.Selection.SelectionCuboid,
				FloorMaterialID = material,
			});
		}

		private void MenuItem_Click_SetWater(object sender, RoutedEventArgs e)
		{
			GameData.Data.Connection.Send(new SetTilesMessage()
			{
				MapID = map.Environment.ObjectID,
				Cube = map.Selection.SelectionCuboid,
				WaterLevel = TileData.MaxWaterLevel,
			});
		}

		private void MenuItem_Click_SetGrass(object sender, RoutedEventArgs e)
		{
			bool g = bool.Parse((string)((MenuItem)e.Source).Tag);
			GameData.Data.Connection.Send(new SetTilesMessage()
			{
				MapID = map.Environment.ObjectID,
				Cube = map.Selection.SelectionCuboid,
				Grass = g,
			});
		}

		private void MenuItem_Click_Job(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			string tag = (string)item.Tag;

			if (tag == "Mine")
			{
				var env = map.Environment;

				foreach (var p in map.Selection.SelectionCuboid.Range())
				{
					if (env.GetInterior(p).ID != InteriorID.NaturalWall)
						continue;

					var job = new Jobs.AssignmentGroups.MoveMineJob(null, ActionPriority.Normal, env, p, MineActionType.Mine);
					job.StateChanged += OnJobStateChanged;
					this.Map.World.JobManager.Add(job);
				}
			}
			else if (tag == "FellTree")
			{
				var env = map.Environment;

				foreach (var p in map.Selection.SelectionCuboid.Range())
				{
					if (env.GetInterior(p).ID != InteriorID.Tree)
						continue;

					var job = new Jobs.AssignmentGroups.MoveFellTreeJob(null, ActionPriority.Normal, env, p);
					job.StateChanged += OnJobStateChanged;
					this.Map.World.JobManager.Add(job);
				}
			}
			else if (tag == "MineArea")
			{
				var area = map.Selection.SelectionCuboid;
				var env = map.Environment;

				var job = new Jobs.AssignmentGroups.MineAreaJob(env, ActionPriority.Normal, area, MineActionType.Mine);
				job.StateChanged += OnJobStateChanged;
				this.Map.World.JobManager.Add(job);
			}
			else if (tag == "MineAreaParallel")
			{
				var area = map.Selection.SelectionCuboid;
				var env = map.Environment;

				var job = new Jobs.JobGroups.MineAreaParallelJob(env, ActionPriority.Normal, area, MineActionType.Mine);
				job.StateChanged += OnJobStateChanged;
				this.Map.World.JobManager.Add(job);
			}
			else if (tag == "MineAreaSerial")
			{
				var area = map.Selection.SelectionCuboid;
				var env = map.Environment;

				var job = new Jobs.JobGroups.MineAreaSerialJob(env, ActionPriority.Normal, area, MineActionType.Mine);
				job.StateChanged += OnJobStateChanged;
				this.Map.World.JobManager.Add(job);
			}
			else if (tag == "Goto")
			{
				var p = map.Selection.SelectionCuboid.Corner1;
				var env = map.Environment;

				var job = new Jobs.Assignments.MoveAssignment(null, ActionPriority.Normal, env, p, Positioning.Exact);
				job.StateChanged += OnJobStateChanged;
				this.Map.World.JobManager.Add(job);
			}
			else if (tag == "RunInCircles")
			{
				var job = new Jobs.AssignmentGroups.RunInCirclesJob(null, ActionPriority.Normal, map.Environment);
				job.StateChanged += OnJobStateChanged;
				this.Map.World.JobManager.Add(job);
			}
			else if (tag == "Loiter")
			{
				var job = new Jobs.AssignmentGroups.LoiterJob(null, ActionPriority.Normal, map.Environment);
				job.StateChanged += OnJobStateChanged;
				this.Map.World.JobManager.Add(job);
			}
			else if (tag == "Consume")
			{
				var env = map.Environment;
				var consumables = map.Selection.SelectionCuboid.Range()
					.SelectMany(l =>
						env.GetContents(l).OfType<ItemObject>().Where(o => o.RefreshmentValue > 0 || o.NutritionalValue > 0)
					);

				foreach (var c in consumables)
				{
					var job = new Jobs.AssignmentGroups.MoveConsumeJob(null, ActionPriority.Normal, c);
					job.StateChanged += OnJobStateChanged;
					this.Map.World.JobManager.Add(job);
				}
			}
			else
			{
				throw new Exception();
			}
		}

		void OnJobStateChanged(IJob job, JobState state)
		{
			if (state == JobState.Done)
			{
				job.StateChanged -= OnJobStateChanged;
				this.Map.World.JobManager.Remove(job);
			}
		}

		private void MenuItem_Click_Build(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			var id = (BuildingID)item.Tag;

			var r = map.Selection.SelectionCuboid.ToIntRect();
			var env = map.Environment;
			int z = map.Z;

			var msg = new Messages.CreateBuildingMessage() { MapID = env.ObjectID, Area = new IntRect3D(r, z), ID = id };
			GameData.Data.Connection.Send(msg);
		}

		private void Get_Button_Click(object sender, RoutedEventArgs e)
		{
			var plr = GameData.Data.CurrentObject;
			if (!(plr.Environment is Environment))
				throw new Exception();

			var list = currentTileItems.SelectedItems.Cast<ClientGameObject>();

			if (list.Count() == 0)
				return;

			if (list.Contains(plr))
				return;

			Debug.Assert(list.All(o => o.Environment == plr.Environment));
			Debug.Assert(list.All(o => o.Location == plr.Location));

			plr.RequestAction(new GetAction(list, ActionPriority.Normal));
		}

		private void Drop_Button_Click(object sender, RoutedEventArgs e)
		{
			var list = inventoryListBox.SelectedItems.Cast<ClientGameObject>();

			if (list.Count() == 0)
				return;

			var plr = GameData.Data.CurrentObject;

			plr.RequestAction(new DropAction(list, ActionPriority.Normal));
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
				case "Retry":
					job.Retry();
					break;

				case "Abort":
					job.Abort();
					break;

				case "Fail":
					job.Fail();
					break;

				case "Remove":
					if (job.Parent != null)
						return;

					GameData.Data.World.JobManager.Remove(job);
					break;

				default:
					throw new Exception();
			}
		}

		Window m_loginDialog;

		private void LogOn_Button_Click(object sender, RoutedEventArgs e)
		{
			if (!GameData.Data.Connection.IsUserConnected)
			{
				m_loginDialog = new Window();
				m_loginDialog.Topmost = true;
				m_loginDialog.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
				m_loginDialog.Width = 200;
				m_loginDialog.Height = 200;
				var label = new Label();
				label.Content = "Logging in";
				m_loginDialog.Content = label;
				m_loginDialog.Show();

				GameData.Data.Connection.BeginConnect(ConnectCallback);
			}
		}

		// in NetThread context
		void ConnectCallback()
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action(delegate
				{
					GameData.Data.Connection.Send(new Messages.LogOnRequestMessage() { Name = "tomba" });
				}
				), null);
		}

		private void LogOff_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection.IsUserConnected)
				GameData.Data.Connection.Send(new LogOffRequestMessage());
		}

		private void LogOnChar_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection.IsUserConnected && !GameData.Data.Connection.IsCharConnected)
				GameData.Data.Connection.Send(new LogOnCharRequestMessage() { Name = "tomba" });
		}

		private void LogOffChar_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection.IsCharConnected)
				GameData.Data.Connection.Send(new LogOffCharRequestMessage());
		}

		void OnLoggedOn()
		{
			map.CenterPos = new IntPoint(15, 15); // XXX
			map.Z = 9;

			m_loginDialog.Close();
			m_loginDialog = null;
			// xxx autologin
			//LogOnChar_Button_Click(null, null);
		}

		void OnCharLoggedOn()
		{
		}

		void OnCharLoggedOff()
		{
			if (m_closing)
			{
				var conn = GameData.Data.Connection;
				conn.Send(new Messages.LogOffRequestMessage());
			}
		}

		void OnLoggedOff()
		{
			GameData.Data.Connection.Disconnect();

			if (m_closing)
				Close();

			Action del = delegate()
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			};

			Dispatcher.BeginInvoke(del, null);
		}



		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		void Notify(string info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}

		private void currentObjectCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			currentObjectComboBox.SelectedItem = GameData.Data.World.Controllables.FirstOrDefault();
		}

		private void currentObjectCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			currentObjectComboBox.SelectedItem = null;
		}

		private void TextBox_PreviewKeyDown(string str)
		{
			if (!GameData.Data.Connection.IsUserConnected)
			{
				outputTextBox.AppendText("** not connected **\n");
				return;
			}

			var msg = new IPCommandMessage() { Text = str };
			GameData.Data.Connection.Send(msg);
		}

		private void Button_Click_GC(object sender, RoutedEventArgs e)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		private void Button_Click_Break(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}

		const string LayoutFileName = "WindowLayout.xml";

		void SaveLayout()
		{
			dockingManager.SaveLayout(LayoutFileName);
		}

		void RestoreLayout()
		{
			if (System.IO.File.Exists(LayoutFileName))
				dockingManager.RestoreLayout(LayoutFileName);
		}

		private void MenuItem_Click_ShowWindow(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			var content = (AvalonDock.DockableContent)item.Tag;
			content.Show();
		}

		private void Button_Click_FullScreen(object sender, RoutedEventArgs e)
		{
			var button = (System.Windows.Controls.Primitives.ToggleButton)sender;

			if (button.IsChecked.Value)
			{
				this.WindowStyle = System.Windows.WindowStyle.None;
				this.Topmost = true;
				this.WindowState = System.Windows.WindowState.Maximized;
			}
			else
			{
				this.WindowStyle = System.Windows.WindowStyle.SingleBorderWindow;
				this.Topmost = false;
				this.WindowState = System.Windows.WindowState.Normal;
			}
		}

		private void MenuItem_Click_Designate(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			var id = (DesignationType)item.Tag;

			var area = map.Selection.SelectionCuboid;
			var env = map.Environment;

			Designation designation;

			switch (id)
			{
				case DesignationType.Mine:
					designation = new MineDesignation(env, area, MineActionType.Mine);
					break;

				case DesignationType.FellTree:
					designation = new FellTreeDesignation(env, area);
					break;

				case DesignationType.CreateStairs:
					designation = new MineDesignation(env, area, MineActionType.Stairs);
					break;

				default:
					throw new Exception();
			}

			designation.DesignationDone += OnDesignationDone;
			env.World.DesignationManager.AddDesignation(designation);
		}

		void OnDesignationDone(Designation designation)
		{
			designation.DesignationDone -= OnDesignationDone;
			this.Map.World.DesignationManager.RemoveDesignation(designation);
		}

		private void MenuItem_Click_Designations(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			string tag = (string)item.Tag;

			Designation designation = (Designation)designationsListBox.SelectedValue;

			switch (tag)
			{
				case "abort":
					designation.Abort();
					break;

				default:
					throw new Exception();
			}
		}

		private void MenuItem_Click_Construct(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			string tag = (string)item.Tag;

			var p = map.Selection.SelectionCuboid.Corner1;
			var env = map.Environment;

			var building = env.GetBuildingAt(p);

			if (building == null)
				return;

			MaterialClass materialClass;

			switch (building.BuildingInfo.ID)
			{
				case BuildingID.Mason:
					materialClass = MaterialClass.Rock;
					break;

				case BuildingID.Carpenter:
					materialClass = MaterialClass.Wood;
					break;

				default:
					return;
			}

			switch (tag)
			{
				case "Chair":
					building.AddBuildOrder(ItemType.Chair, materialClass);
					break;

				case "Table":
					building.AddBuildOrder(ItemType.Table, materialClass);
					break;

				default:
					throw new Exception();
			}
		}

		private void MenuItem_Click_Stockpile(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			string tag = (string)item.Tag;

			var area = map.Selection.SelectionCuboid;
			var env = map.Environment;

			StockpileType type;
			switch (tag)
			{
				case "Logs":
					type = StockpileType.Logs;
					break;

				case "Gems":
					type = StockpileType.Gems;
					break;

				case "Metals":
					type = StockpileType.Metals;
					break;

				case "Rocks":
					type = StockpileType.Rocks;
					break;

				case "Furniture":
					type = StockpileType.Furniture;
					break;

				case "Remove":
					type = StockpileType.None;
					break;

				default:
					throw new Exception();
			}

			if (type == StockpileType.None)
			{
				foreach (var p in area.Range())
				{
					var sp = env.GetStockpileAt(p);
					if (sp != null)
					{
						env.RemoveStockpile(sp);
						sp.Destruct();
					}
				}
			}
			else
			{
				var stockpile = new Stockpile(env, new IntRect3D(area.ToIntRect(), area.Z), type);
				env.AddStockpile(stockpile);
			}
		}

		private void slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (GameData.Data.Connection.IsUserConnected)
			{
				GameData.Data.Connection.Send(new SetWorldConfigMessage()
				{
					MinTickTime = TimeSpan.FromMilliseconds(slider.Value),
				});
			}
		}

	}

	class MyInteriorsConverter : ListConverter<Tuple<InteriorInfo, MaterialInfo>>
	{
		public MyInteriorsConverter() : base(item => item.Item1.Name + " (" + item.Item2.Name + ")") { }
	}

	class MyFloorsConverter : ListConverter<Tuple<FloorInfo, MaterialInfo>>
	{
		public MyFloorsConverter() : base(item => item.Item1.Name + " (" + item.Item2.Name + ")") { }
	}

	class MyWatersConverter : ListConverter<byte>
	{
		public MyWatersConverter() : base(item => item.ToString()) { }
	}

	class MyBuildingsConverter : ListConverter<BuildingObject>
	{
		public MyBuildingsConverter() : base(item => item.BuildingInfo.Name) { }
	}

	class MyGrassesConverter : ListConverter<bool>
	{
		public MyGrassesConverter() : base(item => item.ToString()) { }
	}
}
