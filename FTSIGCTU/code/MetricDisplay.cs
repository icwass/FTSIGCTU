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

public static class MetricDisplay
{
	//data structs, enums, variables
	private static Texture metric_overlay;
	private static bool displayAlternateMetrics;
	private static bool writeGoldNotCost = false;

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

	private static void display_metric(string name, string value, Vector2 position, float offset = 0f)
	{
		//copied from method_2086()
		Texture score_card = class_238.field_1989.field_99.field_706.field_746;
		Texture text_gradient = class_238.field_1989.field_99.field_706.field_751;

		position += new Vector2(class_115.field_1433.X - 364.0f, 268f); // base position

		var font = class_238.field_1990.field_2142;
		class_135.method_272(score_card, position);
		class_135.method_290(name.method_441(), position + new Vector2(offset - 46f, 6f), font, Color.White, (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, -2, Color.Black, text_gradient, int.MaxValue, false, true);
		class_135.method_290(value.method_441(), position + new Vector2(30f, 6f), font, Color.FromHex(3483687), (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, 0, new Color(), (class_256)null, int.MaxValue, false, true);
	}

	//---------------------------------------------------//
	//internal main methods

	//---------------------------------------------------//
	public static void SEPP_method_221(SolutionEditorProgramPanel SEPPSelf)
	{
		var SES = new DynamicData(SEPPSelf).Get<SolutionEditorScreen>("field_2007");
		bool simRunning = (SES.method_503() != enum_128.Stopped);

		Vector2 metric_overlay_position = new Vector2(class_115.field_1433.X - common.textureDimensions(metric_overlay).X - 300f, 296f - 32f);

		if (Input.IsSdlKeyPressed(SDL.enum_160.SDLK_6))
		{
			displayAlternateMetrics = !displayAlternateMetrics;
		}

		//-------------- compute metrics --------------//
		bool isProduction = SES.method_502().method_1934().field_2779.method_1085();
		var maybeSim = Sim.method_1824(SES);
		bool validProgram = maybeSim.method_1085();

		// base metrics
		int cycles = SES.field_4017.method_1090(SES.method_2127());
		int gold = SES.method_502().method_1954();
		int area = SES.field_4018.method_1090(SES.method_2128());

		// program metrics
		int instructions = -1;
		int period = -1;
		if (validProgram)
		{
			CompiledProgramGrid compiledProgramGrid = maybeSim.method_1087().method_1820();
			var programGridDict = new DynamicData(compiledProgramGrid).Get<Dictionary<Part, CompiledProgram>>("field_2368");
			instructions = compiledProgramGrid.method_854();
			period = 0;
			foreach (var ENTRY in programGridDict)
			{
				CompiledInstruction[] compiledArray = ENTRY.Value.field_2367;
				period = Math.Max(period, compiledArray.Length);
			}
		}

		// products
		int productsAccepted = 0;
		int productsExpected = 0;
		foreach (Part output in SES.method_502().field_3919.Where(x => x.method_1159().method_309()))
		{
			PartSimState partSimState = SES.method_507().method_481(output);
			productsAccepted += partSimState.field_2730;
			productsExpected += output.method_1169();
		}
		string productFraction = simRunning ? string.Format("{0}/{1}", productsAccepted, productsExpected) : "----";

		// sum
		int sumFreespace = simRunning ? cycles + gold + area : -1;
		int sumProduction = simRunning && validProgram ? cycles + gold + instructions : -1;
		int sum = isProduction ? sumProduction : sumFreespace;

		// height/width
		int height = AreaDisplay.getHeightMetric;
		int width = AreaDisplay.getWidthMetric;


		//-------------- display metrics --------------//
		// we just write over the normal display
		class_135.method_272(metric_overlay, metric_overlay_position);

		float xpos;

		// METRIC 1:
		xpos = -450f;
		display_metric(class_134.method_253("Products", "SHORT LENGTH").method_1060() + ":", productFraction, new Vector2(xpos, 0), -12f);

		// METRIC 2:
		xpos = -300f;
		if (displayAlternateMetrics)
		{
			display_metric(class_134.method_253("Sum", string.Empty).method_1060() + ":", sum >= 0 ? sum.method_453() : "----", new Vector2(xpos, 0));
		}
		else
		{
			display_metric(class_134.method_253(writeGoldNotCost ? "Gold" : "Cost", string.Empty).method_1060() + ":", string.Format("{0}$", gold), new Vector2(xpos, 0));
		}

		// METRIC 3:
		xpos = -150f;
		if (displayAlternateMetrics)
		{
			if (height >= 0)
			{
				display_metric(class_134.method_253("HEIGHT", string.Empty).method_1060() + ":", height.method_453(), new Vector2(xpos, 0));
			}
			else if (width >= 0)
			{
				display_metric(class_134.method_253("WIDTH", string.Empty).method_1060() + ":", width.method_453(), new Vector2(xpos, 0));
			}
			else
			{
				display_metric(class_134.method_253("PERIOD", string.Empty).method_1060() + ":", period.method_453(), new Vector2(xpos, 0));
			}
		}
		else
		{
			display_metric(class_134.method_253("Cycles", string.Empty).method_1060() + ":", simRunning ? cycles.method_453() : "----", new Vector2(xpos, 0));
		}

		// METRIC 4:
		xpos = -0f;
		if (displayAlternateMetrics ^ isProduction) // XOR
		{
			display_metric(class_134.method_253("Instrs", string.Empty).method_1060() + ":", instructions >= 0 ? instructions.method_453() : "----", new Vector2(xpos, 0));
		}
		else // alternate metrics
		{
			display_metric(class_134.method_253("Area", string.Empty).method_1060() + ":", simRunning ? area.method_453() : "----", new Vector2(xpos, 0));
		}
	}

	public static void LoadPuzzleContent()
	{
		//load texture
		string path = "ftsigctu/textures/solution_editor/program_panel/";
		metric_overlay = class_235.method_615(path + "metric_overlay");

	}

	//---------------------------------------------------//
	public static void ApplySettings(bool _writeGoldNotCost)
	{
		writeGoldNotCost = _writeGoldNotCost;
	}
}