using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Settings;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;

public static class InstructionEditor
{
	static bool PressedExpandKey() => MainClass.MySettings.Instance.instructionEditingSettings.expandResetOrRepeat.Pressed(); /////////////////////////////////////////////////////// not ready yet
	static bool PressedHighlightArmKey() => MainClass.MySettings.Instance.instructionEditingSettings.highlightInstructionsOnArm.Pressed();
	static bool PressedUnhighlightArmKey() => MainClass.MySettings.Instance.instructionEditingSettings.unhighlightInstructionsOnArm.Pressed();

	public static bool drawBlanksOnProgrammingTray = false;

	static class_154 highlightedInstructions(SolutionEditorScreen SES) => SES.field_4012;

	private static IDetour hook_SolutionEditorProgramPanel_method_2064;
	private static IDetour hook_EditableProgram_method_912;
	private static InstructionType repeatBlankInstructionType;

	private static InstructionType overrideInstructionType => class_169.field_1652;
	private static InstructionType resetInstructionType => class_169.field_1665;
	private static InstructionType repeatInstructionType => class_169.field_1666;

	//data structs, enums, variables

	//---------------------------------------------------//
	//internal helper methods
	static SortedDictionary<int, InstructionType> getArmProgramTape(Part part) => new DynamicData(part.field_2697).Get<SortedDictionary<int, InstructionType>>("field_2415");
	static void setArmProgramTape(Part part, SortedDictionary<int, InstructionType> tape) => new DynamicData(part.field_2697).Set("field_2415", tape);

	//---------------------------------------------------//
	//internal main methods

	private static void shiftInstructions(SolutionEditorScreen SES, Part part, bool shiftRightwards, bool shiftFar)
	{
		var sortedDict = getArmProgramTape(part);
		var newDict = new SortedDictionary<int, InstructionType>();

		// if part's program is empty, then there is nothing to do
		if (sortedDict.Count == 0) return;

		// else, we try to shift instructions around
		// start by finding the period
		var solution = SES.method_502();
		int period = 0;

		foreach (Part key in solution.method_1941())
		{
			class_188 keyClass188 = key.field_2697.method_910(key, solution.method_1942(key));
			var keyExtentsDictionary = keyClass188.field_1745;

			if (keyExtentsDictionary.Count > 0)
			{
				int minIndex = keyClass188.field_1745.Keys.Min();
				int maxIndex = keyClass188.field_1745.Keys.Max();
				int armTapeLength = maxIndex - minIndex + keyExtentsDictionary[maxIndex].field_57.Length;
				period = Math.Max(period, armTapeLength);
			}
		}

		//return early if we want to shift left but can't
		int start = sortedDict.First().Key;
		if (!shiftRightwards && sortedDict.Keys.Contains(0)) return;
		if (!shiftRightwards && shiftFar && start < period) return;

		int shift = shiftRightwards ? 1 : -1;
		int scale = shiftFar ? period : 1;
		foreach (var kvp in sortedDict)
		{
			newDict.Add(kvp.Key + shift*scale, kvp.Value);
		}
		setArmProgramTape(part, newDict);
	}
	private static void squishInstructions(SolutionEditorScreen SES, Part part)
	{
		var solution = SES.method_502();
		var editableProgram = part.field_2697;
		class_188 class188 = editableProgram.method_910(part, solution.method_1942(part));
		var extentsDictionary = class188.field_1745;

		// if part's program is empty, then there is nothing to do
		if (extentsDictionary.Count == 0) return;

		// otherwise, squish the program together, with the first instruction where it is
		var newProgramTape = new SortedDictionary<int, InstructionType>();
		int n = extentsDictionary.First().Key;
		foreach (var kvp in extentsDictionary)
		{
			class_14 val = kvp.Value;
			var instructionType = val.field_56;
			var instructionLengthInTray = val.field_57.Length;

			newProgramTape[n] = instructionType;
			n += instructionLengthInTray;
		}

		setArmProgramTape(part, newProgramTape);
	}
	private static void spreadInstructions(Part part)
	{
		var sortedDict = getArmProgramTape(part);
		var newDict = new SortedDictionary<int, InstructionType>();
		int k = 0;
		if (sortedDict.Count > 0) k = sortedDict.First().Key;
		foreach (var kvp in sortedDict)
		{
			newDict.Add((kvp.Key-k)*2+k, kvp.Value);
		}
		setArmProgramTape(part, newDict);
	}
	private static bool tryToExpandHighlightedInstructionsInPart(SolutionEditorScreen SES, Part part)
	{
		var solution = SES.method_502();
		var programmablePartsList = solution.method_1941();
		// not a programmable part? then nothing to do (though how the function got called, i don't know)
		if (!programmablePartsList.Contains(part)) return false;

		var highlighted_instructions = highlightedInstructions(SES);
		var field1594 = new DynamicData(highlighted_instructions).Get<List<struct_8>>("field_1594");
		List<int> instructionsToCheck = field1594.Where(x => x.field_50 == part).Select(x => x.field_51).ToList();
		// no instructions to check? then nothing to do
		if (instructionsToCheck.Count == 0) return false;

		// find instructions to expand - return early if expansion is not possible
		var editableProgram = part.field_2697;
		class_188 class188 = editableProgram.method_910(part, solution.method_1942(part));
		var extentsDictionary = class188.field_1745;
		Dictionary<int, InstructionType[]> instructionsToExpandDictionary = new();

		foreach (KeyValuePair<int, class_14> kvp in extentsDictionary)
		{
			int instructionPosition = kvp.Key;
			class_14 val = kvp.Value;
			var instructionType = val.field_56;
			var resultOfExpandingTheInstruction = val.field_57;

			// if the instruction is the wrong type, or if it is not highlighted, then skip it
			if (instructionType != resetInstructionType && instructionType != repeatInstructionType) continue;
			if (!instructionsToCheck.Contains(instructionPosition)) continue;

			// otherwise, check if it CAN expand
			instructionsToExpandDictionary[instructionPosition] = resultOfExpandingTheInstruction;

			int length = resultOfExpandingTheInstruction.Length;
			for (int i = 1; i < length; ++i)
			{
				if (extentsDictionary.ContainsKey(instructionPosition + i))
				{
					//expanding this instruction would overlap one instruction on top of another, so give up
					return false;
				}
			}
		}

		// return early if there are no instructions to expand
		if (instructionsToExpandDictionary.Count == 0) return false;

		// otherwise, expansion is possible - time to get to work
		var armProgramTape = getArmProgramTape(part);
		foreach (var kvp in instructionsToExpandDictionary)
		{
			int instructionPosition = kvp.Key;
			var resultOfExpandingTheInstruction = kvp.Value;

			for (int i = 0; i < resultOfExpandingTheInstruction.Length; i++)
			{
				var instructionType = resultOfExpandingTheInstruction[i];
				if (instructionType != repeatBlankInstructionType) armProgramTape[i + instructionPosition] = instructionType;
			}
		}
		return true;
	}


