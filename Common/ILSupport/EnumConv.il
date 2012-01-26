// Compile with:
// ilasm /dll /optimize /nologo /quiet /output=Dwarrowdelf.Common.ILSupport.dll EnumConv.il

.assembly extern mscorlib { }

.assembly Dwarrowdelf.Common.ILSupport {}

.class public abstract auto ansi sealed beforefieldinit Dwarrowdelf.EnumConv
	   extends [mscorlib]System.Object
{
	.method public hidebysig static int32 ToInt32<T>(!!T 'value') cil managed
	{
		.maxstack 1
		ldarg.0
		ret
	}
	
	.method public hidebysig static !!T ToEnum<T>(int32 'value') cil managed
	{
		.maxstack 1
		ldarg.0
		ret
	} 
}
