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
using Texture = class_256;

public static class Miscellaneous
{
	//data structs, enums, variables
	private static bool allowDuplicateParts = false;
	public static bool allowWrongNumberOfOutputs = false;
	private static Sound duplicatePart;

	private static float[] simspeed_factor = new float[6]{ 0.1f, 0.25f, 1.0f, 4.0f, 10.0f, 100.0f };
	private static int simspeed_index = 2; // index for 1.0f
	private static Sound clickButton;
	private static Texture[] textures;
	private static float speedtray_position_max = 72.0f;
	private static float speedtray_position = speedtray_position_max; //between 0 (all the way out) and speedtray_position_max (hidden)
	private static bool speedtrayZoomtoolWorkaround = false;

	private enum resource : byte
	{
		speed_tray,
		speed_slider,
		speed_slow_icon,
		speed_fast_icon,
		speed_fastest_icon,
		COUNT,
	}

	//---------------------------------------------------//
	//internal helper methods
	public static float SimSpeedFactor()
	{
		return simspeed_factor[simspeed_index];
	}

	//---------------------------------------------------//
	//internal main methods

	private static bool my_sepp_method_2066(SolutionEditorProgramPanel SEPPSelf, Texture button_icon, Vector2 position, bool is_active)
	{
		//reimplemented so we don't draw the info boxes, and that the button is pressed-in when held
		var SES = new DynamicData(SEPPSelf).Get<SolutionEditorScreen>("field_2007");

		Texture tool_button = class_238.field_1989.field_99.field_706.field_754;
		Texture tool_button_hover = class_238.field_1989.field_99.field_706.field_755;
		Texture tool_button_pressed = class_238.field_1989.field_99.field_706.field_756;

		Bounds2 bounds2 = Bounds2.WithSize(position, new Vector2(57f, 25f));
		bool in_view = bounds2.Contains(Input.MousePos());
		bool flag = in_view && !SES.method_2118();

		Color color = is_active ? Color.White : Color.White.WithAlpha(0.5f);

		class_135.method_275(tool_button, color, bounds2);
		if (flag && is_active) class_135.method_275(tool_button_hover, Color.White.WithAlpha(0.5f), bounds2);
		class_135.method_271(button_icon, color, (bounds2.Center - button_icon.field_2056.ToVector2() / 2).Rounded());
		if (in_view && Input.IsLeftClickHeld()) class_135.method_275(tool_button_pressed, Color.White, bounds2);

		if (flag & is_active) class_269.field_2106 = class_238.field_1994.field_45; // "Content/textures/cursor_normal.png"

		return is_active && flag && class_115.method_206((enum_142)1);
	}

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
					common.playSound(duplicatePart, 0.2f);
					common.addUndoHistoryCheckpoint(SES_self);
				}
			}
		}
	}

	public static void SEPP_method_221(SolutionEditorProgramPanel SEPPSelf)
	{
		//---------- draw the speedtray ----------//
		// tray and reference points
		Texture speed_tray = textures[(int)resource.speed_tray];
		Vector2 speed_base_position = new Vector2(Input.ScreenSize().X - common.textureDimensions(speed_tray).X + speedtray_position, 316f);
		Bounds2 speed_tray_area = Bounds2.WithSize(speed_base_position - new Vector2(speedtray_position, 0f), common.textureDimensions(speed_tray));

		float delta = 4f;
		if (speed_tray_area.Contains(Input.MousePos()))
		{
			speedtray_position = Math.Max(speedtray_position - delta, 0f);
		}
		else
		{
			speedtray_position = Math.Min(speedtray_position + delta, speedtray_position_max);
		}

		class_135.method_272(speed_tray, speed_base_position);

		Vector2 button_position = speed_base_position + new Vector2(38f, 17f);

		if (!speedtrayZoomtoolWorkaround)
		{
			// buttons
			bool flag = simspeed_index > 0;
			if (my_sepp_method_2066(SEPPSelf, textures[(int)resource.speed_slow_icon], button_position, flag))
			{
				simspeed_index--;
				common.playSound(clickButton);
			}

			button_position += new Vector2(0f, 96f);
			flag = simspeed_index < simspeed_factor.Length - 1;
			if (my_sepp_method_2066(SEPPSelf, textures[flag ? (int)resource.speed_fast_icon : (int)resource.speed_fastest_icon], button_position, flag))
			{
				simspeed_index++;
				common.playSound(clickButton);
			}

			// slider
			class_135.method_272(textures[(int)resource.speed_slider], speed_base_position + new Vector2(48f, 46f + Math.Min(simspeed_index, simspeed_factor.Length - 2) * 12.0f));
		}
		else // special workaround - buttons that manually set the speed
		{
			Texture[] icons = new Texture[6] {
				textures[(int)resource.speed_slow_icon],
				textures[(int)resource.speed_slow_icon],
				class_238.field_1989.field_73, // a texture consisting of a single, transparent pixel
				textures[(int)resource.speed_fast_icon],
				textures[(int)resource.speed_fast_icon],
				textures[(int)resource.speed_fastest_icon]
			};
			button_position += new Vector2(0f, -12f);
			for (int i = 0; i < 6; i++)
			{
				if (my_sepp_method_2066(SEPPSelf, icons[i], button_position, true))
				{
					simspeed_index = i;
					common.playSound(clickButton);
				}
				button_position += new Vector2(0f, 24f);
			}

		}

	}

	public static void method_221_manipulateOutputCount(ILContext il)
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

	public static void LoadPuzzleContent()
	{
		duplicatePart = class_238.field_1991.field_1841; // 'sounds/glyph_dispersion'

		//load resources for speed bar
		clickButton = class_238.field_1991.field_1821; // 'sounds/click_button'

		textures = new Texture[(int)resource.COUNT];
		string path = "ftsigctu/textures/solution_editor/program_panel/";
		textures[(int)resource.speed_tray] = class_235.method_615(path + "speed_tray");
		textures[(int)resource.speed_slider] = class_235.method_615(path + "speed_slider");
		textures[(int)resource.speed_slow_icon] = class_235.method_615(path + "speed_slow_icon");
		textures[(int)resource.speed_fast_icon] = class_235.method_615(path + "speed_fast_icon");
		textures[(int)resource.speed_fastest_icon] = class_235.method_615(path + "speed_fastest_icon");

		IL.SolutionEditorProgramPanel.method_221 += method_221_manipulateOutputCount;
	}

	public static void ApplySettings(bool _allowDuplicateParts, bool _speedtrayZoomtoolWorkaround, bool _allowWrongNumberOfOutputs)
	{
		allowDuplicateParts = _allowDuplicateParts;
		speedtrayZoomtoolWorkaround = _speedtrayZoomtoolWorkaround;
		allowWrongNumberOfOutputs = _allowWrongNumberOfOutputs;

		//allow multiple berlo wheels?
		class_191.field_1771.field_1552 = !_allowDuplicateParts;

		//allow multiple disposals?
		class_191.field_1781.field_1552 = !_allowDuplicateParts;
	}
	//---------------------------------------------------//

}