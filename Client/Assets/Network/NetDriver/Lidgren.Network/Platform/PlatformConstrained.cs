﻿#if __CONSTRAINED__ || UNITY_STANDALONE_LINUX
using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;

#pragma warning disable 3021
namespace Lidgren.Network
{
	public static partial class NetUtility
	{
		private static byte[] s_randomMacBytes;
		
		static NetUtility()
		{
		}

		[CLSCompliant(false)]
		public static ulong GetPlatformSeed(int seedInc)
		{
			ulong seed = (ulong)Environment.TickCount + (ulong)seedInc;
			return seed ^ ((ulong)(new object().GetHashCode()) << 32);
		}
		

#pragma warning disable 162
		/// <summary>
		/// Gets my local IPv4 address (not necessarily external) and subnet mask
		/// </summary>
		public static IPAddress GetMyAddress(out IPAddress mask)
		{
			mask = null;
#if UNITY_ANDROID || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_IOS || UNITY_EDITOR
			try
			{
				if (!(UnityEngine.Application.internetReachability == UnityEngine.NetworkReachability.NotReachable))
				{
					return null;
				}
                //return IPAddress.Parse(UnityEngine.Network.player.externalIP);
                return null;
			}
			catch // Catch Access Denied errors
			{
				return null;
			}
#endif
			return null;
		}
#pragma warning restore 162

		public static byte[] GetMacAddressBytes()
		{
			if (s_randomMacBytes == null)
			{
				s_randomMacBytes = new byte[8];
				MWCRandom.Instance.NextBytes(s_randomMacBytes);
			}
			return s_randomMacBytes;
		}

		public static IPAddress GetBroadcastAddress()
		{
			return IPAddress.Broadcast;
		}

		public static void Sleep(int milliseconds)
		{
			System.Threading.Thread.Sleep(milliseconds);
		}

		public static IPAddress CreateAddressFromBytes(byte[] bytes)
		{
			return new IPAddress(bytes);
		}

		private static readonly SHA1 s_sha = SHA1.Create();
		public static byte[] ComputeSHAHash(byte[] bytes, int offset, int count)
		{
			return s_sha.ComputeHash(bytes, offset, count);
		}
	}

	public static partial class NetTime
	{
		private static readonly long s_timeInitialized = Environment.TickCount;
		
		/// <summary>
		/// Get number of seconds since the application started
		/// </summary>
		public static double Now { get { return (double)((uint)Environment.TickCount - s_timeInitialized) / 1000.0; } }
	}
}
#pragma warning restore 3021

#endif

