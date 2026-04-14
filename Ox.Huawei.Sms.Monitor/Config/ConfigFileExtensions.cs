using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Ox.Huawei.Sms.Monitor.Config
{
	/// <summary>
	/// Extension methods to help loading of config
	/// </summary>
	internal static class ConfigFileExtensions
	{
		private const string CONFIG_FILE = "smsmonitor.json";

		/// <summary>
		/// Searches Linux specific locations and loads the first encountered safely
		/// </summary>
		public static HostApplicationBuilder AddLinuxConfigFile(this HostApplicationBuilder builder)
		{
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				/* In Linux, we may have a config file that is in /etc/smsmonitor.json
				 * But we may also have this presented via systemd's credential directory
				 * We're going to check in order:
				 * - Systemd
				 * - ~/.config
				 * - /etc
				 */

				string[] paths = [
					Path.Combine(Environment.GetEnvironmentVariable("CREDENTIALS_DIRECTORY") ?? "/etc", CONFIG_FILE),
					Path.Combine("~/.config", CONFIG_FILE),
					$"/etc/{CONFIG_FILE}"
					];

				for (int i = 0; i < paths.Length; i++)
				{
					if (CanReadFile(paths[i]))
					{
						builder.Configuration.AddJsonFile(paths[i], optional: true, reloadOnChange: false);
						Console.WriteLine($"Adding configuration from :{paths[i]}");
						break;
					}
				}
			}

			return builder;
		}

		/// <summary>
		/// Determines whether a file can be opened for reading
		/// </summary>
		/// <param name="path">Path of file to open</param>
		/// <returns>true on success</returns>
		public static bool CanReadFile(string path)
		{
			// We can mess about with platform specific methods for manually checking permisisons, or we can just
			// try and open the file that we're going to open anyway.
			try
			{
				using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				return true;
			}
			catch(Exception ex) 
			when (ex is FileNotFoundException 
				or UnauthorizedAccessException 
				or IOException)
			{
				return false;
			}
		}
	}
}
