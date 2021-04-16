using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

using PacketSequenceNumber = System.UInt16;

namespace core
{
    public class InFlightPacket
    {
        PacketSequenceNumber mSequenceNumber;
        float mTimeDispatched;
        Dictionary<int, TransmissionData> mTransmissionDataMap = new Dictionary<int, TransmissionData>();


        public InFlightPacket(PacketSequenceNumber inSequenceNumber)
        {
            mSequenceNumber = inSequenceNumber;
            mTimeDispatched = Timing.sInstance.GetTimef();
        }

        public PacketSequenceNumber GetSequenceNumber() { return mSequenceNumber; }
        public float GetTimeDispatched() { return mTimeDispatched; }

        public void SetTransmissionData(int inKey, TransmissionData inTransmissionData)
        {
            mTransmissionDataMap[inKey] = inTransmissionData;
        }
        public TransmissionData GetTransmissionData(int inKey)
        {
            TransmissionData v = null;
            if (mTransmissionDataMap.TryGetValue(inKey, out v) == false)
                return null;
            return v;
        }

        public void HandleDeliveryFailure(DeliveryNotificationManager inDeliveryNotificationManager)
        {
            foreach (var pair in mTransmissionDataMap)
            {
                pair.Value.HandleDeliveryFailure(inDeliveryNotificationManager);
            }
        }
        public void HandleDeliverySuccess(DeliveryNotificationManager inDeliveryNotificationManager)
        {
            foreach (var pair in mTransmissionDataMap)
            {
                pair.Value.HandleDeliverySuccess(inDeliveryNotificationManager);
            }
        }



    }
}
