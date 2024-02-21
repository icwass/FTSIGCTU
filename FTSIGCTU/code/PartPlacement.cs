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

	//---------------------------------------------------//
	//public methods
	public static void PostLoad()
	{
		On.Solution.method_1947 += Solution_method_1947;
		On.Solution.method_1948 += Solution_method_1948;
		On.Solution.method_1952 += Solution_method_1952;
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
	public static Maybe<class_189> Solution_method_1952(On.Solution.orig_method_1952 orig, Solution solution_self, HexIndex hex)
	{
		//DEBUG: try-catch, just in case we still have problems
		Maybe<class_189> ret = (Maybe<class_189>) struct_18.field_1431;
		try
		{
			ret = orig(solution_self, hex);
		}
		catch
		{
			Logger.Log("");
			Logger.Log("[Solution_method_1952] Failed to execute \"ret = orig(solution_self, hex);\"");
			Logger.Log("<Anataeus> Good lord, the original function failed to work! How come?");
			Logger.Log("<Henley> The hexIndex seems fine - look, it's equal to (" + hex.Q + ", " + hex.R + ").");
			Logger.Log("<Anataeus> That just leaves the solution_self object - here, take a look inside.");

			if (solution_self == null)
			{
				Logger.Log("<Henley> There's nothing in there at all. Nothing!");
				Logger.Log("<Anataeus> Oh hell.");
				throw;
			}

			Logger.Log("<Henley> Oh, there's seems to be a solution of some sort in there. I'll try to pull it out.");
			Logger.Log("<Anataeus> It's labeled \"" + solution_self.field_3915 + "\", curious. And it makes...");
			Logger.Log("<Anataeus> It makes \"" + solution_self.method_1934().field_2766 + "\", apparently. Puzzling.");
			Logger.Log("<Henley> Wait, so then the problem was the original function call?");
			Logger.Log("<Anataeus> Oh hell.");
			throw;
		}


		try
		{
			if (ignoreCabinetPlacementRestrictions
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
			Logger.Log("");
			Logger.Log("[Solution_method_1952] Failed to run the return block.");
			Logger.Log("<Anataeus> Good lord, why?!");
			Logger.Log("<Henley> If it made it past the original function call, then the solution_self should be fine.");
			Logger.Log("<Anataeus> Maybe not? Here, take a look inside.");

			if (solution_self == null)
			{
				Logger.Log("<Henley> There's nothing in there at all! Nothing!!");
				Logger.Log("<Anataeus> Oh hell.");
				throw;
			}

			Logger.Log("<Henley> Yep, there's the solution, plain as day.");
			Logger.Log("<Anataeus> And it's labeled \"" + solution_self.field_3915 + "\", apparently. And it makes...");
			Logger.Log("<Anataeus> Ah, it makes \"" + solution_self.method_1934().field_2766 + "\". Delightful.");
			Logger.Log("<Henley> Wait, so what caused the problem?");
			Logger.Log("<Anataeus> Oh hell.");
			throw;
		}
	}
}