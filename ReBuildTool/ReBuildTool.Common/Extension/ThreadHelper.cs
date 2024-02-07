using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ReBuildTool.Common
{
    public class ThreadHelper
    {
        public static void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }
    }
}
