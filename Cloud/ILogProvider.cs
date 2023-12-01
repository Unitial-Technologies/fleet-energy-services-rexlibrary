using System;
using System.Collections.Generic;
using System.Text;

namespace Cloud
{
    public interface ILogProvider
    {
        public void Log(string message);
    }
}
