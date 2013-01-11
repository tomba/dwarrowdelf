using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
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

		public override string ToString()
		{
			return m_mask.ToString();
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

		public override string ToString()
		{
			return m_mask.ToString();
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

		public override string ToString()
		{
			if (m_mask == null)
				return "null";

			StringBuilder sb = new StringBuilder(m_mask.Count);

			foreach (bool b in m_mask)
				sb.Append(b ? '1' : '0');

			return sb.ToString();
		}
	}
}
