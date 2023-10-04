using Amazon.Lambda.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWSLambdaFileConvert.ExportFormats
{
    internal static class Mdf
    {
        public static ILambdaContext? Context { get; set; } //Used to write information to log filesS

        public static Stream? MdfStream { get; set; }
    }
}
