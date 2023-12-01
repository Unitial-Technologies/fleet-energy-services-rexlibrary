using Cloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinIOFileConvert.Providers
{
    internal class MinIOLogProvider : ILogProvider
    {
        public void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
