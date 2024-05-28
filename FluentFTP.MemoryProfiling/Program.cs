using FluentFTP;
using FluentFTP.Client.BaseClient;
using FluentFTP.GnuTLS;
using FluentFTP.GnuTLS.Enums;

static void ConfigureConnection(BaseFtpClient conn) {
	// enable GnuTLS streams for FTP client
	conn.Config.CustomStream = typeof(GnuTlsStream);
	conn.Config.CustomStreamConfig = new GnuConfig() {
		LogLevel = 1,

		//// sample setting to use the default security suite
		SecuritySuite = GnuSuite.Normal,

		//// sample setting to include all TLS protocols except for TLS 1.0 and TLS 1.1
		SecurityOptions = [
						new GnuOption(GnuOperator.Include, GnuCommand.Protocol_All),
						new GnuOption(GnuOperator.Exclude, GnuCommand.Protocol_Tls10),
						new GnuOption(GnuOperator.Exclude, GnuCommand.Protocol_Tls11),
					],

		// no profile required
		SecurityProfile = GnuProfile.None,

		// sample special flags (this is not normally required)
		AdvancedOptions = [
						GnuAdvanced.CompatibilityMode
					],

		HandshakeTimeout = 5000,
	};


	// connect using Explicit FTPS with TLS 1.3
	conn.Config.ValidateAnyCertificate = true;
	conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;
}

static async Task SimulateWorkAsync(int target) {
	var count = 0;
	while (count < target) {
		await using (var conn = new AsyncFtpClient("ftp-server", "ftptest", "ftptest")) {
			ConfigureConnection(conn);

			await Task.Delay(100);

			try {
				await conn.Connect();
			}
			catch /*(Exception ex)*/ {
				// Console.WriteLine(ex.ToString());
			}
		}
		count++;
		if (count % 100 == 0) {
			Console.WriteLine($"Created {count} async clients.");
		}
	}
}

static void SimulateWorkSync(int target) {
	var count = 0;
	while (count < target) {
		using (var conn = new FtpClient("ftp-server", "ftptest", "ftptest")) {
			ConfigureConnection(conn);

			Thread.Sleep(100);

			try {
				conn.Connect();
			}
			catch /*(Exception ex)*/ {
				// Console.WriteLine(ex.ToString());
			}
		}
		count++;
		if (count % 100 == 0) {
			Console.WriteLine($"Created {count} sync clients.");
		}
		
	}
}

static void CollectAndPrint(string stage) {
	var memMB = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
	Console.WriteLine("======================================================");
	Console.WriteLine($"{stage} memory usage before GC: {memMB} MB");
	GC.Collect();
	GC.WaitForPendingFinalizers();
	GC.Collect();
	memMB = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64 / (1024 * 1024);
	Console.WriteLine($"{stage} memory usage after GC: {memMB} MB");
	Console.WriteLine("======================================================");
}

const int targetCount = 500;
Console.WriteLine("Starting to create clients!");

CollectAndPrint("BEFORE_SYNC");
SimulateWorkSync(targetCount);
CollectAndPrint("AFTER_SYNC");

await Task.Delay(TimeSpan.FromMinutes(1));

CollectAndPrint("BEFORE_ASYNC");
await SimulateWorkAsync(targetCount);
CollectAndPrint("AFTER_ASYNC");

Console.WriteLine("Finished Creating clients. Sleeping for 60 minutes before closing");
await Task.Delay(TimeSpan.FromMinutes(60));

