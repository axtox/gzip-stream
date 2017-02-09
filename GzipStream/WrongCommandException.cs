using System;

namespace GzipStreamDemo
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
