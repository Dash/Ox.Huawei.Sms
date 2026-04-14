using System.Reflection;
using System.Text;

namespace Ox.Huawei.Sms.SendCli
{
	internal class Program
	{
		protected Program() { }

		static async Task Main(string[] args)
		{

			Options options = ParseOptions(args);

			if (options.valid)
			{

				using HttpClient http = ApiClient.CreateHttpClient(ref options.BaseAddress);
				var api = new ApiClient(http);

				var smsClient = new SmsClient(api);

				using var input = Console.OpenStandardInput();
				string? buffer;
				StringBuilder stringBuilder = new();
				while ((buffer = await Console.In.ReadLineAsync()) != null)
				{
					if (buffer.Length == 0 && !Console.IsInputRedirected)
						break;
					stringBuilder.AppendLine(buffer);
				}

				foreach (string to in options.to)
				{
					await smsClient.Send(to, stringBuilder.ToString());
				}
			}
		}

		static Options ParseOptions(string[] args)
		{
			var options = new Options();
			for (int i = 0; i < args.Length; i++)
			{
				switch (args[i])
				{
					case "-v":
						Console.WriteLine($"Version: {typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion}");
						break;
					case "-b":
						options.BaseAddress = args[i + 1];
						break;
					case "-t":
						options.to = args[i + 1].Split(',');

						// Look for any disallowed characters
						if(options.to.Any(to => to.Any(c => !char.IsDigit(c) && c != ' ' && c != '+')))
						{
							options.to = null!;
							Console.WriteLine("Error: Invalid phone number format.");
						}
						break;

				}
			}

			options.BaseAddress ??= "http://192.168.8.1/api/";

			if (options.to == null)
			{
				Console.WriteLine($"Usage: {System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name} -t <number> [OPTIONS]\n");
				Console.WriteLine("\t-v\tPrint version number.");
				Console.WriteLine("\t-t\tComma separated list of telephone numbers to send to.");
				Console.WriteLine("\t-b\tOverride http base address URL.");
				Console.WriteLine("\nMessage content is captured from standard input, blank line to end input. Piped input supported.");
				options.valid = false;
			}
			else
			{
				options.valid = true;
			}

			return options;
		}
	}
}
