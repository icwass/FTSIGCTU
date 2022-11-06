using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;

public static class Miscellaneous
{
	//data structs, enums, variables
	private static bool allowDuplicateParts = false;

	private static Sound[] sounds;

	private enum resource : byte
	{
		create,
		COUNT,
	}

	//---------------------------------------------------//
	//internal helper methods
	//private static bool PartIsConduit(Part part) => part.method_1159() == common.IOConduit();
	//private static bool PartIsEquilibrium(Part part) => part.method_1159() == common.GlyphEquilibrium();
	//private static int ConduitIndex(Part part) => part.field_2703;

	//---------------------------------------------------//
	//internal main methods

	//---------------------------------------------------//

	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		if (!allowDuplicateParts) return;
		var current_interface = SES_self.field_4010;

		//duplicate an input or output
		if (current_interface.GetType() == (new NormalInputMode()).GetType() && Input.IsControlHeld() && Input.IsSdlKeyPressed(SDL.enum_160.SDLK_h))
		{
			class_6 partSelection = SES_self.field_4011;
			int sizeOfSelection = partSelection.method_13();
			if (sizeOfSelection == 1)
			{
				Part part = partSelection.method_14().ToList()[0];
				if (part.method_1159().method_310()) //input, standard output, or infinite output
				{
					var SOLUTION = SES_self.method_502();
					var partList = SOLUTION.field_3919;
					partList.Add(part.method_1175(SOLUTION, (Maybe<Part>)struct_18.field_1431));
					common.playSound(sounds[(int)resource.create], 0.2f);
					common.addUndoHistoryCheckpoint(SES_self);
				}
			}
		}
	}

	public static void LoadPuzzleContent()
	{
		//load sounds
		sounds = new Sound[(int)resource.COUNT];

		sounds[(int)resource.create] = class_238.field_1991.field_1841; // 'sounds/glyph_dispersion'
	}

	public static void ApplySettings(bool _allowDuplicateParts)
	{
		allowDuplicateParts = _allowDuplicateParts;

		//allow multiple berlo wheels?
		class_191.field_1771.field_1552 = !_allowDuplicateParts;

		//allow multiple disposals?
		class_191.field_1781.field_1552 = !_allowDuplicateParts;
	}
	//---------------------------------------------------//
}