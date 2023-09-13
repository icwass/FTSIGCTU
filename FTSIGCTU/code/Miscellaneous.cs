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
//using Texture = class_256;

public static class Miscellaneous
{
	//data structs, enums, variables
	public static bool allowWrongNumberOfOutputs = false;
	public static bool ignorePartPlacementRestrictions = false;

	//---------------------------------------------------//
	//public methods
	public static void LoadPuzzleContent()
	{
		IL.SolutionEditorProgramPanel.method_221 += method_221_manipulateOutputCount;
	}

	public static void PostLoad()
	{
		On.Solution.method_1948 += Solution_method_1948;
	}

	//---------------------------------------------------//
	//internal helper methods

	//---------------------------------------------------//
	//internal main methods

	//---------------------------------------------------//

	private static void method_221_manipulateOutputCount(ILContext il)
	{
		ILCursor cursor = new ILCursor(il);
		// skip ahead to roughly where the output-count conditional begins
		cursor.Goto(1673);

		// jump ahead to just after we add the total number of output parts
		if (!cursor.TryGotoNext(MoveType.After, instr => instr.Match(OpCodes.Add))) return;

		// duplicate the sum so we can use it later
		cursor.Emit(OpCodes.Dup);

		// jump ahead to just before the branch instruction
		cursor.GotoNext(MoveType.Before, instr => instr.Match(OpCodes.Beq_S));

		cursor.EmitDelegate<Func<int, int, int>>((int boardCount, int puzzleCount) =>
		{
			// there is another copy of boardCount on the stack already
			// returning boardCount will force the branch instruction to execute
			// returning puzzleCount will yield the original behavior
			return allowWrongNumberOfOutputs ? boardCount : puzzleCount;
		});
	}


	public static bool Solution_method_1948(On.Solution.orig_method_1948 orig,
		Solution solution_self,
		Part part,
		HexIndex hex1,
		HexIndex hex2,
		HexRotation rot,
		out string errorMessage)
	{
		if (ignorePartPlacementRestrictions)
		{
			errorMessage = null;
			return true;
		}
		else
		{
			bool ret = orig(solution_self, part, hex1, hex2, rot, out errorMessage);
			return ret;
		}
	}
}