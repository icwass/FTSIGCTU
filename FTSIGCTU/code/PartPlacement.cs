using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;

public static class PartPlacement
{
	//data structs, enums, variables
	public static bool ignorePartOverlapPlacementRestrictions = false;
	public static bool ignoreCabinetPlacementRestrictions = false;
	public static bool allowTrackOverlapDragging = true;
	public static bool ignoreCabinetTrackRestrictions = false;

	//---------------------------------------------------//
	//public methods
	public static void PostLoad()
	{
		On.Solution.method_1947 += Solution_method_1947;
		On.Solution.method_1948 += Solution_method_1948;
	}

	//---------------------------------------------------//
	//internal helper methods

	//---------------------------------------------------//
	//internal main methods

	//---------------------------------------------------//

	static string errStr(string str) => (string)class_134.method_253(str, string.Empty);

	public static HashSet<HexIndex> Solution_method_1947(On.Solution.orig_method_1947 orig, Solution solution_self, Maybe<Part> maybePart, enum_137 enum137)
	{
		var ret = orig(solution_self, maybePart, enum137);
		if (enum137 == (enum_137)2
			&& !maybePart.method_1085()
			&& allowTrackOverlapDragging
		)
		{
			ret = new HashSet<HexIndex>();
		}
		return ret;
	}
	public static bool Solution_method_1948(On.Solution.orig_method_1948 orig,
		Solution solution_self,
		Part part,
		HexIndex hex1,
		HexIndex hex2,
		HexRotation rot,
		out string errorMessageOut)
	{
		string errorMessage;
		bool ret = orig(solution_self, part, hex1, hex2, rot, out errorMessage);

		if ((ignorePartOverlapPlacementRestrictions && errorMessage == errStr("There is already another part here."))
			|| (ignoreCabinetPlacementRestrictions && errorMessage == errStr("This puzzle requires the product and reagent to be placed in separate chambers."))
			|| (ignoreCabinetPlacementRestrictions && errorMessage == errStr("Conduits cannot be moved to different chambers."))
			|| (ignoreCabinetPlacementRestrictions && errorMessage == errStr("Parts cannot be placed here."))
		)
		{
			errorMessage = null;
			ret = true;
		}

		errorMessageOut = errorMessage;
		return ret;
	}

	static void errorLog1952(string header) => Logger.Log("[FTSIGCTU.PartPlacement.Solution_method_1952] Error: " + header
		+ "\n\tThis crash usually occurs when trying to move a part (such has a conduit) in a production level."
		+ "\n\tIf this is the case, please follow these instructions:"
		+ "\n\t\t1. [Optional] Send a copy of this log.txt to the mod developer (isaac.wass / mr_puzzel) through GitHub or Discord."
		+ "\n\t\t2. Restart modded OpusMagnum."
		+ "\n\t\t3. Go to FTSIGCTU's mod settings in the \"MODS\" menu."
		+ "\n\t\t4. Under \"Part-Placement Settings\", UN-CHECK the setting \"Disable cabinet-related track-extension restrictions.\""
		+ "\nThis should prevent the crash from happening again, but of course, you will not be able to drag track outside of chambers or through chamber walls."
	);

	public static Maybe<class_189> Solution_method_1952(On.Solution.orig_method_1952 orig, Solution solution_self, HexIndex hex)
	{
		//NOTE: Adding this functionality has caused some users to experience game crashes.
		// Specifically, the crash occurs when attempting to move a part in a solution for a production puzzle.
		// (For some reason, the relevant Solution object is missing or something when this function gets called.)
		// So now this functionality is loaded/unloaded via a setting.
		Maybe<class_189> ret = (Maybe<class_189>) struct_18.field_1431;
		try
		{
			ret = orig(solution_self, hex);
		}
		catch
		{
			if (solution_self == null)
			{
				errorLog1952("solution_self is null.");
				throw;
			}
			else if (hex == null)
			{
				errorLog1952("hex is null.");
				throw;
			}
			else if (solution_self.field_3915 == null || solution_self.method_1934() == null)
			{
				errorLog1952("solution_self is malformed or inaccessible.");
				throw;
			}

			errorLog1952("failed to execute \"ret = orig(solution_self, hex);\" for unknown reasons.");
			throw;
		}


		try
		{
			if (ignoreCabinetTrackRestrictions
				&& !ret.method_1085()
				&& solution_self.method_1934().field_2779.method_99(out _)
			)
			{
				ret = (Maybe<class_189>)new class_189(0, 0, Puzzles.field_2926);
			}
			return ret;
		}
		catch
		{
			if (solution_self == null)
			{
				errorLog1952("solution_self is null.");
				throw;
			}
			else if (hex == null)
			{
				errorLog1952("hex is null.");
				throw;
			}
			else if (solution_self.field_3915 == null || solution_self.method_1934() == null)
			{
				errorLog1952("solution_self is malformed or inaccessible.");
				throw;
			}

			errorLog1952("failed to execute \"ret = orig(solution_self, hex);\" for unknown reasons.");
			throw;
		}
	}
}