using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Globalization;

namespace Dwarrowdelf
{
	public sealed class ItemCategoryMaskConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == null) || !(value is ItemCategoryMask) || destinationType != typeof(string))
				return base.ConvertTo(context, culture, value, destinationType);

			var mask = (ItemCategoryMask)value;

			return string.Join(", ", mask.EnumValues);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				throw base.GetConvertFromException(value);

			string source = value as string;

			if (source == null)
				return base.ConvertFrom(context, culture, value);

			var strs = source.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

			var categories = new List<ItemCategory>();

			foreach (var str in strs)
			{
				ItemCategory c;

				if (Enum.TryParse(str, out c) == false)
					throw base.GetConvertFromException(value);

				categories.Add(c);
			}

			return new ItemCategoryMask(categories);
		}
	}

	public sealed class MaterialCategoryMaskConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == null) || !(value is MaterialCategoryMask) || destinationType != typeof(string))
				return base.ConvertTo(context, culture, value, destinationType);

			var mask = (MaterialCategoryMask)value;

			return string.Join(", ", mask.EnumValues);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				throw base.GetConvertFromException(value);

			string source = value as string;

			if (source == null)
				return base.ConvertFrom(context, culture, value);

			var strs = source.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

			var categories = new List<MaterialCategory>();

			foreach (var str in strs)
			{
				MaterialCategory c;

				if (Enum.TryParse(str, out c) == false)
					throw base.GetConvertFromException(value);

				categories.Add(c);
			}

			return new MaterialCategoryMask(categories);
		}
	}
}
