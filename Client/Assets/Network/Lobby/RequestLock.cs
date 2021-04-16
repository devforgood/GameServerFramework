using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public class RequestLock : IDisposable
{
    static bool isUse = false;

    public RequestLock()
    {
        if(isUse== true)
        {
            throw new Exception("Request lock error");
        }

        isUse = true;
    }

    public void Dispose()
    {
        isUse = false;
    }
}

