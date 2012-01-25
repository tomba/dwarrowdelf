using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Dwarrowdelf
{
	public interface ITerrainFilter
	{
		bool Match(TileData td);
	}

	public sealed class TerrainFilter : ITerrainFilter
	{
		uint m_terrainMask;
		uint m_interiorMask;

		static TerrainFilter()
		{
			int max;

			max = EnumHelpers.GetEnumMax<TerrainID>() + 1;
			if (max > 32)
				throw new Exception();

			max = EnumHelpers.GetEnumMax<InteriorID>() + 1;
			if (max > 32)
				throw new Exception();
		}

		public TerrainFilter(IEnumerable<TerrainID> terrains, IEnumerable<InteriorID> interiors)
		{
			m_terrainMask = FilterHelpers.CreateMask32(terrains);
			m_interiorMask = FilterHelpers.CreateMask32(interiors);
		}

		public bool Match(TerrainID terrain, InteriorID interior)
		{
			return FilterHelpers.Check(m_terrainMask, (int)terrain) &&
				FilterHelpers.Check(m_interiorMask, (int)interior);
		}

		public bool Match(TileData td)
		{
			return Match(td.TerrainID, td.InteriorID);
		}
	}

	public interface IItemFilter
	{
		bool Match(IItemObject item);
	}

	public sealed class CompoundItemFilter : IItemFilter
	{
		IItemFilter[] m_filters;

		public CompoundItemFilter(IItemFilter filter1, IItemFilter filter2)
		{
			m_filters = new IItemFilter[] { filter1, filter2 };
		}

		public CompoundItemFilter(IItemFilter filter1, IItemFilter filter2, IItemFilter filter3)
		{
			m_filters = new IItemFilter[] { filter1, filter2, filter3 };
		}

		public CompoundItemFilter(params object[] args)
		{
			m_filters = args.Cast<IItemFilter>().ToArray();
		}

		public bool Match(IItemObject item)
		{
			return m_filters.Any(f => f.Match(item));
		}
	}

	public sealed class ItemFilter : IItemFilter
	{
		BitArray m_itemIDMask;
		uint m_itemCategoryMask;
		ulong m_materialIDMask;
		uint m_materialCategoryMask;

		static ItemFilter()
		{
			int max;

			max = EnumHelpers.GetEnumMax<ItemCategory>() + 1;
			if (max > 32)
				throw new Exception();

			max = EnumHelpers.GetEnumMax<MaterialID>() + 1;
			if (max > 64)
				throw new Exception();

			max = EnumHelpers.GetEnumMax<MaterialCategory>() + 1;
			if (max > 32)
				throw new Exception();
		}

		public ItemFilter(IEnumerable<ItemID> itemIDs, IEnumerable<ItemCategory> itemCategories,
			IEnumerable<MaterialID> materialIDs, IEnumerable<MaterialCategory> materialCategories)
		{
			m_itemIDMask = FilterHelpers.CreateMaskArray(itemIDs);
			m_itemCategoryMask = FilterHelpers.CreateMask32(itemCategories);
			m_materialIDMask = FilterHelpers.CreateMask64(materialIDs);
			m_materialCategoryMask = FilterHelpers.CreateMask32(materialCategories);
		}

		public ItemFilter(ItemID itemID, MaterialCategory materialCategory)
		{
			m_itemIDMask = FilterHelpers.CreateMaskArray(new ItemID[] { itemID });
			m_materialCategoryMask = FilterHelpers.CreateMask32(new MaterialCategory[] { materialCategory });
		}

		public bool Match(ItemID itemID, ItemCategory itemCategory, MaterialID materialID, MaterialCategory materialCategory)
		{
			return FilterHelpers.Check(m_itemIDMask, (int)itemID) &&
				FilterHelpers.Check(m_itemCategoryMask, (int)itemCategory) &&
				FilterHelpers.Check(m_materialIDMask, (int)materialID) &&
				FilterHelpers.Check(m_materialCategoryMask, (int)materialCategory);
		}

		public bool Match(IItemObject item)
		{
			return Match(item.ItemID, item.ItemCategory, item.MaterialID, item.MaterialCategory);
		}
	}

	static class FilterHelpers
	{
		public static BitArray CreateMaskArray<T>(IEnumerable<T> bits) where T : IConvertible
		{
			BitArray mask = null;

			if (bits == null || bits.Any())
			{
				mask = new BitArray(EnumHelpers.GetEnumMax<T>() + 1);
				foreach (int v in bits.Select(b => b.ToInt32(null)))
					mask.Set(v, true);
			}

			return mask;
		}

		public static uint CreateMask32<T>(IEnumerable<T> bits) where T : IConvertible
		{
			uint mask = 0;

			if (bits != null)
			{
				foreach (int v in bits.Select(b => b.ToInt32(null)))
					mask |= 1U << v;
			}

			return mask;
		}

		public static ulong CreateMask64<T>(IEnumerable<T> bits) where T : IConvertible
		{
			ulong mask = 0;

			if (bits != null)
			{
				foreach (int v in bits.Select(b => b.ToInt32(null)))
					mask |= 1UL << v;
			}

			return mask;
		}

		public static bool Check(BitArray mask, int bit)
		{
			return mask == null || mask.Get(bit);
		}

		public static bool Check(ulong mask, int bit)
		{
			return (mask == 0 || (mask & (1UL << bit)) != 0);
		}

		public static bool Check(uint mask, int bit)
		{
			return (mask == 0 || (mask & (1U << bit)) != 0);
		}
	}

	public static class WorkHelpers
	{
		public static readonly TerrainFilter ConstructFloorFilter = new TerrainFilter(
			new TerrainID[] { TerrainID.Empty },
			new InteriorID[] { InteriorID.Empty });

		public static readonly ItemFilter ConstructFloorItemFilter = new ItemFilter(ItemID.Block, MaterialCategory.Rock);


		public static readonly TerrainFilter ConstructPavementFilter = new TerrainFilter(
			new TerrainID[] { TerrainID.NaturalFloor, TerrainID.BuiltFloor },
			new InteriorID[] { InteriorID.Empty });

		public static readonly CompoundItemFilter ConstructPavementItemFilter =
			new CompoundItemFilter(
				new ItemFilter(ItemID.Block, MaterialCategory.Rock),
				new ItemFilter(ItemID.Log, MaterialCategory.Wood)
			);


		public static readonly TerrainFilter ConstructWallFilter = new TerrainFilter(
			new TerrainID[] { TerrainID.NaturalFloor, TerrainID.BuiltFloor },
			new InteriorID[] { InteriorID.Empty });

		public static readonly ItemFilter ConstructWallItemFilter = new ItemFilter(ItemID.Block, MaterialCategory.Rock);
	}
}
