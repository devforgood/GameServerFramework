﻿
using System.Collections.Generic;

public abstract class ACListContainer<TCONTAINER> : List<TCONTAINER>
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// ACListContainer()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//		생성자
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public ACListContainer( int nCapacity ) : base( nCapacity ) {}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Add()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public new void Add( TCONTAINER item )
	{
		//ACDebug.Assert( base.Contains( item ) == false, EACError.AC_ALREADY_EXIST_VALUE );

		if( item == null )
		{
			return;
		}

		base.Add( item );

		OnAdd( item );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// OnAdd()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected abstract void OnAdd( TCONTAINER item );

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Remove()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public new void Remove( TCONTAINER item )
	{
		//ACDebug.Assert( base.Contains( item ) != false, EACError.AC_NOT_EXIST_VALUE );

		base.Remove( item );

		OnRemove( item );
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// OnRemove()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected abstract void OnRemove( TCONTAINER item );

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Clear()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public new void Clear()
	{
		OnClear();

		base.Clear();
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// OnClear()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected virtual void OnClear()
	{
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Destroy()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public void Destroy()
	{
		Shutdown();
		this.Clear();
	}

	////////////////////////////////////////////////////////////////////////////////////////////////////
	// Shutdown()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	protected virtual void Shutdown()
	{
	}
}