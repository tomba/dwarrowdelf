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

	[Serializable]
	public sealed class TerrainFilter : ITerrainFilter
	{
		EnumBitMask32<TerrainID> m_terrainMask;
		EnumBitMask32<InteriorID> m_interiorMask;

		public TerrainFilter(IEnumerable<TerrainID> terrains, IEnumerable<InteriorID> interiors)
		{
			m_terrainMask = new EnumBitMask32<TerrainID>(terrains);
			m_interiorMask = new EnumBitMask32<InteriorID>(interiors);
		}

		public bool Match(TerrainID terrain, InteriorID interior)
		{
			return m_terrainMask.Get(terrain) && m_interiorMask.Get(interior);
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

	public interface IItemMaterialFilter : IItemFilter
	{
		IEnumerable<ItemID> ItemIDs { get; }
		IEnumerable<ItemCategory> ItemCategories { get; }
		IEnumerable<MaterialID> MaterialIDs { get; }
		IEnumerable<MaterialCategory> MaterialCategories { get; }
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
	}

	[Serializable]
	public sealed class ItemFilter : IItemMaterialFilter
	{
		EnumBitMask<ItemID> m_itemIDMask;
		EnumBitMask32<ItemCategory> m_itemCategoryMask;
		EnumBitMask64<MaterialID> m_materialIDMask;
		EnumBitMask32<MaterialCategory> m_materialCategoryMask;

		public ItemFilter(IEnumerable<ItemID> itemIDs, IEnumerable<ItemCategory> itemCategories,
			IEnumerable<MaterialID> materialIDs, IEnumerable<MaterialCategory> materialCategories)
		{
			m_itemIDMask = new EnumBitMask<ItemID>(itemIDs);
			m_itemCategoryMask = new EnumBitMask32<ItemCategory>(itemCategories);
			m_materialIDMask = new EnumBitMask64<MaterialID>(materialIDs);
			m_materialCategoryMask = new EnumBitMask32<MaterialCategory>(materialCategories);
		}

		public ItemFilter(IEnumerable<ItemID> itemIDs, IEnumerable<MaterialID> materialIDs)
		{
			m_itemIDMask = new EnumBitMask<ItemID>(itemIDs);
			m_itemCategoryMask = new EnumBitMask32<ItemCategory>();
			m_materialIDMask = new EnumBitMask64<MaterialID>(materialIDs);
			m_materialCategoryMask = new EnumBitMask32<MaterialCategory>();
		}

		public ItemFilter(ItemID itemID, MaterialCategory materialCategory)
		{
			m_itemIDMask = new EnumBitMask<ItemID>(itemID);
			m_itemCategoryMask = new EnumBitMask32<ItemCategory>();
			m_materialIDMask = new EnumBitMask64<MaterialID>();
			m_materialCategoryMask = new EnumBitMask32<MaterialCategory>(materialCategory);
		}

		public bool Match(ItemID itemID, ItemCategory itemCategory, MaterialID materialID, MaterialCategory materialCategory)
		{
			return m_itemIDMask.Get(itemID) &&
				m_itemCategoryMask.Get(itemCategory) &&
				m_materialIDMask.Get(materialID) &&
				m_materialCategoryMask.Get(materialCategory);
		}

		public bool Match(IItemObject item)
		{
			return Match(item.ItemID, item.ItemCategory, item.MaterialID, item.MaterialCategory);
		}

		public IEnumerable<ItemID> ItemIDs { get { return m_itemIDMask.EnumValues; } }
		public IEnumerable<ItemCategory> ItemCategories { get { return m_itemCategoryMask.EnumValues; } }
		public IEnumerable<MaterialID> MaterialIDs { get { return m_materialIDMask.EnumValues; } }
		public IEnumerable<MaterialCategory> MaterialCategories { get { return m_materialCategoryMask.EnumValues; } }
	}

	[Serializable]
	public class EnumBitMask32<TEnum>
	{
		uint m_mask;

		static EnumBitMask32()
		{
			var max = EnumHelpers.GetEnumMax<TEnum>() + 1;
			if (max > 32)
				throw new Exception();
		}

		public EnumBitMask32()
		{
			m_mask = 0;
		}

		public EnumBitMask32(TEnum enumValue)
		{
			m_mask = 1U << EnumConv.ToInt32(enumValue);
		}

		public EnumBitMask32(IEnumerable<TEnum> enumValues)
		{
			uint mask = 0;

			if (enumValues != null)
			{
				foreach (TEnum e in enumValues)
					mask |= 1U << EnumConv.ToInt32(e);
			}

			m_mask = mask;
		}

		public bool Get(TEnum enumValue)
		{
			return m_mask == 0 || (m_mask & (1U << EnumConv.ToInt32(enumValue))) != 0;
		}

		public IEnumerable<TEnum> EnumValues
		{
			get
			{
				for (int i = 0; i < 32; ++i)
				{
					var b = ((m_mask >> i) & 1) == 1;
					if (b)
						yield return EnumConv.ToEnum<TEnum>(i);
				}
			}
		}
	}

	[Serializable]
	public class EnumBitMask64<TEnum>
	{
		ulong m_mask;

		static EnumBitMask64()
		{
			var max = EnumHelpers.GetEnumMax<TEnum>() + 1;
			if (max > 64)
				throw new Exception();
		}

		public EnumBitMask64()
		{
			m_mask = 0;
		}

		public EnumBitMask64(TEnum enumValue)
		{
			m_mask = 1UL << EnumConv.ToInt32(enumValue);
		}

		public EnumBitMask64(IEnumerable<TEnum> enumValues)
		{
			ulong mask = 0;

			if (enumValues != null)
			{
				foreach (TEnum e in enumValues)
					mask |= 1UL << EnumConv.ToInt32(e);
			}

			m_mask = mask;
		}

		public bool Get(TEnum enumValue)
		{
			return m_mask == 0 || (m_mask & (1UL << EnumConv.ToInt32(enumValue))) != 0;
		}

		public IEnumerable<TEnum> EnumValues
		{
			get
			{
				for (int i = 0; i < 32; ++i)
				{
					var b = ((m_mask >> i) & 1) == 1;
					if (b)
						yield return EnumConv.ToEnum<TEnum>(i);
				}
			}
		}
	}

	[Serializable]
	public class EnumBitMask<TEnum>
	{
		BitArray m_mask;

		public EnumBitMask()
		{
			m_mask = new BitArray(EnumHelpers.GetEnumMax<TEnum>() + 1);
		}

		public EnumBitMask(TEnum enumValue)
		{
			BitArray mask = new BitArray(EnumHelpers.GetEnumMax<TEnum>() + 1);
			mask.Set(EnumConv.ToInt32(enumValue), true);

			m_mask = mask;
		}

		public EnumBitMask(IEnumerable<TEnum> enumValues)
		{
			BitArray mask = null;

			if (enumValues != null && enumValues.Any())
			{
				mask = new BitArray(EnumHelpers.GetEnumMax<TEnum>() + 1);
				foreach (TEnum e in enumValues)
					mask.Set(EnumConv.ToInt32(e), true);
			}

			m_mask = mask;
		}

		public bool Get(TEnum enumValue)
		{
			return m_mask == null || m_mask.Get(EnumConv.ToInt32(enumValue));
		}

		public IEnumerable<TEnum> EnumValues
		{
			get
			{
				for (int i = 0; i < m_mask.Length; ++i)
				{
					var b = m_mask.Get(i);
					if (b)
						yield return EnumConv.ToEnum<TEnum>(i);
				}
			}
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
			new InteriorID[] { InteriorID.Empty, InteriorID.Grass });

		public static readonly OrItemFilter ConstructPavementItemFilter =
			new OrItemFilter(
				new ItemFilter(ItemID.Block, MaterialCategory.Rock),
				new ItemFilter(ItemID.Log, MaterialCategory.Wood)
			);


		public static readonly TerrainFilter ConstructWallFilter = new TerrainFilter(
			new TerrainID[] { TerrainID.NaturalFloor, TerrainID.BuiltFloor },
			new InteriorID[] { InteriorID.Empty, InteriorID.Grass });

		public static readonly ItemFilter ConstructWallItemFilter = new ItemFilter(ItemID.Block, MaterialCategory.Rock);
	}
}
