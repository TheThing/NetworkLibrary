using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Exceptions
{
	public class Warning : Exception
	{
		public Warning(string message) : base(message) { }

		public Warning(string message, Exception innerException) : base(message, innerException) { }
	}
}
