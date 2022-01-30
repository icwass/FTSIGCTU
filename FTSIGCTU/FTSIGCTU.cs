using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU
{
	using PartType = class_139;
	using Permissions = enum_149;
	using BondType = enum_126;
	using BondSite = class_222;
	using AtomTypes = class_175;
	using PartTypes = class_191;
	using Texture = class_256;

	public class FTSIGCTU : QuintessentialMod
	{
		//global variables and assets
		public static int MetricDisplaySwitch = 0;
		public static bool latencyFound = false;
		public static int latencyValue = 0;
		public static string CostMetric = "Cost";

		public static Texture ProgramPanelMetricHider;


		public override void Load()
		{
			//
			Settings = new FTSIGCTU_Settings();
		}

		public override Type SettingsType => typeof(FTSIGCTU_Settings);

		public class FTSIGCTU_Settings
		{
			[SettingsLabel("Display the 'Cost' metric as 'Gold'")]
			public bool displayCostAsGold = false;
		}

		public override void ApplySettings()
		{
			base.ApplySettings();
			CostMetric = ((FTSIGCTU_Settings)(Settings)).displayCostAsGold ? "Gold" : "Cost";
		}

		public override void LoadPuzzleContent()
		{
			//
			ProgramPanelMetricHider = class_235.method_615("textures/solution_editor/program_panel/frame_metric_hider");
		}

		public override void Unload()
		{
			//
		}

		public override void PostLoad()	{
			//
			On.SolutionEditorProgramPanel.method_221 += Method_221;
		}

		public static Action<string, string, Vector2, float> DrawMetric = (METRIC, VALUE, POSITION, FLOAT) =>
		{
			//based on Method_2086
			METRIC = class_134.method_253(METRIC, string.Empty).method_1060() + ":";
			VALUE = class_134.method_253(VALUE, string.Empty).method_1060();
			class_135.method_272(class_238.field_1989.field_99.field_706.field_746, POSITION);
			class_135.method_290(METRIC.method_441(), POSITION + new Vector2(FLOAT - 46f, 6f), class_238.field_1990.field_2142, Color.White, (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, -2, Color.Black, class_238.field_1989.field_99.field_706.field_751, int.MaxValue, false, true);
			class_135.method_290(VALUE.method_441(), POSITION + new Vector2(30f, 6f), class_238.field_1990.field_2142, Color.FromHex(3483687), (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, 0, new Color(), (class_256)null, int.MaxValue, false, true);
		};
		public static void DrawMetricTuple(
			string name1, string value1,
			string name2, string value2,
			string name3, string value3,
			string name4, string value4,
			float panel_y_offset
			)
		{
			float x = class_115.field_1433.X - 364f - 450f;
			float y = panel_y_offset + 268f;
			DrawMetric(name1, value1, new Vector2(x + 0.0f, y), -12f);
			DrawMetric(name2, value2, new Vector2(x + 150f, y), 0.0f);
			DrawMetric(name3, value3, new Vector2(x + 300f, y), 0.0f);
			DrawMetric(name4, value4, new Vector2(x + 450f, y), 0.0f);
		}

		public static void Method_221(On.SolutionEditorProgramPanel.orig_method_221 orig, SolutionEditorProgramPanel SEPPSelf, float param_5658)
		{
			//
			orig(SEPPSelf, param_5658);

			float panel_y = 0;
			var SES = new DynamicData(SEPPSelf).Get<SolutionEditorScreen>("field_2007");

			//hide the display drawn by the game
			class_135.method_272(ProgramPanelMetricHider, new Vector2((float)((double)class_115.field_1433.X - 944), panel_y + 266f));

			/////////////////////////////////////////////
			//fetch commonly-used data
			bool simRunning = (SES.method_503() != enum_128.Stopped);
			bool isProduction = SES.method_502().method_1934().field_2779.method_1085();
			int gold = SES.method_502().method_1954();
			int cycles = SES.field_4017.method_1090(SES.method_2127());
			int area = SES.field_4018.method_1090(SES.method_2128());
			if (!simRunning)
			{
				//compute area of parts manually
				area = -1;
			}
			Maybe<Sim> maybe6 = Sim.method_1824((SolutionEditorBase)SES);
			int instructions = maybe6.method_1087().method_1820().method_854();
			
			//parts list
			//int arms = 0;
			//int grippers = 0;
			//int track = 0;

			//instruction grid
			//int vintage_instructions = 0;
			//int assembly_instructions = 0;
			
			//product counts and latency calculation
			int productsAccepted = 0;
			int productsExpected = 0;
			if (!simRunning) latencyValue = 0;
			if (simRunning && !latencyFound)
			{
				latencyValue = cycles + 1;
			}
			latencyFound = true;
			foreach (Part part in SES.method_502().field_3919)
			{
				if (part.method_1159().method_309())//if output part
				{
					PartSimState partSimState = SES.method_507().method_481(part);
					productsAccepted += partSimState.field_2730;
					productsExpected += part.method_1169();
					if (partSimState.field_2730 == 0)
					{
						latencyFound = false;
						latencyValue = cycles;
					}
				}
			}
			/////////////////////////////////////////////




			/*
			
			*/

			//products


			string productFraction = simRunning ? string.Format("{0}/{1}", (object)productsAccepted, (object)productsExpected) : "----";

			if (class_115.method_198(SDL2.SDL.enum_160.SDLK_6)) MetricDisplaySwitch += 1;
			switch (MetricDisplaySwitch)
			{
				default:
					DrawMetricTuple("Products", productFraction, CostMetric, gold.method_453() + "$", "Cycles", simRunning ? cycles.method_453() : "----", isProduction ? "Instrs" : "Area", isProduction ? instructions.method_453() : (area >= 0 ? area.method_453() : "----"), panel_y);
					MetricDisplaySwitch = 0;
					break;
				case 1:
					DrawMetricTuple("Latency", simRunning ? latencyValue.method_453() : "----", "Sum", simRunning ? (gold + cycles + area).method_453() : "----", "Sum4", simRunning ? (gold + cycles + area + instructions).method_453() : "----", !isProduction ? "Instrs" : "Area", !isProduction ? instructions.method_453() : (area >= 0 ? area.method_453() : "----"), panel_y);
					break;
				//case 1:
				//	DrawMetricTuple("Products", productFraction, "Arms", arms.method_453(), "Sum", (gold + cycles + area).method_453(), !isProduction ? "Instrs" : "Area", !isProduction ? instructions.method_453() : (area >= 0 ? area.method_453() : "----"), panel_y);
				//	break;
				//case 2:
				//	DrawMetricTuple("Products", productFraction, "Track", track.method_453(), "Sum4", (gold + cycles + area+instructions).method_453(), "Vtg I", vintage_instructions.method_453(), panel_y);
				//	break;
				//case 3:
				//	DrawMetricTuple("Products", productFraction, "Grippers", grippers.method_453(), "Latency", simRunning ? latencyValue.method_453() : "----", "Assm I", assembly_instructions.method_453(), panel_y);
				//	break;
				//case 4:
			}
		}
	}
}