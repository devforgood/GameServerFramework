using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public partial class CActor
{
     public void DoClientSidePredictionAfterReplicationForRemoteActor(UInt32 inReadState)
    {
        if ((inReadState & (UInt32)ReplicationState.Pose) != 0)
        {

            //simulate movement for an additional RTT
            float rtt = NetworkManagerClient.sInstance.GetRoundTripTime();
            //LOG( "Other cat came in, simulating for an extra %f", rtt );

            // 지연시간 (RTT) 만큼 예측한 방향으로 이동(시뮬레이션)을 하게 되면 벽을 뚫고 갈수 있으므로 최대 델타타임 만큼 잘라서 이동한다.
            float deltaTime = 1.0f / 30.0f;

            while (true)
            {
                if (rtt < deltaTime)
                {
                    SimulateMovement(rtt);
                    break;
                }
                else
                {
                    SimulateMovement(deltaTime);
                    rtt -= deltaTime;
                }
            }
        }
    }
	
    /// <summary>
    /// 상태 동기화에서 예측
    /// </summary>
    /// <param name="readState"></param>
    void PredictionStateSync(UInt32 readState)
    {
        if (!oldLocation.Equals(GetLocation()))
        {
            if ((readState & (UInt32)ReplicationState.PlayerId) == 0)
            {
                var durationOutOfSync = NetworkManagerClient.sInstance.GetRoundTripTime();
                var velocity = GetVelocity() * durationOutOfSync;
                velocity.y = 0f;
                //Debug.Log($"PredictionStateSync velocity  ({velocity.x},{velocity.y},{velocity.z}), durationOutOfSync{durationOutOfSync}");
                //core.LogHelper.LogDrawLine(GetLocation(), GetLocation() + (velocity), new Vector3(1, 1, 1), 1);

                // 이동 예측으로 인해 벽을 뚤고 가는 문제 수정
                RaycastHit hit;
                if (Physics.Raycast(GetLocation(), velocity, out hit, velocity.magnitude))
                {
                    SetLocation(hit.point);
                }
                else
                {
                    SetLocation(GetLocation() + velocity);
                }
            }
        }
    }
}
