using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using StructureMap;
using StructureMap.Configuration.DSL;

namespace NetworkLibrary.Core
{
	/// <summary>
	/// A class implementing StructureMap Registry which takes care of registering all default
	/// values to the StructureMap.
	/// </summary>
	public class NetworkRegistry : Registry
	{
		/// <summary>
		/// Initialise a new instance of NetworkRegistry. You should not need to manually initialise this
		/// since StructureMap does this automatically.
		/// </summary>
		public NetworkRegistry()
		{
			//Register the NetworkDataHandler.
			For<INetworkDataHandler>().Singleton().Use<NetworkDataHandler>();
			Scan(y =>
			{
				//Find valid directories in the application path and load all plugins within it.
				if (Directory.Exists("plugin"))
					y.AssembliesFromPath("plugin");
				if (Directory.Exists("plugins"))
					y.AssembliesFromPath("plugins");
				//Look for Registry classes found in any of the plugins to register it with StructureMap.
				y.LookForRegistries();
			});
		}
	}
}
