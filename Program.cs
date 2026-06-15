using System;
using MyGame.Engine.Networking;

namespace MyGame;

public static class Program
{
	[STAThread]
	public static void Main()
	{
		// ARCHITECTURE FIX: Restored Vulkan as the primary graphics API.
		// We will keep the validation layer bypasses active to aggressively block
		// standard hook injections (like Steam overlay) from panicking the Vulkan instance.
		Environment.SetEnvironmentVariable("VK_INSTANCE_LAYERS", "");
		Environment.SetEnvironmentVariable("DISABLE_VULKAN_VALIDATION_LAYERS", "1");
		Environment.SetEnvironmentVariable("FNA3D_VULKAN_VALIDATION", "0");
		Environment.SetEnvironmentVariable("DISABLE_VK_LAYER_discord_overlay", "1");

		SteamManager.Initialize();

		using var game = new Game1();
		game.Run();
	}
}
