using System;
using System.Collections;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TinyHalflifeServer.UDP
{
    internal class RawSender
    {
        //char* srcIp, int srcPort, char* dstIp, int dstPort, char* buffer, int buffersize
        [DllImport("RawUDPSender.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int SendUDPRequest(IntPtr srcIp, int srcPort, IntPtr dstIp, int dstPort, IntPtr buffer, int buffersize);

        /// <summary>
        /// Send UDP Request
        /// </summary>
        /// <param name="srcIp">Source IP</param>
        /// <param name="srcPort">Source Port</param>
        /// <param name="dstIp">Destination IP</param>
        /// <param name="dstPort">Destination Port</param>
        /// <param name="buffer">Data without udp header</param>
        /// <returns></returns>
        public static int Send(string srcIp, int srcPort, string dstIp, int dstPort, byte[] buffer)
        {
            IntPtr srcIpNative = Marshal.StringToHGlobalAnsi(srcIp);
            IntPtr dstIpNative = Marshal.StringToHGlobalAnsi(srcIp);
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            IntPtr dataPtr = handle.AddrOfPinnedObject();
            int size = buffer.Length;

            int ret = SendUDPRequest(srcIpNative, srcPort, dstIpNative, dstPort, dataPtr, size);

            handle.Free();
            Marshal.FreeHGlobal(dstIpNative);
            Marshal.FreeHGlobal(srcIpNative);

            return ret;
        }
    }
}
