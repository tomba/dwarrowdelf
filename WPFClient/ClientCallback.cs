using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using MyGame.ClientMsgs;

namespace MyGame
{
	class ClientCallback : IClientCallback
	{
		public ClientCallback()
		{
			MyDebug.WriteLine("ClientCallback()");
		}

		#region IClientCallback Members

		public void LoginReply(ObjectID playerID, int visionRange)
		{
			try
			{
				PlayerObject player = new PlayerObject(playerID);
				player.VisionRange = visionRange;
				GameData.Data.Player = player;
				MainWindow.s_mainWindow.map.FollowObject = player;

				MapLevel level = new MapLevel();
				MainWindow.s_mainWindow.Map = level;
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		public void DeliverMessage(Message[] messages)
		{
			foreach (Message msg in messages)
			{
				MyDebug.WriteLine("Received msg {0}", msg.GetType().ToString());
				try
				{
					if (msg is TerrainData)
					{
						MainWindow.s_mainWindow.Map.SetTerrains(((TerrainData)msg).MapDataList);
					}
					else if (msg is ClientMsgs.TurnChange)
					{
						GameData.Data.TurnNumber = ((ClientMsgs.TurnChange)msg).TurnNumber;
					}
					else if (msg is ObjectMove)
					{
						ObjectMove om = (ObjectMove)msg;
						ClientGameObject ob = ClientGameObject.FindObject(om.ObjectID);

						if (ob == null)
						{
							MyDebug.WriteLine("New object appeared {0}", om.ObjectID);
							ob = new ClientGameObject(om.ObjectID);
						}

						ob.SymbolID = om.Symbol;

						if (ob.Environment == null)
							ob.SetEnvironment(MainWindow.s_mainWindow.Map, om.TargetLocation);
						else
							ob.Location = om.TargetLocation;
					}
					else if (msg is ItemsData)
					{						
						ItemsData id = (ItemsData)msg;
						var items = id.Items;

						MyDebug.WriteLine("DeliverInventory, {0} items", items.Length);

						ItemCollection itemCollection = GameData.Data.Player.Inventory;
						itemCollection.Clear();
						foreach (ItemData item in items)
						{
							ItemObject ob = new ItemObject(item.ObjectID);
							ob.Name = item.Name;
							ob.SymbolID = item.SymbolID;
							itemCollection.Add(ob);
						}
						
					}
				}
				catch (Exception e)
				{
					MyDebug.WriteLine("Uncaught exception");
					MyDebug.WriteLine(e.ToString());
				}
			}
		}

		public void TransactionDone(int transactionID)
		{
			MyDebug.WriteLine("TransactionDone({0})", transactionID);
			GameAction action = GameData.Data.ActionCollection.Single(a => a.TransactionID == transactionID);
			GameData.Data.ActionCollection.Remove(action);
		}


		#endregion
	}
}
