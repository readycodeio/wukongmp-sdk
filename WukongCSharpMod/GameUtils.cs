using b1;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongCSharpMod
{
	public static class GameUtils
	{
		private static UWorld _world;
		private static readonly object _lockObj = new object();
		private static bool _isExecuting = false;

		public static string Name => typeof(GameUtils).Namespace;

		public static UWorld GetWorld()
		{
			if ((UObject)(object)_world == (UObject)null)
			{
				var obj = GCHelper.FindRef(FGlobals.GWorld)?.Managed;
				_world = (UWorld)(object)((obj is UWorld) ? obj : null);
			}
			return _world;
		}

		public static APawn GetControlledPawn()
		{
			return ((AController)UGSE_EngineFuncLib.GetFirstLocalPlayerController((UObject)(object)GetWorld())).GetControlledPawn();
		}

		public static BGUPlayerCharacterCS GetBGUPlayerCharacterCS()
		{
			var controlledPawn = GetControlledPawn();
			return (BGUPlayerCharacterCS)(object)((controlledPawn is BGUPlayerCharacterCS) ? controlledPawn : null);
		}

		public static BGP_PlayerControllerB1 GetPlayerController()
		{
			return (BGP_PlayerControllerB1)UGSE_EngineFuncLib.GetFirstLocalPlayerController((UObject)(object)GetWorld());
		}

		public static BUS_GSEventCollection GetBUS_GSEventCollection()
		{
			return BUS_EventCollectionCS.Get((AActor)(object)GetControlledPawn());
		}

		public static BGUPlayerCharacterCS GetThis()
		{
			var controlledPawn = GetControlledPawn();
			return (BGUPlayerCharacterCS)(object)((controlledPawn is BGUPlayerCharacterCS) ? controlledPawn : null);
		}
	}
}
