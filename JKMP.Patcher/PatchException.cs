using System;

namespace JKMP.Patcher
{
    public class PatchException : Exception
    {
        public PatchException(string message) : base(message)
        {
        }
    }
}