	//---------------------------------------------------//
	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		var current_interface = SES_self.field_4010;
		bool modeMovingInstructionRow = current_interface.GetType() == new class_249().GetType();
		bool keyA = Input.IsSdlKeyPressed(SDL.enum_160.SDLK_a);
		bool keyD = Input.IsSdlKeyPressed(SDL.enum_160.SDLK_d);
		bool keyLBracket = Input.IsSdlKeyPressed(SDL.enum_160.SDLK_LEFTBRACKET);
		bool keyRBracket = Input.IsSdlKeyPressed(SDL.enum_160.SDLK_RIGHTBRACKET);
		bool keyShift = Input.IsShiftHeld();

		// time to do something!
		var interfaceDyn = new DynamicData(current_interface);
		var part = interfaceDyn.Get<Part>("field_2010");

		void clearHighlightedInstructions() => highlightedInstructions(SES_self).method_367();
		void soundInstructionPlace() => common.playSound(class_238.field_1991.field_1852, 0.2f);  // 'sounds/instruction_place'

		if (!modeMovingInstructionRow) return;

		if (keyA || keyD || keyLBracket || keyRBracket || PressedExpandKey())
		{
			// misc instruction editing
			if (keyA || keyD)
			{
				shiftInstructions(SES_self, part, keyD, keyShift);
			}
			else if (keyLBracket)
			{
				squishInstructions(SES_self, part);
				// if a repeat instruction would have expanded out to contain a blank,
				// then running squishInstructions will get rid of the blank in the "definition" of the repeat,
				// but the repeat instruction would have been maneuver _as if_ it was the original longer length
				// which would result in gaps between each repeat-definition and repeat-instance
				// and so, we run squishInstructions a second time to get rid of the gaps
				squishInstructions(SES_self, part);
			}
			else if (keyRBracket)
			{
				spreadInstructions(part);
			}
			else if (PressedExpandKey())
			{
				if (tryToExpandHighlightedInstructionsInPart(SES_self, part))
				{
					// success! play success noise
					common.playSound(class_238.field_1991.field_1877, 0.2f);  // 'sounds/ui_transition_back'
				}
				else
				{
					// failed to expand instructions - play fail noise
					common.playSound(class_238.field_1991.field_1872, 0.2f);  // 'sounds/ui_modal'
				}
			}

			clearHighlightedInstructions();
			soundInstructionPlace();
		}

