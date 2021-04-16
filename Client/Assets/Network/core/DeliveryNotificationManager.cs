using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using PacketSequenceNumber = System.UInt16;

namespace core
{
    public class DeliveryNotificationManager
    {
        public static float MinDelayBeforeAckTimeout = 0.5f;
        public static float MaxDelayBeforeAckTimeout = 1.5f;


        PacketSequenceNumber mNextOutgoingSequenceNumber;
        PacketSequenceNumber mNextExpectedSequenceNumber;

        Queue<InFlightPacket> mInFlightPackets = new Queue<InFlightPacket>();
        Queue<AckRange> mPendingAcks = new Queue<AckRange>();

        bool mShouldSendAcks;
        bool mShouldProcessAcks;

        UInt32 mDeliveredPacketCount;
        UInt32 mDroppedPacketCount;
        UInt32 mDispatchedPacketCount;

        public float DelayBeforeAckTimeout;

        public DeliveryNotificationManager(bool inShouldSendAcks, bool inShouldProcessAcks)
        {
            mNextOutgoingSequenceNumber = 0;
            mNextExpectedSequenceNumber = 0;
            //everybody starts at 0...
            mShouldSendAcks = inShouldSendAcks;
            mShouldProcessAcks = inShouldProcessAcks;
            mDeliveredPacketCount = 0;
            mDroppedPacketCount = 0;
            mDispatchedPacketCount = 0;

            DelayBeforeAckTimeout = MinDelayBeforeAckTimeout;
        }

        ~DeliveryNotificationManager()
        {
            //LOG("DNM destructor. Delivery rate %d%%, Drop rate %d%%",
            //    (100 * mDeliveredPacketCount) / mDispatchedPacketCount,
            //    (100 * mDroppedPacketCount) / mDispatchedPacketCount);
        }


        public InFlightPacket WriteState(NetOutgoingMessage inOutputStream)
        {
            InFlightPacket toRet = WriteSequenceNumber(inOutputStream);
            if (mShouldSendAcks)
            {

                WriteAckData(inOutputStream);
            }
            return toRet;
        }

        public bool ReadAndProcessState(NetIncomingMessage inInputStream)
        {
            bool toRet = ProcessSequenceNumber(inInputStream);
            if (mShouldProcessAcks)
            {
                ProcessAcks(inInputStream);
            }
            return toRet;
        }

        public UInt32 GetDroppedPacketCount() { return mDroppedPacketCount; }
        public UInt32 GetDeliveredPacketCount() { return mDeliveredPacketCount; }
        public UInt32 GetDispatchedPacketCount() { return mDispatchedPacketCount; }

        public Queue<InFlightPacket>	GetInFlightPackets()    { return mInFlightPackets; }


        public InFlightPacket WriteSequenceNumber(NetOutgoingMessage inOutputStream)
        {
            //write the sequence number, but also create an inflight packet for this...
            PacketSequenceNumber sequenceNumber = mNextOutgoingSequenceNumber++;
            inOutputStream.Write(sequenceNumber);
            //LogHelper.LogInfo($"WriteSequenceNumber sequenceNumber : {sequenceNumber}");


            ++mDispatchedPacketCount;

            if (mShouldProcessAcks)
            {
                var v = new InFlightPacket(sequenceNumber);
                mInFlightPackets.Enqueue(v);
                return v;
            }
            else
            {
                return null;
            }
        }


        public void WriteAckData(NetOutgoingMessage inOutputStream)
        {
            //we usually will only have one packet to ack
            //so we'll follow that with a 0 bit if that's the case
            //however, if we have more than 1, we'll make that 1 bit a 1 and then write 8 bits of how many packets
            //we could do some statistical analysis to determine if this is the best strategy but we'll use it for now

            //do we have any pending acks?
            //if so, write a 1 bit and write the first range
            //otherwise, write 0 bit
            bool hasAcks = (mPendingAcks.Count > 0);

            inOutputStream.Write(hasAcks);
            if (hasAcks)
            {
                //note, we could write all the acks
                mPendingAcks.Peek().Write(inOutputStream);
                mPendingAcks.Dequeue();
            }
        }

        //returns wether to drop the packet- if sequence number is too low!
        public bool ProcessSequenceNumber(NetIncomingMessage inInputStream)
        {
            PacketSequenceNumber sequenceNumber = inInputStream.ReadUInt16();
            //LogHelper.LogInfo($"ProcessSequenceNumber sequenceNumber : {sequenceNumber}");

            if (sequenceNumber == mNextExpectedSequenceNumber)
            {
                mNextExpectedSequenceNumber = (ushort)(sequenceNumber + 1);
                //is this what we expect? great, let's add an ack to our pending list
                if (mShouldSendAcks)
                {
                    AddPendingAck(sequenceNumber);
                }
                //and let's continue processing this packet...
                return true;
            }
            //is the sequence number less than our current expected sequence? silently drop it.
            //if this is due to wrapping around, we might fail to ack some packets that we should ack, but they'll get resent, so it's not a big deal
            //note that we don't have to re-ack it because our system doesn't reuse sequence numbers
            else if (sequenceNumber < mNextExpectedSequenceNumber)
            {
                return false;
            }
            else if (sequenceNumber > mNextExpectedSequenceNumber)
            {
                //we missed a lot of packets!
                //so our next expected packet comes after this one...
                mNextExpectedSequenceNumber = (ushort)(sequenceNumber + 1);
                //we should nack the missing packets..this will happen automatically inside AddPendingAck because
                //we're adding an unconsequitive ack
                //and then we can ack this and process it
                if (mShouldSendAcks)
                {
                    AddPendingAck(sequenceNumber);
                }
                return true;
            }

            //drop packet if we couldn't even read sequence number!
            return false;
        }

