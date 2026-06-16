using System;
using System.IO;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace MyGame.Engine.Core;

public static class EngineLogger
{
	private static Channel<string>? _logChannel;
	private static Task? _writerTask;
	private static StreamWriter? _fileWriter;

	public static void Initialize()
	{
		string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MyGame", "Logs");
		Directory.CreateDirectory(logDir);
		string logFile = Path.Combine(logDir, $"engine_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

		_fileWriter = new StreamWriter(logFile, append: true) { AutoFlush = true };
		_logChannel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });

		_writerTask = Task.Run(async () =>
		{
			await foreach (var msg in _logChannel.Reader.ReadAllAsync())
			{
				await _fileWriter.WriteLineAsync(msg);
			}
		});

		Log("EngineLogger Subsystem Initialized.", "SYSTEM");
	}

	public static void Log(string message, string category = "INFO")
	{
		string formatted = $"[{DateTime.Now:HH:mm:ss.fff}] [{category}] {message}";
		_logChannel?.Writer.TryWrite(formatted);
		Console.WriteLine(formatted);
	}

	public static void LogError(string message, Exception ex)
	{
		Log($"{message} | Exception: {ex.Message}\n{ex.StackTrace}", "ERROR");
	}

	public static void Shutdown()
	{
		if (_logChannel != null)
		{
			_logChannel.Writer.Complete();
			_writerTask?.Wait();
			_fileWriter?.Dispose();
		}
	}
}
