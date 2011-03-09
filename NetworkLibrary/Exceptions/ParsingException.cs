using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Exceptions
{
	public class ParsingException : Exception
	{
		public ParsingException(string message)
			: base(message)
		{
		}

		public ParsingException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
