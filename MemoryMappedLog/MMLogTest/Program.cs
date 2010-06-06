using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MemoryMappedLog
{
    class Program
    {
        static void Main(string[] args)
        {
            int i = 0;

            while (true)
            {
                MMLog.Append("asd", "kala", String.Format("qwe {0}", i++));
                System.Threading.Thread.Sleep(500);
            }
        }
    }
}
