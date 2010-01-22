using System;

namespace wyUpdate.Compression.Vcdiff
{
	[Serializable()]
	public class VcdiffFormatException : Exception
	{
        internal VcdiffFormatException(string message) : base(message) { }
        internal VcdiffFormatException(string message, Exception inner) : base(message, inner) { }
	}
}
