using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace core
{
    public enum TransmissionDataType
    {
        kReplicationManager,
    }

    public abstract class TransmissionData
    {
        public abstract void HandleDeliveryFailure(DeliveryNotificationManager inDeliveryNotificationManager);
        public abstract void HandleDeliverySuccess(DeliveryNotificationManager inDeliveryNotificationManager);
    }
}
