using System;

namespace My.XXX.Shared
{
    public class NotConfigurationException : Exception
    {
        public NotConfigurationException(string message) : base(message) { }
    }
}