﻿using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.IO;

namespace core
{
    /*
	 * Welcome, hope you made it here without your IDE crashing (*cough* Rider)
	 * This file is automatically genererated and contains all the RPC overloads.
	 * Conveience methods include up to 32 parameters.
	 * The generation script can be found here (Heads up, it's not pretty)
	 * https://gist.github.com/TwoTenPvP/6dd0fbfa8ec34329c0e219281779c935
	 */
    public partial class NetGameObject
    {
        #region SEND METHODS
#pragma warning disable HAA0101 // Array allocation for params parameter
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
        /// <exclude />
        public delegate void RpcMethod();
        /// <exclude />
        public delegate void RpcMethod<T1>(T1 t1);
        /// <exclude />
        public delegate void RpcMethod<T1, T2>(T1 t1, T2 t2);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3>(T1 t1, T2 t2, T3 t3);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31);
        /// <exclude />
        public delegate void RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult>();
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1>(T1 t1);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2>(T1 t1, T2 t2);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3>(T1 t1, T2 t2, T3 t3);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31);
        /// <exclude />
        public delegate TResult ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32);



        #region BOXED CLIENT RPC
        public void InvokeClientRpc(string methodName, List<int> clientIds, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security);
        }

        public void InvokeClientRpc(RpcMethod method, List<int> clientIds, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security);
        }

        public void InvokeClientRpcOnOwner(RpcMethod method, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security);
        }

        public void InvokeClientRpcOnOwner(string methodName, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security);
        }

        public void InvokeClientRpcOnEveryone(RpcMethod method, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security);
        }

        public void InvokeClientRpcOnEveryone(string methodName, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security);
        }

        public void InvokeClientRpcOnClient(RpcMethod method, int clientId, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security);
        }

        public void InvokeClientRpcOnClient(string methodName, int clientId, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security);
        }

        public void InvokeClientRpcOnEveryoneExcept(RpcMethod method, int clientIdToIgnore, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security);
        }

        public void InvokeClientRpcOnEveryoneExcept(string methodName, int clientIdToIgnore, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security);
        }

        public void InvokeClientRpc<T1>(string methodName, List<int> clientIds, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1);
        }

        public void InvokeClientRpc<T1>(RpcMethod<T1> method, List<int> clientIds, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1);
        }

        public void InvokeClientRpcOnOwner<T1>(RpcMethod<T1> method, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1>(ResponseRpcMethod<TResult, T1> method, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1);
        }

        public void InvokeClientRpcOnOwner<T1>(string methodName, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1>(string methodName, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1);
        }

        public void InvokeClientRpcOnEveryone<T1>(RpcMethod<T1> method, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1);
        }

        public void InvokeClientRpcOnEveryone<T1>(string methodName, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1);
        }

        public void InvokeClientRpcOnClient<T1>(RpcMethod<T1> method, int clientId, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1>(ResponseRpcMethod<TResult, T1> method, int clientId, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1);
        }

        public void InvokeClientRpcOnClient<T1>(string methodName, int clientId, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1>(string methodName, int clientId, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1>(RpcMethod<T1> method, int clientIdToIgnore, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1>(string methodName, int clientIdToIgnore, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1);
        }

        public void InvokeClientRpc<T1, T2>(string methodName, List<int> clientIds, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2);
        }

        public void InvokeClientRpc<T1, T2>(RpcMethod<T1, T2> method, List<int> clientIds, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2);
        }

        public void InvokeClientRpcOnOwner<T1, T2>(RpcMethod<T1, T2> method, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2>(ResponseRpcMethod<TResult, T1, T2> method, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2);
        }

        public void InvokeClientRpcOnOwner<T1, T2>(string methodName, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2>(string methodName, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2);
        }

        public void InvokeClientRpcOnEveryone<T1, T2>(RpcMethod<T1, T2> method, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2);
        }

        public void InvokeClientRpcOnEveryone<T1, T2>(string methodName, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2);
        }

        public void InvokeClientRpcOnClient<T1, T2>(RpcMethod<T1, T2> method, int clientId, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2>(ResponseRpcMethod<TResult, T1, T2> method, int clientId, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2);
        }

        public void InvokeClientRpcOnClient<T1, T2>(string methodName, int clientId, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2>(string methodName, int clientId, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2>(RpcMethod<T1, T2> method, int clientIdToIgnore, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2);
        }

        public void InvokeClientRpc<T1, T2, T3>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3);
        }

        public void InvokeClientRpc<T1, T2, T3>(RpcMethod<T1, T2, T3> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3>(RpcMethod<T1, T2, T3> method, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3>(ResponseRpcMethod<TResult, T1, T2, T3> method, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3>(string methodName, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3>(string methodName, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3>(RpcMethod<T1, T2, T3> method, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3>(string methodName, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3>(RpcMethod<T1, T2, T3> method, int clientId, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3>(ResponseRpcMethod<TResult, T1, T2, T3> method, int clientId, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3>(RpcMethod<T1, T2, T3> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3);
        }

        public void InvokeClientRpc<T1, T2, T3, T4>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpc<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4>(ResponseRpcMethod<TResult, T1, T2, T3, T4> method, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4>(ResponseRpcMethod<TResult, T1, T2, T3, T4> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7>(RpcMethod<T1, T2, T3, T4, T5, T6, T7> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7>(RpcMethod<T1, T2, T3, T4, T5, T6, T7> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7>(RpcMethod<T1, T2, T3, T4, T5, T6, T7> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7>(RpcMethod<T1, T2, T3, T4, T5, T6, T7> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7>(RpcMethod<T1, T2, T3, T4, T5, T6, T7> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeClientRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, List<int> clientIds, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), clientIds, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeClientRpcOnOwner<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public RpcResponse<TResult> InvokeClientRpcOnOwner<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), OwnerClientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethod(method.Method), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeClientRpcOnEveryone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxed(HashMethodName(methodName), null, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethod(method.Method), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeClientRpcOnClient<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToClient(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public RpcResponse<TResult> InvokeClientRpcOnClient<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, int clientId, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendClientRPCBoxedResponse<TResult>(HashMethodName(methodName), clientId, channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeClientRpcOnEveryoneExcept<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, int clientIdToIgnore, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCBoxedToEveryoneExcept(clientIdToIgnore, HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        #endregion

        #region BOXED SERVER RPC
        public void InvokeServerRpc(RpcMethod method, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security);
        }

        public void InvokeServerRpc(string methodName, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult>(ResponseRpcMethod<TResult> method, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult>(string methodName, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security);
        }

        public void InvokeServerRpc<T1>(RpcMethod<T1> method, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1>(ResponseRpcMethod<TResult, T1> method, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1);
        }

        public void InvokeServerRpc<T1>(string methodName, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1>(string methodName, T1 t1, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1);
        }

        public void InvokeServerRpc<T1, T2>(RpcMethod<T1, T2> method, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2>(ResponseRpcMethod<TResult, T1, T2> method, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2);
        }

        public void InvokeServerRpc<T1, T2>(string methodName, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2>(string methodName, T1 t1, T2 t2, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2);
        }

        public void InvokeServerRpc<T1, T2, T3>(RpcMethod<T1, T2, T3> method, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3>(ResponseRpcMethod<TResult, T1, T2, T3> method, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3);
        }

        public void InvokeServerRpc<T1, T2, T3>(string methodName, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3>(string methodName, T1 t1, T2 t2, T3 t3, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3);
        }

        public void InvokeServerRpc<T1, T2, T3, T4>(RpcMethod<T1, T2, T3, T4> method, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4>(ResponseRpcMethod<TResult, T1, T2, T3, T4> method, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4);
        }

        public void InvokeServerRpc<T1, T2, T3, T4>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5>(RpcMethod<T1, T2, T3, T4, T5> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6>(RpcMethod<T1, T2, T3, T4, T5, T6> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7>(RpcMethod<T1, T2, T3, T4, T5, T6, T7> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(RpcMethod<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(ResponseRpcMethod<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32> method, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethod(method.Method), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public void InvokeServerRpc<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCBoxed(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        public RpcResponse<TResult> InvokeServerRpc<TResult, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21, T22, T23, T24, T25, T26, T27, T28, T29, T30, T31, T32>(string methodName, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, T11 t11, T12 t12, T13 t13, T14 t14, T15 t15, T16 t16, T17 t17, T18 t18, T19 t19, T20 t20, T21 t21, T22 t22, T23 t23, T24 t24, T25 t25, T26 t26, T27 t27, T28 t28, T29 t29, T30 t30, T31 t31, T32 t32, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            return SendServerRPCBoxedResponse<TResult>(HashMethodName(methodName), channel, security, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11, t12, t13, t14, t15, t16, t17, t18, t19, t20, t21, t22, t23, t24, t25, t26, t27, t28, t29, t30, t31, t32);
        }

        #endregion
#pragma warning restore HAA0101 // Array allocation for params parameter
#pragma warning restore HAA0601 // Value type to reference type conversion causing boxing allocation
        #region PERFORMANCE CLIENT RPC
  

        public void InvokeClientRpcOnOwnerPerformance(RpcDelegate method, NetBuffer stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCPerformance(HashMethod(method.Method), OwnerClientId, stream, channel, security);
        }
        public void InvokeClientRpcOnClientPerformance(RpcDelegate method, int clientId, NetBuffer stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCPerformance(HashMethod(method.Method), clientId, stream, channel, security);
        }
        public void InvokeClientRpcOnEveryoneExceptPerformance(RpcDelegate method, int clientIdToIgnore, NetBuffer stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCPerformance(HashMethod(method.Method), stream, clientIdToIgnore, channel, security);
        }
        public void InvokeClientRpcOnClientPerformance(string methodName, int clientId, NetBuffer stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCPerformance(HashMethodName(methodName), clientId, stream, channel, security);
        }
        public void InvokeClientRpcOnOwnerPerformance(string methodName, NetBuffer stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCPerformance(HashMethodName(methodName), OwnerClientId, stream, channel, security);
        }
        public void InvokeClientRpcOnEveryoneExceptPerformance(string methodName, int clientIdToIgnore, NetBuffer stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendClientRPCPerformance(HashMethodName(methodName), stream, clientIdToIgnore, channel, security);
        }
        #endregion
        #region PERFORMANCE SERVER RPC

        public void InvokeServerRpcPerformance(RpcDelegate method, NetBuffer stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCPerformance(HashMethod(method.Method), stream, channel, security);
        }
        public void InvokeServerRpcPerformance(string methodName, NetBuffer stream, string channel = null, SecuritySendFlags security = SecuritySendFlags.None)
        {
            SendServerRPCPerformance(HashMethodName(methodName), stream, channel, security);
        }
        #endregion
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

        #endregion
    }
}
