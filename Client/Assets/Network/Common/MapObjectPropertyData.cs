
public class MapObjectPropertyData : ACDataSync<int, JMapObjectPropertyData>
{
	////////////////////////////////////////////////////////////////////////////////////////////////////
	// MapObjectPropertyData()
	//--------------------------------------------------------------------------------------------------
	//	Desc.
	//
	////////////////////////////////////////////////////////////////////////////////////////////////////
	public MapObjectPropertyData( EACDataType eDataType ) 
		: base( eDataType )
	{
	}

    ////////////////////////////////////////////////////////////////////////////////////////////////////
    // GetData()
    //--------------------------------------------------------------------------------------------------
    //	Desc.
    //
    ////////////////////////////////////////////////////////////////////////////////////////////////////
    public JMapObjectPropertyData GetData(int objectId, int materialtype)
    {
        JMapObjectPropertyData data = null;
        foreach(JMapObjectPropertyData value in this.Values)
        {
            if(value.ObjectId == objectId && value.Materialtype == materialtype)
            {
                data = value;
                break;
            }
        }
        return data;
    }
}
