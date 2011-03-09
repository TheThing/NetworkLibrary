using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetworkLibrary.Exceptions
{
	public class ParsingWarning : Warning
	{
		public ParsingWarning(string message) : base(message) { }
	}
}