		if (PressedHighlightArmKey())
		{
			foreach (struct_17 struct17 in part.field_2697.method_902())
			{
				// add instruction to highlights
				highlightedInstructions(SES_self).method_365(new struct_8(part, struct17.field_1421));
			}
			soundInstructionPlace();
		}
		else if (PressedUnhighlightArmKey())
		{
			foreach (struct_17 struct17 in part.field_2697.method_902())
			{
				// add instruction from highlights, if present
				highlightedInstructions(SES_self).method_366(new struct_8(part, struct17.field_1421));
			}
			common.playSound(class_238.field_1991.field_1851, 1f); // 'sounds/instruction_pickup'
		}
	}

	public static void LoadPuzzleContent()
	{
		//modify instructionTypes

		//blank1
		class_169.field_1653.field_2545 = SDL.enum_160.SDLK_b;
		class_169.field_1653.field_2543 = class_134.method_253("Idle Instruction", string.Empty);
		class_169.field_1653.field_2544 = class_134.method_253("An empty instruction that does nothing.", string.Empty);
		class_169.field_1653.field_2550 = enum_149.PreBasicSymbols;

		//blank2
		class_169.field_1654.field_2545 = SDL.enum_160.SDLK_n;
		class_169.field_1654.field_2543 = class_134.method_253("Marker Instruction", string.Empty);
		class_169.field_1654.field_2544 = class_134.method_253("An empty instruction that does nothing, but with an engraved symbol. Useful for marking an important part of the instruction tape", string.Empty);
		class_169.field_1654.field_2546 = class_235.method_615("ftsigctu/textures/instructions/idle2");
		class_169.field_1654.field_2550 = enum_149.PreBasicSymbols;

		//the blank used inside repeat instructions
		//copied from class_169.field_1654
		repeatBlankInstructionType = new InstructionType()
		{
			field_2542 = 'i',
			field_2543 = class_134.method_254(string.Empty),
			field_2544 = class_134.method_254(string.Empty),
			field_2546 = class_238.field_1989.field_87.field_659,
			field_2548 = (enum_144)0,
			field_2552 = false
		};



		//augment keyboard mappings
		var field = typeof(class_203).GetField("field_1883", BindingFlags.Static | BindingFlags.NonPublic);
		var keymappings = (Dictionary<SDL.enum_160, SDL.enum_160[]>)field.GetValue(null);

		keymappings.Add(
			SDL.enum_160.SDLK_n,
			new SDL.enum_160[6]
			{
				SDL.enum_160.SDLK_n,
				SDL.enum_160.SDLK_n,
				SDL.enum_160.SDLK_b,
				SDL.enum_160.SDLK_k,
				SDL.enum_160.SDLK_n,
				SDL.enum_160.SDLK_n
			}
		);
		field.SetValue(null, keymappings);

		//------------------------- HOOKING -------------------------//
		hook_SolutionEditorProgramPanel_method_2064 = new Hook(
			typeof(SolutionEditorProgramPanel).GetMethod("method_2064", BindingFlags.Instance | BindingFlags.NonPublic),
			typeof(InstructionEditor).GetMethod("OnSolutionEditorProgramPanelMethod2064", BindingFlags.Static | BindingFlags.NonPublic)
		);
		hook_EditableProgram_method_912 = new Hook(
			typeof(EditableProgram).GetMethod("method_912", BindingFlags.Static | BindingFlags.NonPublic),
			typeof(InstructionEditor).GetMethod("OnEditableProgramMethod912", BindingFlags.Static | BindingFlags.NonPublic)
		);
	}

	private delegate void orig_SolutionEditorProgramPanel_method_2064(SolutionEditorProgramPanel sepp_self, InstructionType param_4881, Vector2 param_5660, Maybe<InstructionType> param_5661);
	private delegate void orig_EditableProgram_method_912(class_188 param_4563, int param_4564);
	private static void OnSolutionEditorProgramPanelMethod2064(orig_SolutionEditorProgramPanel_method_2064 orig, SolutionEditorProgramPanel sepp_self, InstructionType instructionType, Vector2 position, Maybe<InstructionType> maybeInstructionType)
	{
		orig(sepp_self, instructionType, position, maybeInstructionType);

		// do the blank instructions right after the override instruction
		// need to draw it SOMEWHERE, even if not visible, in order to get the hotkey-functionality all the time
		float x = drawBlanksOnProgrammingTray ? 0f : -1000000f;
		float y = drawBlanksOnProgrammingTray ? 0f : -1000000f;
		if (instructionType == overrideInstructionType)
		{
			orig(sepp_self, class_169.field_1653, new Vector2(268f + x, 146f + y), maybeInstructionType);
			orig(sepp_self, class_169.field_1654, new Vector2(268f + x, 199f + y), maybeInstructionType);
		}
	}
	private static void OnEditableProgramMethod912(orig_EditableProgram_method_912 orig, class_188 param_4563, int param_4564)
	{
		var buffer = class_169.field_1654;
		class_169.field_1654 = repeatBlankInstructionType;
		/////////////////////////////
		orig(param_4563, param_4564);
		/////////////////////////////
		class_169.field_1654 = buffer;
	}

	public static void Unload()
	{
		hook_SolutionEditorProgramPanel_method_2064.Dispose();
		hook_EditableProgram_method_912.Dispose();
	}

	//------------------------- END HOOKING -------------------------//

	public static void ApplySettings(bool _drawBlanksOnProgrammingTray, bool _allowMultipleOverrides)
	{
		drawBlanksOnProgrammingTray = _drawBlanksOnProgrammingTray;

		//decide whether more than one override can be placed
		overrideInstructionType.field_2551 = !_allowMultipleOverrides;
	}
}