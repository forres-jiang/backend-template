using System;

namespace My.XXX.Infra
{
    public class NotConfigurationException : Exception
    {
        public NotConfigurationException(string message) : base(message) { }
    }
}