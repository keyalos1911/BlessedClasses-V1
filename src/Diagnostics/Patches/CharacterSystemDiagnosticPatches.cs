using HarmonyLib;
using System;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using BlessedClasses.src;
using BlessedClasses.src.Diagnostics;

namespace BlessedClasses.src.Diagnostics.Patches
{
	[HarmonyPatch]
	public static class CharacterSystemDiagnosticPatches
	{
		private static ILogger Logger => BlessedClassesModSystem.Logger;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Vintagestory.GameContent.CharacterSystem), "setCharacterClass")]
		public static bool Prefix_SetCharacterClass(
			CharacterSystem __instance,
			EntityPlayer eplayer,
			string classCode,
			bool initializeGear)
		{
			try
			{
				string playerName = eplayer.GetName() ?? "Unknown";

				// check if the class code is valid (exists in characterClasses)
				var validClass = __instance.characterClasses?.FirstOrDefault(c => c.Code == classCode);

				if (validClass == null)
				{
					// class doesn't exist! skip original method to prevent crash
					Logger.Warning("[BlessedClasses] Player '{0}' has invalid/disabled class '{1}'. " +
						"This can happen when adding BlessedClasses to an existing save. " +
						"The player should select a new class from the character selection screen.",
						playerName, classCode);

					DiagnosticLogger.LogCharselFailure(classCode, playerName,
						$"Class '{classCode}' not found in available character classes (may be disabled or removed)");

					return false;
				}

				// log the attempt
				DiagnosticLogger.LogCharselAttempt(classCode, playerName);

				Logger.Debug("[BlessedClasses] setCharacterClass called:");
				Logger.Debug("[BlessedClasses]   Player: {0}", playerName);
				Logger.Debug("[BlessedClasses]   Target Class: {0}", classCode);
				Logger.Debug("[BlessedClasses]   Initialize Gear: {0}", initializeGear);

				return true; // Continue to original method
			}
			catch (Exception ex)
			{
				Logger.Error("[BlessedClasses] Error in setCharacterClass prefix patch: {0}", ex.Message);
				return true; // On error, let original method run (and potentially fail with better error)
			}
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(Vintagestory.GameContent.CharacterSystem), "setCharacterClass")]
		public static void Postfix_SetCharacterClass(
			EntityPlayer eplayer,
			string classCode)
		{
			try
			{
				string playerName = eplayer.GetName() ?? "Unknown";
				string actualClass = eplayer.WatchedAttributes.GetString("characterClass");

				if (actualClass == classCode)
				{
					DiagnosticLogger.LogCharselSuccess(classCode, playerName);
				}
				else
				{
					DiagnosticLogger.LogCharselFailure(classCode, playerName,
						$"Class mismatch: expected '{classCode}', got '{actualClass}'");
				}

			}
			catch (Exception ex)
			{
				Logger.Error("[BlessedClasses] Error in setCharacterClass postfix patch: {0}", ex.Message);
			}
		}

		[HarmonyFinalizer]
		[HarmonyPatch(typeof(Vintagestory.GameContent.CharacterSystem), "setCharacterClass")]
		public static Exception Finalizer_SetCharacterClass(
			Exception __exception,
			EntityPlayer eplayer,
			string classCode)
		{
			if (__exception != null)
			{
				try
				{
					string playerName = eplayer?.GetName() ?? "Unknown";
					DiagnosticLogger.LogCharselFailure(classCode, playerName, __exception.Message);

					Logger.Error("[BlessedClasses] setCharacterClass threw exception:");
					Logger.Error("[BlessedClasses]   Player: {0}", playerName);
					Logger.Error("[BlessedClasses]   Class: {0}", classCode);
					Logger.Error("[BlessedClasses]   Exception: {0}", __exception);

				}
				catch (Exception ex)
				{
					Logger?.Error("[BlessedClasses] Error in setCharacterClass finalizer patch: {0}", ex.Message);
				}
			}

			// return the exception
			return __exception;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(Vintagestory.GameContent.CharacterSystem), "onCharacterSelection")]
		public static void Prefix_OnCharacterSelection(
			IServerPlayer fromPlayer,
			object p) // CharacterSelectionPacket
		{
			try
			{
				// use reflection to get the packet data since we can't reference the internal type
				var packetType = p.GetType();
				var didSelectProp = packetType.GetProperty("DidSelect");
				var characterClassProp = packetType.GetProperty("CharacterClass");

				if (didSelectProp != null && characterClassProp != null)
				{
					bool didSelect = (bool)didSelectProp.GetValue(p);
					string characterClass = (string)characterClassProp.GetValue(p);

					if (didSelect)
					{
						Logger.Notification("[BlessedClasses] Character selection packet received:");
						Logger.Notification("[BlessedClasses]   Player: {0}", fromPlayer.PlayerName);
						Logger.Notification("[BlessedClasses]   Selected Class: {0}", characterClass);
						Logger.Notification("[BlessedClasses]   Game Mode: {0}", fromPlayer.WorldData.CurrentGameMode);

						bool allowCharselOnce = fromPlayer.Entity.WatchedAttributes.GetBool("allowcharselonce");
						Logger.Debug("[BlessedClasses]   allowcharselonce: {0}", allowCharselOnce);
					}
				}

			}
			catch (Exception ex)
			{
				Logger.Error("[BlessedClasses] Error in onCharacterSelection prefix patch: {0}", ex.Message);
			}
		}
	}
}
