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
using System.Diagnostics;
using System.ComponentModel;
using MyGame.ClientMsgs;

namespace MyGame.Client
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	partial class MainWindow : Window, INotifyPropertyChanged
	{
		ClientGameObject m_followObject;
		bool m_closing;

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

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var p = (Win32.WindowPlacement)Properties.Settings.Default.MainWindowPlacement;
			if (p != null)
				Win32.Helpers.LoadWindowPlacement(this, p);
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (m_closing)
				return;

			var p = Win32.Helpers.SaveWindowPlacement(this);
			Properties.Settings.Default.MainWindowPlacement = p;
			Properties.Settings.Default.Save();

			var conn = GameData.Data.Connection;

			if (conn.IsCharConnected)
			{
				e.Cancel = true;
				m_closing = true;
				conn.Send(new ClientMsgs.LogOffCharRequest());
			}
			else if (conn.IsUserConnected)
			{
				e.Cancel = true;
				m_closing = true;
				conn.Send(new ClientMsgs.LogOffRequest());
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
					map.InvalidateTiles();

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

			int xd = map.Columns / 2;
			int yd = map.Rows / 2;
			int x = loc.X;
			int y = loc.Y;
			IntPoint newPos = new IntPoint(((x + xd / 2) / xd) * xd, ((y + yd / 2) / yd) * yd);

			map.CenterPos = newPos;
			map.Z = loc.Z;

			this.CurrentTileInfo.Environment = env;
			this.CurrentTileInfo.Location = loc;
		}

		public TileInfo CurrentTileInfo { get; set; }

		Direction KeyToDir(Key key)
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

		bool KeyIsDir(Key key)
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

			var currentOb = GameData.Data.CurrentObject;

			if (KeyIsDir(e.Key))
			{
				e.Handled = true;
				Direction dir = KeyToDir(e.Key);
				if (currentOb != null)
				{
					if (e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
						currentOb.EnqueueAction(new MineAction(dir));
					else
						currentOb.EnqueueAction(new MoveAction(dir));
				}
				else
				{
					map.CenterPos += IntVector.FromDirection(dir);
				}
			}
			else if (e.Key == Key.Space)
			{
				e.Handled = true;
				GameData.Data.Connection.Send(new ProceedTickMessage());
			}
			else if (e.Key == Key.Add)
			{
				e.Handled = true;
				map.TileSize += 8;
			}

			else if (e.Key == Key.Subtract)
			{
				e.Handled = true;
				if (map.TileSize <= 16)
					return;
				map.TileSize -= 8;
			}

		}

		void Window_PreTextInput(object sender, TextCompositionEventArgs e)
		{
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
				currentOb.EnqueueAction(new MoveAction(dir));
			else
				map.Z += IntVector3D.FromDirection(dir).Z;
		}

		void MapControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.RightButton == MouseButtonState.Pressed)
			{
				IntRect r = map.SelectionRect;

				if (r.Width > 1 || r.Height > 1)
					return;

				IntPoint ml = map.ScreenPointToMapLocation(e.GetPosition(map));

				map.SelectionRect = new IntRect(ml, new IntSize(1, 1));
			}
		}

		internal Environment Map
		{
			get { return map.Environment; }
			set { map.Environment = value; }
		}

		private void MenuItem_Click_Floor(object sender, RoutedEventArgs e)
		{
			IntRect r = map.SelectionRect;

			var stone = map.Environment.World.AreaData.Materials.GetMaterialInfo("Stone").ID;
			var undef = new MaterialID(0);

			GameData.Data.Connection.Send(new SetTilesMessage()
			{
				MapID = map.Environment.ObjectID,
				Cube = new IntCube(r, map.Z),
				TileData = new TileData()
				{
					FloorID = FloorID.NaturalFloor,
					FloorMaterialID = stone,
					InteriorID = InteriorID.Empty,
					InteriorMaterialID = undef,
				}
			});
		}

		private void MenuItem_Click_Wall(object sender, RoutedEventArgs e)
		{
			IntRect r = map.SelectionRect;

			var stone = map.Environment.World.AreaData.Materials.GetMaterialInfo("Stone").ID;

			GameData.Data.Connection.Send(new SetTilesMessage()
			{
				MapID = map.Environment.ObjectID,
				Cube = new IntCube(r, map.Z),
				TileData = new TileData()
				{
					FloorID = FloorID.NaturalFloor,
					FloorMaterialID = stone,
					InteriorID = InteriorID.NaturalWall,
					InteriorMaterialID = stone,
				}
			});
		}

		private void MenuItem_Click_Job(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			string tag = (string)item.Tag;

			if (tag == "Mine")
			{
				IntRect r = map.SelectionRect;
				var env = map.Environment;
				int z = map.Z;

				foreach (var p in r.Range())
				{
					if (env.GetInterior(new IntPoint3D(p, z)).ID != InteriorID.NaturalWall)
						continue;

					var job = new MoveMineJob(null, env, new IntPoint3D(p, z));
					this.Map.World.JobManager.Add(job);
				}
			}
			else if (tag == "MineArea")
			{
				IntRect r = map.SelectionRect;
				var env = map.Environment;
				int z = map.Z;

				var job = new MineAreaJob(env, r, z);
				this.Map.World.JobManager.Add(job);
			}
			else if (tag == "MineAreaParallel")
			{
				IntRect r = map.SelectionRect;
				var env = map.Environment;
				int z = map.Z;

				var job = new MineAreaParallelJob(env, r, z);
				this.Map.World.JobManager.Add(job);
			}
			else if (tag == "MineAreaSerial")
			{
				IntRect r = map.SelectionRect;
				var env = map.Environment;
				int z = map.Z;

				var job = new MineAreaSerialJob(env, r, z);
				this.Map.World.JobManager.Add(job);
			}
			else if (tag == "BuildItem")
			{
				IntRect r = map.SelectionRect;
				var env = map.Environment;
				int z = map.Z;

				var building = env.GetBuildingAt(new IntPoint3D(r.TopLeft, z));

				if (building == null)
					return;

				building.AddBuildItem();
			}
			else if (tag == "Goto")
			{
				IntPoint p = map.SelectionRect.TopLeft;
				var env = map.Environment;
				int z = map.Z;

				var job = new MoveActionJob(null, env, new IntPoint3D(p, z), false);
				this.Map.World.JobManager.Add(job);
			}
			else
			{
				throw new Exception();
			}
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

			plr.EnqueueAction(new GetAction(list));
		}

		private void Drop_Button_Click(object sender, RoutedEventArgs e)
		{
			var list = inventoryListBox.SelectedItems.Cast<ClientGameObject>();

			if (list.Count() == 0)
				return;

			var plr = GameData.Data.CurrentObject;

			plr.EnqueueAction(new DropAction(list));
		}

		private void BuildItem_Button_Click(object sender, RoutedEventArgs e)
		{
			var list = inventoryListBox.SelectedItems.Cast<ClientGameObject>();

			if (list.Count() != 2)
				return;

			var plr = GameData.Data.CurrentObject;

			plr.EnqueueAction(new BuildItemAction(list));
		}

		private void MenuItem_Click_JobTreeView(object sender, RoutedEventArgs e)
		{
			MenuItem item = (MenuItem)e.Source;
			string tag = (string)item.Tag;
			IJob job = (IJob)jobTreeView.SelectedValue;

			if (job == null)
				return;

			if (job.Parent != null)
				return;

			if (tag == "Abort")
			{
				job.Abort();
			}
			else if (tag == "Remove")
			{
				GameData.Data.World.JobManager.Remove(job);
			}
		}


		private void LogOn_Button_Click(object sender, RoutedEventArgs e)
		{
			if (!GameData.Data.Connection.IsUserConnected)
				GameData.Data.Connection.BeginConnect(ConnectCallback);
		}

		void ConnectCallback()
		{
			GameData.Data.Connection.Send(new ClientMsgs.LogOnRequest() { Name = "tomba" });
		}

		private void LogOff_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection.IsUserConnected)
				GameData.Data.Connection.Send(new LogOffRequest());
		}

		private void LogOnChar_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection.IsUserConnected && !GameData.Data.Connection.IsCharConnected)
				GameData.Data.Connection.Send(new LogOnCharRequest() { Name = "tomba" });
		}

		private void LogOffChar_Button_Click(object sender, RoutedEventArgs e)
		{
			if (GameData.Data.Connection.IsCharConnected)
				GameData.Data.Connection.Send(new LogOffCharRequest());
		}

		void OnLoggedOn()
		{
			//GameData.Data.Connection.Send(new LogOnCharRequest() { Name = "tomba" });
		}

		void OnCharLoggedOn()
		{
		}

		void OnCharLoggedOff()
		{
			if (m_closing)
			{
				var conn = GameData.Data.Connection;
				conn.Send(new ClientMsgs.LogOffRequest());
			}
		}

		void OnLoggedOff()
		{
			GameData.Data.Connection.Disconnect();

			if (m_closing)
				Close();
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
			currentObjectComboBox.SelectedItem = World.TheWorld.Controllables.FirstOrDefault();
		}

		private void currentObjectCheckBox_Unchecked(object sender, RoutedEventArgs e)
		{
			currentObjectComboBox.SelectedItem = null;
		}
	}
}
