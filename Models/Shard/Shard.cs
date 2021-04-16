using System;
using System.Collections.Generic;
using System.Text;


public class Shard
{
    static int maxShading = 2;
    public static int MaxSharding
    { 
        get { return maxShading; }
        set { maxShading = value; }
    }

    public static int GetShardId(long member_no)
    {
        return (int)(member_no % MaxSharding);
    }
}

