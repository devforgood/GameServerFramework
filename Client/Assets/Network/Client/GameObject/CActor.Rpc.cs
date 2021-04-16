using Lidgren.Network;
using System;
using System.Collections.Generic;
using UnityEngine;


public partial class CActor
{
    [core.ClientRPC]
    public override void PingClient(int number)
    {
        Debug.Log("Ping " + number);
    }

    [core.ClientRPC]
    public override void JumpClient(int power)
    {
#if _USE_BEPU_PHYSICS
        mCharacterController.Jump();
#endif
    }

 
}
