using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetworkLibrary.Connection;
using StructureMap;
using StructureMap.Configuration.DSL;
using SharpTCP.Manager;

namespace SharpTCP.Core
{
	public class SharpRegistry : Registry
	{
		public SharpRegistry()
		{
			For<INetworkConnectionHost>().Singleton().Use<SharpHost>();
			For<INetworkConnectionClient>().Singleton().Use<SharpClient>();
			For<INetworkConnectionManager>().Use<SharpManager>();
		}
	}
}
