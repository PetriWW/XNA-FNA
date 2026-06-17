using System;
using System.Runtime.ExceptionServices;
using MyGame.Engine.Networking;
using MyGame.Engine.Core;

namespace MyGame;

public static class Program
{
	[STAThread]
	public static void Main()
	{
		EngineLogger.Initialize();

		// Catches hard fatal crashes
		AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
		{
			Exception ex = (Exception)args.ExceptionObject;
			EngineLogger.LogError("FATAL ENGINE CRASH", ex);
			EngineLogger.Shutdown();
		};

		// ARCHITECTURE FIX: Catches silent/swallowed background thread errors
		AppDomain.CurrentDomain.FirstChanceException += (sender, eventArgs) =>
		{
			// Only log it, don't crash the engine for handled exceptions
			EngineLogger.Log($"FirstChanceException detected: {eventArgs.Exception.Message}", "DIAGNOSTIC");
		};

		SteamManager.Initialize();

		using var game = new Game1();
		game.Run();
	}
}
