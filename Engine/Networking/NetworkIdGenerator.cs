using System.Threading;
using Steamworks;

namespace MyGame.Engine.Networking;

public static class NetworkIdGenerator
{
	private static ulong _currentId = 0;

	public static ulong GetNext()
	{
		return ++_currentId;
	}

	public static void ResetSequence()
	{
		_currentId = 0;
	}
}
