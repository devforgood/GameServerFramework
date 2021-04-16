
using System;

public interface IACPrimaryKey<T> 
	where T : struct, IConvertible, IComparable<T> 
{
	T	VALUE	{ get; }
}

public interface IACPrimaryKey<T1, T2> 
	where T1 : struct, IConvertible, IComparable<T1> 
	where T2 : struct, IConvertible, IComparable<T2>
{
	T1		VALUE_1		{ get; }
	T2		VALUE_2		{ get; }
}