using System;

namespace GZipStream
{
    public class WrongCommandException : Exception {
        public WrongCommandException() : base() {}
        public WrongCommandException(string message) : base(message) { }
        public override string ToString()
        {
            return Message;
        }
    }
}
