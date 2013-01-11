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
		static int s_numBits;
		uint m_mask;

		static EnumBitMask32()
		{
			var max = EnumHelpers.GetEnumMax<TEnum>();
			if (max > 32)
				throw new Exception();
			s_numBits = max;
		}

		public EnumBitMask32()
		{
			m_mask = 0;
		}

		public EnumBitMask32(TEnum enumValue)
		{
			if (EnumConv.ToInt32(enumValue) != 0)
				m_mask = EnumToBit(enumValue);
			else
				m_mask = 0;
		}

		public EnumBitMask32(IEnumerable<TEnum> enumValues)
		{
			uint mask = 0;

			if (enumValues != null)
			{
				foreach (TEnum e in enumValues)
					mask |= EnumToBit(e);
			}

			m_mask = mask;
		}

		public bool Get(TEnum enumValue)
		{
			return m_mask == 0 || (m_mask & EnumToBit(enumValue)) != 0;
		}

		public IEnumerable<TEnum> EnumValues
		{
			get
			{
				for (int i = 0; i < s_numBits; ++i)
				{
					if (m_mask == 0 || (m_mask & (1U << i)) != 0)
						yield return EnumConv.ToEnum<TEnum>(i + 1);
				}
			}
		}

		uint EnumToBit(TEnum e)
		{
			return 1U << (EnumConv.ToInt32(e) - 1);
		}

		public override string ToString()
		{
			if (m_mask == 0)
				return "null";

			var sb = new StringBuilder("0b", s_numBits + 2);

			bool zeroes = true;

			for (int i = s_numBits - 1; i >= 0; --i)
			{
				bool b = (m_mask & (1U << i)) != 0;

				if (zeroes)
				{
					if (!b)
						continue;

					zeroes = false;
				}

				sb.Append(b ? '1' : '0');
			}

			return sb.ToString();
		}
	}

	[Serializable]
	public class EnumBitMask64<TEnum>
	{
		static int s_numBits;
		ulong m_mask;

		static EnumBitMask64()
		{
			var max = EnumHelpers.GetEnumMax<TEnum>();
			if (max > 64)
				throw new Exception();
			s_numBits = max;
		}

		public EnumBitMask64()
		{
			m_mask = 0;
		}

		public EnumBitMask64(TEnum enumValue)
		{
			if (EnumConv.ToInt32(enumValue) != 0)
				m_mask = EnumToBit(enumValue);
			else
				m_mask = 0;
		}

		public EnumBitMask64(IEnumerable<TEnum> enumValues)
		{
			ulong mask = 0;

			if (enumValues != null)
			{
				foreach (TEnum e in enumValues)
					mask |= EnumToBit(e);
			}

			m_mask = mask;
		}

		public bool Get(TEnum enumValue)
		{
			return m_mask == 0 || (m_mask & EnumToBit(enumValue)) != 0;
		}

		public IEnumerable<TEnum> EnumValues
		{
			get
			{
				for (int i = 0; i < s_numBits; ++i)
				{
					if (m_mask == 0 || (m_mask & (1UL << i)) != 0)
						yield return EnumConv.ToEnum<TEnum>(i + 1);
				}
			}
		}

		ulong EnumToBit(TEnum e)
		{
			return 1UL << (EnumConv.ToInt32(e) - 1);
		}

		public override string ToString()
		{
			if (m_mask == 0)
				return "null";

			var sb = new StringBuilder("0b", s_numBits + 2);

			bool zeroes = true;

			for (int i = s_numBits - 1; i >= 0; --i)
			{
				bool b = (m_mask & (1UL << i)) != 0;

				if (zeroes)
				{
					if (!b)
						continue;

					zeroes = false;
				}

				sb.Append(b ? '1' : '0');
			}

			return sb.ToString();
		}
	}

	[Serializable]
	public class EnumBitMask<TEnum>
	{
		static int s_numBits;
		BitArray m_mask;

		static EnumBitMask()
		{
			s_numBits = EnumHelpers.GetEnumMax<TEnum>();
		}

		public EnumBitMask()
		{
			m_mask = null;
		}

		public EnumBitMask(TEnum enumValue)
		{
			BitArray mask = null;

			if (EnumConv.ToInt32(enumValue) != 0)
			{
				mask = new BitArray(s_numBits);
				mask.Set(EnumConv.ToInt32(enumValue) - 1, true);
			}

			m_mask = mask;
		}

		public EnumBitMask(IEnumerable<TEnum> enumValues)
		{
			BitArray mask = null;

			if (enumValues != null && enumValues.Any())
			{
				mask = new BitArray(s_numBits);
				foreach (TEnum e in enumValues)
					mask.Set(EnumConv.ToInt32(e) - 1, true);
			}

			m_mask = mask;
		}

		public bool Get(TEnum enumValue)
		{
			return m_mask == null || m_mask.Get(EnumConv.ToInt32(enumValue) - 1);
		}

		public IEnumerable<TEnum> EnumValues
		{
			get
			{
				for (int i = 0; i < s_numBits; ++i)
				{
					if (m_mask == null || m_mask.Get(i))
						yield return EnumConv.ToEnum<TEnum>(i + 1);
				}
			}
		}

		public override string ToString()
		{
			if (m_mask == null)
				return "null";

			var sb = new StringBuilder("0b", s_numBits + 2);

			bool zeroes = true;

			for (int i = s_numBits - 1; i >= 0; --i)
			{
				bool b = m_mask.Get(i);

				if (zeroes)
				{
					if (!b)
						continue;

					zeroes = false;
				}

				sb.Append(b ? '1' : '0');
			}

			return sb.ToString();
		}
	}
}
