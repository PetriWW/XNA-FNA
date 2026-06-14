using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;

namespace MyGame.Engine.Core;

public static class AudioManager
{
	// Stubs for future memory caches
	private static readonly Dictionary<string, SoundEffect> SoundCache = new();
	private static readonly Dictionary<string, Song> MusicCache = new();

	public static float MasterVolume { get; set; } = 1.0f;
	public static float MusicVolume { get; set; } = 0.5f;
	public static float SfxVolume { get; set; } = 0.8f;

	public static void Initialize()
	{
		MediaPlayer.Volume = MasterVolume * MusicVolume;
		SoundEffect.MasterVolume = MasterVolume * SfxVolume;
		Console.WriteLine("[AudioManager]: Audio subsystem stub initialized.");
	}

	public static void PlaySound(string assetName)
	{
		// TODO: Implement AssetManager hook and playback logic
		Console.WriteLine($"[AudioManager Stub]: Request to play sound -> {assetName}");
	}

	public static void PlayMusic(string assetName, bool loop = true)
	{
		// TODO: Implement background music streaming logic
		Console.WriteLine($"[AudioManager Stub]: Request to play music -> {assetName}");
	}

	public static void UnloadAll()
	{
		foreach (var sfx in SoundCache.Values) sfx.Dispose();
		foreach (var song in MusicCache.Values) song.Dispose();
		SoundCache.Clear();
		MusicCache.Clear();
	}
}
