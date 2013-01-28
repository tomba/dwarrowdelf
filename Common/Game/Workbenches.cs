using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Markup;

namespace Dwarrowdelf
{
	[ContentProperty("BuildableItems")]
	public sealed class WorkbenchInfo
	{
		public ItemID WorkbenchID { get; internal set; }
		public List<BuildableItem> BuildableItems { get; internal set; }

		public WorkbenchInfo()
		{
			this.BuildableItems = new List<BuildableItem>();
		}

		public BuildableItem FindBuildableItem(string buildableItemKey)
		{
			return this.BuildableItems.SingleOrDefault(i => i.Key == buildableItemKey);
		}
	}

	[ContentProperty("FixedBuildMaterials")]
	public sealed class BuildableItem
	{
		public string Key { get; internal set; }
		public string FullKey { get; internal set; }
		public ItemID ItemID { get; internal set; }
		public ItemInfo ItemInfo { get { return Items.GetItemInfo(this.ItemID); } }
		public MaterialID? MaterialID { get; internal set; }
		public SkillID SkillID { get; internal set; }
		public LaborID LaborID { get; internal set; }
		public List<FixedMaterialFilter> FixedBuildMaterials { get; internal set; }

		public BuildableItem()
		{
			FixedBuildMaterials = new List<FixedMaterialFilter>();
		}

		public bool MatchBuildItems(IItemObject[] obs)
		{
			var materials = this.FixedBuildMaterials;

			if (obs.Length != materials.Count)
				return false;

			for (int i = 0; i < materials.Count; ++i)
			{
				if (!materials[i].Match(obs[i]))
					return false;
			}

			return true;
		}
	}

	// XXX ItemFilter could possibly be used instead of this
	public sealed class FixedMaterialFilter : IItemMaterialFilter
	{
		public ItemID? ItemID { get; internal set; }
		public ItemCategory? ItemCategory { get; internal set; }

		public MaterialCategory? MaterialCategory { get; internal set; }
		public MaterialID? MaterialID { get; internal set; }

		public bool Match(IItemObject ob)
		{
			if (this.ItemID.HasValue && this.ItemID.Value != ob.ItemID)
				return false;

			if (this.ItemCategory.HasValue && this.ItemCategory.Value != ob.ItemCategory)
				return false;

			if (this.MaterialID.HasValue && this.MaterialID.Value != ob.MaterialID)
				return false;

			if (this.MaterialCategory.HasValue && this.MaterialCategory.Value != ob.MaterialCategory)
				return false;

			return true;
		}

		public IEnumerable<ItemID> ItemIDs { get { if (this.ItemID.HasValue) yield return this.ItemID.Value; } }
		public IEnumerable<ItemCategory> ItemCategories { get { if (this.ItemCategory.HasValue) yield return this.ItemCategory.Value; } }
		public IEnumerable<MaterialID> MaterialIDs { get { if (this.MaterialID.HasValue) yield return this.MaterialID.Value; } }
		public IEnumerable<MaterialCategory> MaterialCategories { get { if (this.MaterialCategory.HasValue) yield return this.MaterialCategory.Value; } }
	}

	public static class Workbenches
	{
		static Dictionary<ItemID, WorkbenchInfo> s_workbenchInfos;

		static Workbenches()
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();

			WorkbenchInfo[] workbenchInfos;

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Game.Workbenches.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = asm,
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					workbenchInfos = (WorkbenchInfo[])System.Xaml.XamlServices.Load(reader);
			}

			s_workbenchInfos = new Dictionary<ItemID, WorkbenchInfo>(workbenchInfos.Length);

			foreach (var workbench in workbenchInfos)
			{
				if (s_workbenchInfos.ContainsKey(workbench.WorkbenchID))
					throw new Exception();

				foreach (var bi in workbench.BuildableItems)
				{
					if (String.IsNullOrEmpty(bi.Key))
						bi.Key = bi.ItemID.ToString();

					bi.FullKey = String.Format("{0},{1}", workbench.WorkbenchID, bi.Key);
				}

				// verify BuildableItem key uniqueness
				var grouped = workbench.BuildableItems.GroupBy(bi => bi.Key);
				foreach (var g in grouped)
					if (g.Count() != 1)
						throw new Exception();

				s_workbenchInfos[workbench.WorkbenchID] = workbench;
			}
		}

		public static WorkbenchInfo GetWorkbenchInfo(ItemID workbenchID)
		{
			Debug.Assert(workbenchID != ItemID.Undefined);
			Debug.Assert(s_workbenchInfos[workbenchID] != null);

			return s_workbenchInfos[workbenchID];
		}

		public static BuildableItem FindBuildableItem(string buildableItemFullKey)
		{
			return s_workbenchInfos.SelectMany(kvp => kvp.Value.BuildableItems).SingleOrDefault(bi => bi.FullKey == buildableItemFullKey);
		}
	}
}
