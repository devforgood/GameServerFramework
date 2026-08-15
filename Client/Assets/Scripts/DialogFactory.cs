// This file is auto-generated. Do not modify directly.

using System;
using UnityEngine;

public static class DialogFactory
{
    public static Dialog Create(int id)
    {
        Dialog obj = null;
        Gamedata.Dialog res = null;
		if(!GameManager.Instance.resource.Dialogs.TryGetValue(id, out res))
		{
			Debug.LogError($"Dialog with ID {id} not found in resource Dialog.");
			return null;
        }

        
        obj = new Dialog();
        

        obj.gamedata = res;

        return obj;
    }
} 