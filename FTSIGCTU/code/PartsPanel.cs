using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;

public static class PartsPanel
{
	//data structs, enums, variables
	private static bool ignorePartAllowances = false;
	private static bool allowMultipleIO = false;

	//---------------------------------------------------//
	//public methods
	public static void ApplySettings(bool _ignorePartAllowances, bool _allowMultipleIO)
	{
		ignorePartAllowances = _ignorePartAllowances;
		allowMultipleIO = _allowMultipleIO;
	}
	public static void LoadPuzzleContent()
	{
		On.PartTypeForToolbar.method_1225 += method_1225_ignorePartAllowances;
		On.PartTypeForToolbar.method_1226 += method_1226_allowMultipleIO;
	}

	//---------------------------------------------------//
	//internal main methods
	private static PartTypeForToolbar method_1225_ignorePartAllowances(
		On.PartTypeForToolbar.orig_method_1225 orig,
		class_139 partType,
		bool allowPlayerToGrabAnother,
		bool param_4948)
	{
		//if (partType.field_1552 && allowDuplicateParts) allowPlayerToGrabAnother = true;
		if (ignorePartAllowances) allowPlayerToGrabAnother = true;

		return orig(partType, allowPlayerToGrabAnother, param_4948);
	}
	private static PartTypeForToolbar method_1226_allowMultipleIO(
		On.PartTypeForToolbar.orig_method_1226 orig,
		class_139 partType,
		int param_4950,
		bool allowPlayerToGrabAnother,
		bool param_4952)
	{
		if (partType.method_310() && allowMultipleIO) allowPlayerToGrabAnother = true;

		return orig(partType, param_4950, allowPlayerToGrabAnother, param_4952);
	}
}