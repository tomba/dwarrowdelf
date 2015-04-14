using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public interface ITerrainFilter
	{
		bool Match(TileData td);
	}

	[Serializable]
	public sealed class TerrainFilter : ITerrainFilter
	{
		EnumBitMask32<TileID> m_terrainMask;

		public TerrainFilter(IEnumerable<TileID> terrains)
		{
			m_terrainMask = new EnumBitMask32<TileID>(terrains);
		}

		public bool Match(TileID terrain)
		{
			return m_terrainMask.Get(terrain);
		}

		public bool Match(TileData td)
		{
			// ZZZ: todo
			//return Match(td.TerrainID, td.InteriorID);
			return false;
		}
	}

	public interface IItemFilter
	{
		bool Match(IItemObject item);
	}

	[Serializable]
	public sealed class OrItemFilter : IItemFilter
	{
		IItemFilter[] m_filters;

		public OrItemFilter(IItemFilter filter1, IItemFilter filter2)
		{
			m_filters = new IItemFilter[] { filter1, filter2 };
		}

		public OrItemFilter(IItemFilter filter1, IItemFilter filter2, IItemFilter filter3)
		{
			m_filters = new IItemFilter[] { filter1, filter2, filter3 };
		}

		public OrItemFilter(params object[] args)
		{
			m_filters = args.Cast<IItemFilter>().ToArray();
		}

		public IItemFilter this[int i]
		{
			get
			{
				return m_filters[i];
			}
		}

		public bool Match(IItemObject item)
		{
			return m_filters.Any(f => f.Match(item));
		}

		public override string ToString()
		{
			return String.Format("({0})", String.Join<IItemFilter>(" OR ", m_filters));
		}
	}

	[Serializable]
	public sealed class AndItemFilter : IItemFilter
	{
		IItemFilter[] m_filters;

		public AndItemFilter(IItemFilter filter1, IItemFilter filter2)
		{
			m_filters = new IItemFilter[] { filter1, filter2 };
		}

		public AndItemFilter(IItemFilter filter1, IItemFilter filter2, IItemFilter filter3)
		{
			m_filters = new IItemFilter[] { filter1, filter2, filter3 };
		}

		public AndItemFilter(params object[] args)
		{
			m_filters = args.Cast<IItemFilter>().ToArray();
		}

		public bool Match(IItemObject item)
		{
			return m_filters.All(f => f.Match(item));
		}

		public override string ToString()
		{
			return String.Format("({0})", String.Join<IItemFilter>(" AND ", m_filters));
		}
	}

	[Serializable]
	public sealed class ItemFilter : IItemFilter
	{
		ItemIDMask m_itemIDMask;
		ItemCategoryMask m_itemCategoryMask;
		MaterialIDMask m_materialIDMask;
		MaterialCategoryMask m_materialCategoryMask;

		public ItemFilter(ItemIDMask itemIDMask, ItemCategoryMask itemCategoryMask,
			MaterialIDMask materialIDMask, MaterialCategoryMask materialCategoryMask)
		{
			m_itemIDMask = itemIDMask;
			m_itemCategoryMask = itemCategoryMask;
			m_materialIDMask = materialIDMask;
			m_materialCategoryMask = materialCategoryMask;
		}

		public ItemFilter(ItemID itemID, MaterialCategory materialCategory)
		{
			m_itemIDMask = new ItemIDMask(itemID);
			m_materialCategoryMask = new MaterialCategoryMask(materialCategory);
		}

		public bool Match(ItemID itemID, ItemCategory itemCategory, MaterialID materialID, MaterialCategory materialCategory)
		{
			return (m_itemIDMask == null || m_itemIDMask.Get(itemID)) &&
				(m_itemCategoryMask == null || m_itemCategoryMask.Get(itemCategory)) &&
				(m_materialIDMask == null || m_materialIDMask.Get(materialID)) &&
				(m_materialCategoryMask == null || m_materialCategoryMask.Get(materialCategory));
		}

		public bool Match(IItemObject item)
		{
			return Match(item.ItemID, item.ItemCategory, item.MaterialID, item.MaterialCategory);
		}

		public ItemIDMask ItemIDMask { get { return m_itemIDMask; } }
		public ItemCategoryMask ItemCategoryMask { get { return m_itemCategoryMask; } }
		public MaterialIDMask MaterialIDMask { get { return m_materialIDMask; } }
		public MaterialCategoryMask MaterialCategoryMask { get { return m_materialCategoryMask; } }

		public IEnumerable<ItemID> ItemIDs
		{
			get { return m_itemIDMask != null ? m_itemIDMask.EnumValues : Items.GetItemIDs(); }
		}
		public IEnumerable<ItemCategory> ItemCategories
		{
			get { return m_itemCategoryMask != null ? m_itemCategoryMask.EnumValues : Items.GetItemCategories(); }
		}
		public IEnumerable<MaterialID> MaterialIDs
		{
			get { return m_materialIDMask != null ? m_materialIDMask.EnumValues : Materials.GetMaterialIDs(); }
		}
		public IEnumerable<MaterialCategory> MaterialCategories
		{
			get { return m_materialCategoryMask != null ? m_materialCategoryMask.EnumValues : Materials.GetMaterialCategories(); }
		}

		public override string ToString()
		{
			return String.Format("({0}, {1}, {2}, {3})",
				m_itemIDMask, m_itemCategoryMask,
				m_materialIDMask, m_materialCategoryMask);
		}
	}

	public static class WorkHelpers
	{
		// ZZZ: TODO
		public static readonly TerrainFilter ConstructFloorTerrainFilter = new TerrainFilter(new[] { TileID.Empty });
		/*
		public static readonly TerrainFilter ConstructFloorTerrainFilter = new TerrainFilter(
			new TerrainID[] { TerrainID.Empty },
			new InteriorID[] { InteriorID.Empty });
		*/
		public static readonly ItemFilter ConstructFloorItemFilter = new ItemFilter(ItemID.Block, MaterialCategory.Rock);


		public static readonly TerrainFilter ConstructPavementTerrainFilter = new TerrainFilter(new[] { TileID.Empty });
		/*
		public static readonly TerrainFilter ConstructPavementTerrainFilter = new TerrainFilter(
			new TerrainID[] { TerrainID.NaturalFloor, TerrainID.BuiltFloor },
			new InteriorID[] { InteriorID.Empty, InteriorID.Grass });
		*/
		public static readonly OrItemFilter ConstructPavementItemFilter =
			new OrItemFilter(
				new ItemFilter(ItemID.Block, MaterialCategory.Rock),
				new ItemFilter(ItemID.Log, MaterialCategory.Wood)
			);

		public static readonly TerrainFilter ConstructWallTerrainFilter = new TerrainFilter(new[] { TileID.Empty });
		/*
		public static readonly TerrainFilter ConstructWallTerrainFilter = new TerrainFilter(
			new TerrainID[] { TerrainID.NaturalFloor, TerrainID.BuiltFloor },
			new InteriorID[] { InteriorID.Empty, InteriorID.Grass });
		*/
		public static readonly ItemFilter ConstructWallItemFilter = new ItemFilter(ItemID.Block, MaterialCategory.Rock);
	}
}
