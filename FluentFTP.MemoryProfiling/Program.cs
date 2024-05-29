using FluentFTP;
using FluentFTP.Client.BaseClient;
using FluentFTP.GnuTLS;
using FluentFTP.GnuTLS.Enums;

const string serverAddress = "127.0.0.1";
const string serverUser = "ftptest";
const string serverPass = "ftptest";


const int sleepMilliseconds = 1_000;

static void ConfigureConnection(BaseFtpClient conn) {
	// enable GnuTLS streams for FTP client
	conn.Config.CustomStream = typeof(GnuTlsStream);
	conn.Config.CustomStreamConfig = new GnuConfig() {
		LogLevel = 1,

		//// sample setting to use the default security suite
		SecuritySuite = GnuSuite.Normal,

		//// sample setting to include all TLS protocols except for TLS 1.0 and TLS 1.1
		SecurityOptions = [
						new GnuOption(GnuOperator.Include, GnuCommand.Protocol_All)
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
	conn.Config.ValidateAnyCertificate = false;
	conn.Config.ValidateCertificateRevocation = false;
	conn.Config.EncryptionMode = FtpEncryptionMode.Explicit;
	conn.Config.LogToConsole = true;
}

static bool OnError(Exception ex) {
	Console.WriteLine(ex.ToString());
	return true;
}

static async Task SimulateWorkAsync(int target) {
	var count = 0;
	while (count < target) {
		await using (var conn = new AsyncFtpClient(serverAddress, serverUser, serverPass)) {
			ConfigureConnection(conn);

			await Task.Delay(sleepMilliseconds);

			try {
				await conn.Connect();
			}
			catch (Exception ex) {
				if (OnError(ex)) {
					throw;
				}
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
		using (var conn = new FtpClient(serverAddress, serverUser, serverPass)) {
			ConfigureConnection(conn);

			Thread.Sleep(sleepMilliseconds);

			try {
				conn.Connect();
			}
			catch (Exception ex) {
				if (OnError(ex)) {
					throw;
				}
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

