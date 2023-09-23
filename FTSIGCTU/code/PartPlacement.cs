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
			&& ignorePartOverlapPlacementRestrictions
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
		var ret = orig(solution_self, hex);
		if (ignoreCabinetPlacementRestrictions
			&& solution_self.method_1934().field_2779.method_99(out _)
		)
		{
			ret = (Maybe<class_189>) new class_189(0, 0, new class_183());
		}
		return ret;
	}
}