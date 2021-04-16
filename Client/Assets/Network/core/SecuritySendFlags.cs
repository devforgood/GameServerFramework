using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    [Flags]
    public enum SecuritySendFlags
    {
        /// <summary>
        /// No security operations are applied
        /// </summary>
        None = 0x0,
        /// <summary>
        /// The payload is encrypted
        /// </summary>
        Encrypted = 0x1,
        /// <summary>
        /// The payload is authenticated
        /// </summary>
        Authenticated = 0x2
    }
}
