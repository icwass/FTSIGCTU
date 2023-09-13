using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;

public static class InstructionEditor
{
	public static bool drawBlanksOnProgrammingTray = false;

	private static IDetour hook_SolutionEditorProgramPanel_method_2064;
	private static IDetour hook_EditableProgram_method_912;
	private static InstructionType repeatBlankInstruction;

	//data structs, enums, variables

	//---------------------------------------------------//
	//internal helper methods

	//---------------------------------------------------//
	//internal main methods

	private static void shiftInstructions(Part part, bool shiftRightwards, bool shiftFar)
	{
		var tapeDyn = new DynamicData(part.field_2697);
		var sortedDict = tapeDyn.Get<SortedDictionary<int, InstructionType>>("field_2415");
		var newDict = new SortedDictionary<int, InstructionType>();

		//return early if we want to shift left but can't
		if (!shiftRightwards && sortedDict.Keys.Contains(0)) return;
		if (!shiftRightwards && shiftFar && sortedDict.Keys.Contains(1)) return;
		if (!shiftRightwards && shiftFar && sortedDict.Keys.Contains(2)) return;
		if (!shiftRightwards && shiftFar && sortedDict.Keys.Contains(3)) return;

		int shift = shiftRightwards ? 1 : -1;
		int scale = shiftFar ? 4 : 1;
		foreach (var kvp in sortedDict)
		{
			newDict.Add(kvp.Key + shift*scale, kvp.Value);
		}
		tapeDyn.Set("field_2415", newDict);
	}
	private static void squishInstructions(Part part)
	{
		var tapeDyn = new DynamicData(part.field_2697);
		var sortedDict = tapeDyn.Get<SortedDictionary<int, InstructionType>>("field_2415");
		var newDict = new SortedDictionary<int, InstructionType>();
		int i = 0;
		if (sortedDict.Count > 0) i = sortedDict.First().Key;
		foreach (var kvp in sortedDict)
		{
			newDict.Add(i, kvp.Value);
			i++;
		}
		tapeDyn.Set("field_2415", newDict);
	}
	private static void spreadInstructions(Part part)
	{
		var tapeDyn = new DynamicData(part.field_2697);
		var sortedDict = tapeDyn.Get<SortedDictionary<int, InstructionType>>("field_2415");
		var newDict = new SortedDictionary<int, InstructionType>();
		int k = 0;
		if (sortedDict.Count > 0) k = sortedDict.First().Key;
		foreach (var kvp in sortedDict)
		{
			newDict.Add((kvp.Key-k)*2+k, kvp.Value);
		}
		tapeDyn.Set("field_2415", newDict);
	}


	//---------------------------------------------------//
	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		var current_interface = SES_self.field_4010;
		bool inInstructionRowMode = current_interface.GetType() == new class_249().GetType();
		bool keyA = Input.IsSdlKeyPressed(SDL.enum_160.SDLK_a);
		bool keyD = Input.IsSdlKeyPressed(SDL.enum_160.SDLK_d);
		bool keyLBracket = Input.IsSdlKeyPressed(SDL.enum_160.SDLK_LEFTBRACKET);
		bool keyRBracket = Input.IsSdlKeyPressed(SDL.enum_160.SDLK_RIGHTBRACKET);
		bool keyShift = Input.IsShiftHeld();

		// exit early if wrong mode
		if (!inInstructionRowMode) return;

		// time to do something!
		var interfaceDyn = new DynamicData(current_interface);
		var part = interfaceDyn.Get<Part>("field_2010");

		if (keyA || keyD)
		{
			shiftInstructions(part, keyD, keyShift);
		}
		else if (keyLBracket)
		{
			squishInstructions(part);
		}
		else if (keyRBracket)
		{
			spreadInstructions(part);
		}
		else
		{
			//no inputs
			return;
		}

		//interfaceDyn.Set("field_2010", part); // not needed?
		common.playSound(class_238.field_1991.field_1852, 0.2f);  // 'sounds/instruction_place'
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
		repeatBlankInstruction = new InstructionType()
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
	private static void OnSolutionEditorProgramPanelMethod2064(orig_SolutionEditorProgramPanel_method_2064 orig, SolutionEditorProgramPanel sepp_self, InstructionType param_4881, Vector2 param_5660, Maybe<InstructionType> param_5661)
	{
		orig(sepp_self, param_4881, param_5660, param_5661);

		//do the blank instructions right after the override instruction
		float x = drawBlanksOnProgrammingTray ? 0f : -1000000f;
		float y = drawBlanksOnProgrammingTray ? 0f : -1000000f;

		if (param_4881 == class_169.field_1652)
		{
			orig(sepp_self, class_169.field_1653, new Vector2(268f + x, 146f + y), param_5661);
			orig(sepp_self, class_169.field_1654, new Vector2(268f + x, 199f + y), param_5661);
		}
	}
	private static void OnEditableProgramMethod912(orig_EditableProgram_method_912 orig, class_188 param_4563, int param_4564)
	{
		var buffer = class_169.field_1654;
		class_169.field_1654 = repeatBlankInstruction;
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
		class_169.field_1652.field_2551 = !_allowMultipleOverrides;
	}
}