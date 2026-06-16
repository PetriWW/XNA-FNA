using System;
using MyGame.Engine.Networking;
using MyGame.Engine.Core;

namespace MyGame;

public static class Program
{
	[STAThread]
	public static void Main()
	{
		EngineLogger.Initialize();

		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			Exception ex = (Exception)args.ExceptionObject;
			EngineLogger.LogError("FATAL ENGINE CRASH", ex);
			EngineLogger.Shutdown();
		};

		SteamManager.Initialize();

		using var game = new Game1();
		game.Run();
	}
}
