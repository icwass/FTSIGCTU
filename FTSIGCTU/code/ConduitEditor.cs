using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;
using PartType = class_139;
using Texture = class_256;

public static class ConduitEditor
{
	//data structs, enums, variables
	private static bool allowConduitEditor = false;
	private static SDL.enum_160 editingKey = SDL.enum_160.SDLK_g;

	//private static Texture[] textures;
	private static Sound[] sounds;

	private enum resource : byte
	{
		create,
		destroy,
		swap,
		COUNT,
	}

	//---------------------------------------------------//
	//internal helper methods
	private static bool PartIsConduit(Part part) => part.method_1159() == common.IOConduit();
	private static bool PartIsEquilibrium(Part part) => part.method_1159() == common.GlyphEquilibrium();
	private static int ConduitIndex(Part part) => part.field_2703;

	//---------------------------------------------------//
	//internal main methods
	private static void createConduitFromEquilibriums(SolutionEditorScreen SES_self, List<Part> equilibriums)
	{
		//we will make two conduit parts, similar to the code in method_1957
		var SOLUTION = SES_self.method_502();
		var partList = SOLUTION.field_3919;
		var hexList = new List<HexIndex>();
		var origin = common.getPartOrigin(equilibriums[0]);
		//delete the equilibriums, but note where they were
		foreach (var equilibrium in equilibriums)
		{
			partList.Remove(equilibrium);
			hexList.Add(common.getPartOrigin(equilibrium) - origin);
		}
		//find an unused conduit index
		HashSet<int> ints = new();
		foreach (var part in partList.Where(x => PartIsConduit(x)))
		{
			ints.Add(ConduitIndex(part));
		}
		int num = 100;
		while(ints.Contains(num))
		{
			num++;
		}
		//create the conduit parts
		for (int i = 0; i < 2; i++)
		{
			Part part = new Part(class_191.field_1763, false);
			SOLUTION.method_1939(part, origin);
			part.field_2703 = num; // set conduit index
			part.method_1204(hexList);
			part.method_1198(SOLUTION, HexRotation.R0);
		}
		common.playSound(sounds[(int)resource.create], 0.2f);
	}

	private static void swapEquilibriumAndConduit(SolutionEditorScreen SES_self, Part part1, Part part2)
	{
		HexIndex hex1 = common.getPartOrigin(part1);
		HexIndex hex2 = common.getPartOrigin(part2);
		common.setPartOrigin(part1, hex2);
		common.setPartOrigin(part2, hex1);
		common.playSound(sounds[(int)resource.swap], 0.2f);
	}

	private static void deleteConduitFromSolution(SolutionEditorScreen SES_self, Part conduit)
	{
		var SOLUTION = SES_self.method_502();
		var partList = SOLUTION.field_3919;
		var conduitEnds = new List<Part>();
		foreach (Part part in partList.Where(x => ConduitIndex(x) == ConduitIndex(conduit) && PartIsConduit(x)))
		{
			conduitEnds.Add(part);
		}
		foreach (var end in conduitEnds)
		{
			partList.Remove(end);
		}
		common.playSound(sounds[(int)resource.destroy], 0.2f);
	}

	//---------------------------------------------------//

	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		if (!allowConduitEditor) return;
		var current_interface = SES_self.field_4010;


		///////////////////////////////////
		//temporary code that can change an instruction to a BLANK or OVERRIDE instruction
		if (current_interface.GetType() == (new class_217()).GetType())
		{
			var interfaceDyn = new DynamicData(current_interface);
			var draggedInstructions = interfaceDyn.Get<List<class_217.class_220>>("field_1908");

			if (draggedInstructions.Count == 1)
			{
				if (Input.IsSdlKeyPressed(SDL.enum_160.SDLK_b))
				{
					InstructionType BlankInstruction = class_169.field_1653;
					if (Input.IsShiftHeld())
					{
						BlankInstruction = class_169.field_1654;
					}
					draggedInstructions[0].field_1912 = BlankInstruction;
					interfaceDyn.Set("field_1908", draggedInstructions);
				}
				if (Input.IsSdlKeyPressed(SDL.enum_160.SDLK_o))
				{
					draggedInstructions[0].field_1912 = class_169.field_1652; // override
					interfaceDyn.Set("field_1908", draggedInstructions);
				}
			}
		}
		///////////////////////////////////

		if (current_interface.GetType() == (new NormalInputMode()).GetType() && Input.IsControlHeld() && Input.IsSdlKeyPressed(editingKey))
		{
			//we are trying to either create or destroy conduit

			class_6 partSelection = SES_self.field_4011;
			int sizeOfSelection = partSelection.method_13();
			if (sizeOfSelection == 0) return;

			List<Part> partsInSelection = partSelection.method_14().ToList();
			bool thereIsAConduit = partsInSelection.Any(x => PartIsConduit(x));
			bool thereIsAnEquilibrium = partsInSelection.Any(x => PartIsEquilibrium(x));
			bool onlyOneConduit = sizeOfSelection == 1 && thereIsAConduit;
			bool onlyOneConduitAndOneEquilibrium = sizeOfSelection == 2 && thereIsAConduit && thereIsAnEquilibrium;
			bool allEquilibriums = !partsInSelection.Any(x => !PartIsEquilibrium(x));

			if (onlyOneConduit)
			{
				deleteConduitFromSolution(SES_self, partsInSelection[0]);
			}
			else if (allEquilibriums)
			{
				createConduitFromEquilibriums(SES_self, partsInSelection);
			}
			else if (onlyOneConduitAndOneEquilibrium)
			{
				swapEquilibriumAndConduit(SES_self, partsInSelection[0], partsInSelection[1]);
			}
			if (onlyOneConduit || allEquilibriums || onlyOneConduitAndOneEquilibrium)
			{
				partSelection.method_16(); // clear the selection
				common.addUndoHistoryCheckpoint(SES_self);
			}
		}
	}

	public static void LoadPuzzleContent()
	{
		//load textures and sounds
		sounds = new Sound[(int)resource.COUNT];

		sounds[(int)resource.create] = class_238.field_1991.field_1841; // 'sounds/glyph_dispersion'
		sounds[(int)resource.destroy] = class_238.field_1991.field_1842;// 'sounds/glyph_disposal'
		sounds[(int)resource.swap] = class_238.field_1991.field_1852;// 'sounds/instruction_place'
	}
	public static void ApplySettings(bool _allowConduitEditor)
	{
		allowConduitEditor = _allowConduitEditor;
	}
	//---------------------------------------------------//
}