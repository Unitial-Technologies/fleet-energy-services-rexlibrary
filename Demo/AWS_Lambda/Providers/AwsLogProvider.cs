using Amazon.Lambda.Core;
using Cloud;

namespace AWSLambdaFileConvert.Providers
{
    internal class AwsLogProvider : ILogProvider
    {
        ILambdaContext _context;
        public AwsLogProvider(ILambdaContext context)
        {
            _context = context;
        }
        public void Log(string message)
        {
            _context?.Logger.Log(message);
        }
    }
}
