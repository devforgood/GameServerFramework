using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

using PacketSequenceNumber = System.UInt16;

namespace core
{
    public class AckRange
    {
        PacketSequenceNumber mStart;
        UInt32 mCount;


        public AckRange()
        {
            mStart = 0;
            mCount = 0;
        }

        public AckRange(PacketSequenceNumber inStart)
        {
            mStart = inStart;
            mCount = 1;
        }

        //if this is the next in sequence, just extend the range
        public bool ExtendIfShould(PacketSequenceNumber inSequenceNumber)
        {
            if (inSequenceNumber == mStart + mCount)
            {
                ++mCount;
                return true;
            }
            else
            {
                return false;
            }
        }

        public PacketSequenceNumber GetStart() { return mStart; }

        public UInt32 GetCount() { return mCount; }

        public void Write(NetOutgoingMessage inOutputStream )
        {
	        inOutputStream.Write( mStart );
	        bool hasCount = mCount > 1;
            inOutputStream.Write( hasCount );
	        if( hasCount )
	        {
		        //most you can ack is 255...
                UInt32 countMinusOne = mCount - 1;
                Byte countToAck = (Byte)(countMinusOne > 255 ? 255 : (countMinusOne));
                inOutputStream.Write( countToAck );
	        }
        }

        public void Read(NetIncomingMessage inInputStream )
        {
            mStart = inInputStream.ReadUInt16();
            bool hasCount = inInputStream.ReadBoolean();
            if (hasCount)
            {
                Byte countMinusOne = inInputStream.ReadByte();
                mCount = (UInt32)(countMinusOne + 1);
            }
            else
            {
                //default!
                mCount = 1;
            }
        }
    }
}