        //in each packet we can ack a range
        //anything in flight before the range will be considered nackd by the other side immediately
        public void ProcessAcks(NetIncomingMessage inInputStream)
        {

            bool hasAcks = inInputStream.ReadBoolean();
            if (hasAcks)
            {
                AckRange ackRange = new AckRange();
                ackRange.Read(inInputStream);

                //for each InfilghtPacket with a sequence number less than the start, handle delivery failure...
                PacketSequenceNumber nextAckdSequenceNumber = ackRange.GetStart();
                UInt32 onePastAckdSequenceNumber = nextAckdSequenceNumber + ackRange.GetCount();
                while (nextAckdSequenceNumber < onePastAckdSequenceNumber && mInFlightPackets.Count != 0)
                {
                    var nextInFlightPacket = mInFlightPackets.Peek();
                    //if the packet has a lower sequence number, we didn't get an ack for it, so it probably wasn't delivered
                    PacketSequenceNumber nextInFlightPacketSequenceNumber = nextInFlightPacket.GetSequenceNumber();
                    if (nextInFlightPacketSequenceNumber < nextAckdSequenceNumber)
                    {
                        //copy this so we can remove it before handling the failure- we don't want to find it when checking for state
                        var copyOfInFlightPacket = nextInFlightPacket;
                        mInFlightPackets.Dequeue();
                        HandlePacketDeliveryFailure(copyOfInFlightPacket);
                    }
                    else if (nextInFlightPacketSequenceNumber == nextAckdSequenceNumber)
                    {
                        HandlePacketDeliverySuccess(nextInFlightPacket);
                        //received!
                        mInFlightPackets.Dequeue();
                        //decrement count, advance nextAckdSequenceNumber
                        ++nextAckdSequenceNumber;
                    }
                    else if (nextInFlightPacketSequenceNumber > nextAckdSequenceNumber)
                    {
                        //we've already ackd some packets in here.
                        //keep this packet in flight, but keep going through the ack...
                        ++nextAckdSequenceNumber;
                    }
                }
            }
        }

        public void ProcessTimedOutPackets()
        {
            float timeoutTime = Timing.sInstance.GetTimef() - DelayBeforeAckTimeout;

            bool IsClear = true;
            bool HasFlightPackets = mInFlightPackets.Count > 0;
            while (mInFlightPackets.Count != 0)
            {
                var nextInFlightPacket = mInFlightPackets.Peek();

                //was this packet dispatched before the current time minus the timeout duration?
                if (nextInFlightPacket.GetTimeDispatched() < timeoutTime)
                {
                    //it failed! let us know about that
                    HandlePacketDeliveryFailure(nextInFlightPacket);
                    mInFlightPackets.Dequeue();
                    IsClear = false;

                    //LogHelper.LogInfo($"ProcessTimedOutPackets Fail");

                }
                else
                {
                    //LogHelper.LogInfo($"ProcessTimedOutPackets End");

                    //it wasn't, and packets are all in order by time here, so we know we don't have to check farther
                    break;
                }
            }

            if (HasFlightPackets)
            {
                if (IsClear)
                {
                    if (DelayBeforeAckTimeout > MinDelayBeforeAckTimeout)
                        DelayBeforeAckTimeout = MinDelayBeforeAckTimeout;
                }
                else
                {
                    if (DelayBeforeAckTimeout < MaxDelayBeforeAckTimeout)
                    {
                        DelayBeforeAckTimeout += 0.1f;
                        LogHelper.LogInfo($"ProcessTimedOutPackets increase DelayBeforeAckTimeout : {DelayBeforeAckTimeout}");
                    }
                }
                //LogHelper.LogInfo($"ProcessTimedOutPackets {DelayBeforeAckTimeout}");
            }
        }

        public void AddPendingAck(PacketSequenceNumber inSequenceNumber)
        {
            //if you don't have a range yet, or you can't correctly extend the final range with the sequence number,
            //start a new range
            if (mPendingAcks.Count == 0 || !mPendingAcks.LastOrDefault().ExtendIfShould(inSequenceNumber))
            {
                //LogHelper.LogInfo($"AddPendingAck inSequenceNumber : {inSequenceNumber}");
                mPendingAcks.Enqueue(new AckRange(inSequenceNumber));
            }
        }


        public void HandlePacketDeliveryFailure(InFlightPacket inFlightPacket)
        {
            ++mDroppedPacketCount;
            inFlightPacket.HandleDeliveryFailure(this);

        }


        public void HandlePacketDeliverySuccess(InFlightPacket inFlightPacket)
        {
            ++mDeliveredPacketCount;
            inFlightPacket.HandleDeliverySuccess(this);
        }


    }
}
