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
	private static IDetour hook_SolutionEditorProgramPanel_method_2064;
	private static bool drawBlanksOnProgrammingTray = false;

	//data structs, enums, variables

	//---------------------------------------------------//
	//internal helper methods

	//---------------------------------------------------//
	//internal main methods

	//---------------------------------------------------//

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

		//augment keyboard mappings
		var field = typeof(class_203).GetField("field_1883", BindingFlags.Static |BindingFlags.NonPublic);
		var keymappings = (Dictionary <SDL.enum_160, SDL.enum_160[]> ) field.GetValue(null);

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
	}

	private delegate void orig_SolutionEditorProgramPanel_method_2064(SolutionEditorProgramPanel sepp_self, InstructionType param_4881, Vector2 param_5660, Maybe<InstructionType> param_5661);
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
	public static void Unload()
	{
		hook_SolutionEditorProgramPanel_method_2064.Dispose();
	}

	//------------------------- END HOOKING -------------------------//

	public static void ApplySettings(bool _drawBlanksOnProgrammingTray, bool _allowMultipleOverrides)
	{
		drawBlanksOnProgrammingTray = _drawBlanksOnProgrammingTray;

		class_169.field_1652.field_2551 = !_allowMultipleOverrides;
	}
}