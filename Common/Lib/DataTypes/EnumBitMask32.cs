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

			foreach (TEnum e in enumValues)
				mask |= EnumToBit(e);

			m_mask = mask;
		}

		public bool Get(TEnum enumValue)
		{
			return (m_mask & EnumToBit(enumValue)) != 0;
		}

		public IEnumerable<TEnum> EnumValues
		{
			get
			{
				if (m_mask == 0)
					yield break;

				for (int i = 0; i < s_numBits; ++i)
				{
					if ((m_mask & (1U << i)) != 0)
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
}
