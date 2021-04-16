using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public class NetworkHelper
    {

		public static System.Net.IPAddress GetIPAddress(string hostname)
        {
			var addresses = System.Net.Dns.GetHostAddresses(hostname);
			if (addresses.Length > 0)
			{
				for (int i = 0; i < addresses.Length; ++i)
				{
					core.LogHelper.LogInfo($"hostname to ip : {addresses[i]}, AddressFamily:{addresses[i].AddressFamily}, IsIPv4MappedToIPv6:{addresses[i].IsIPv4MappedToIPv6}, IsIPv6SiteLocal:{addresses[i].IsIPv6SiteLocal},IsIPv6Multicast:{addresses[i].IsIPv6Multicast},IsIPv6LinkLocal:{addresses[i].IsIPv6LinkLocal},IsIPv6Teredo:{addresses[i].IsIPv6Teredo},");
					//if (addresses[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
     //               if (addresses[i].AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
     //               {
     //                       return addresses[i];
					//}
				}

				return addresses[0];
			}

			return null;
		}

		static System.Net.IPAddress GetIPAddress(string[] ep)
        {
			System.Net.IPAddress ip;
			if (ep.Length > 2)
			{
				if (!System.Net.IPAddress.TryParse(string.Join(":", ep, 0, ep.Length - 1), out ip))
				{
					return null;
				}
			}
			else
			{
				if (!System.Net.IPAddress.TryParse(ep[0], out ip))
				{
					return null;
				}
			}
			return ip;
		}
		public static  System.Net.IPEndPoint CreateIPEndPoint(string endPoint)
		{
			try
			{
				string[] ep = endPoint.Split(':');
				if (ep.Length < 2)
					throw new FormatException("Invalid endpoint format");

				System.Net.IPAddress ip = GetIPAddress(ep);
				if (ip == null)
				{
					ip = GetIPAddress(ep[0]);
					if (ip == null)
					{
						throw new FormatException("Invalid ip-adress");
					}
				}

				int port;
				if (!int.TryParse(ep[ep.Length - 1], System.Globalization.NumberStyles.None, System.Globalization.NumberFormatInfo.CurrentInfo, out port))
				{
					throw new FormatException("Invalid port");
				}

				core.LogHelper.LogInfo($"ip : {ip}, port : {port}");
				return new System.Net.IPEndPoint(ip, port);
			}
			catch(Exception ex)
			{
				core.LogHelper.LogError(ex.ToString());
				return null;
			}
		}

	}
}
