using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
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
		public static bool DEBUG = false;

		public static int MetricDisplaySwitch = 0;
		public static bool puzzleSolved = false;
		public static bool latencyFound = false;
		public static int latencyValue = 0;
		public static int heightValue = 0;
		public static int widthValue = 0;
		public static int blueprintValue = 0;
		public static int gifRecorderSpeed = 100;
		public static string CostMetric = "Cost";

		public static Texture ProgramPanelMetricHider;
		public static Texture ProgramPanelMetricTab;
		public static Texture GifFrame;
		public const int numberOfMetricDisplays = 5;
		public static float[] metricTabHeights = new float[numberOfMetricDisplays];

		//upgrade to new version of Quintessential that has built-in methods for this keyboard crap/////////////////////////////////////////////
		public static bool keyHeld(SDL.enum_160 KEY) => class_115.method_192(KEY);
		public static bool keyPressed(SDL.enum_160 KEY) => class_115.method_198(KEY);
		public static bool keyReleased(SDL.enum_160 KEY) => class_115.method_199(KEY);

		public override void Load()
		{
			//
			Settings = new FTSIGCTU_Settings();
			for (int i = 0; i < numberOfMetricDisplays; i++)
			{
				metricTabHeights[i] = (i==0 ? 1f : 0f);
			}
		}

		public override Type SettingsType => typeof(FTSIGCTU_Settings);

		public class FTSIGCTU_Settings
		{
			[SettingsLabel("Display the 'Cost' metric as 'Gold'.")]
			public bool displayCostAsGold = false;
			[SettingsLabel("Use thicker lines when drawing the current area.")]
			public bool advancedAreaThick = false;
			[SettingsLabel("Use color-coding when drawing the current area.")]
			public bool advancedAreaColors = false;
		}

		public override void ApplySettings()
		{
			base.ApplySettings();
			CostMetric = ((FTSIGCTU_Settings)(Settings)).displayCostAsGold ? "Gold" : "Cost";

			AreaManager.thickHexes = ((FTSIGCTU_Settings)(Settings)).advancedAreaThick;

			AreaManager.enabled = ((FTSIGCTU_Settings)(Settings)).advancedAreaColors;
			class_238.field_1989.field_90.field_172 = AreaManager.areaTex[AreaManager.enabled ? 1 : 0];
		}

		private static IDetour hook_Sim_method_1835;
		private static IDetour hook_Sim_method_1838;

		public override void LoadPuzzleContent()
		{
			//
			ProgramPanelMetricHider = class_235.method_615("textures/solution_editor/program_panel/frame_metric_hider");
			ProgramPanelMetricTab = class_235.method_615("textures/solution_editor/program_panel/metric_tab");
			AreaManager.init();


			GifFrame = class_235.method_615("textures/gif_frame_blank");


			hook_Sim_method_1835 = new Hook(
				typeof(Sim).GetMethod("method_1835", BindingFlags.Instance | BindingFlags.NonPublic),
				typeof(FTSIGCTU).GetMethod("OnSimMethod1835", BindingFlags.Static | BindingFlags.NonPublic)
			);
			hook_Sim_method_1838 = new Hook(
				typeof(Sim).GetMethod("method_1838", BindingFlags.Instance | BindingFlags.NonPublic),
				typeof(FTSIGCTU).GetMethod("OnSimMethod1838", BindingFlags.Static | BindingFlags.NonPublic)
			);
		}


		private delegate void orig_Sim_method_1835(Sim Sim_self);
		private delegate void orig_Sim_method_1838(Sim Sim_self);
		private delegate void orig_Sim_method_1840(Sim Sim_self, Vector2 param_5376);



		private static void OnSimMethod1835(orig_Sim_method_1835 orig, Sim Sim_self)
		{
			AreaManager.Method1835(Sim_self);
			orig(Sim_self);
		}
		private static void OnSimMethod1838(orig_Sim_method_1838 orig, Sim Sim_self)
		{
			AreaManager.Method1838(Sim_self);

			orig(Sim_self);
		}

		public override void Unload()
		{
			hook_Sim_method_1835.Dispose();
			hook_Sim_method_1838.Dispose();
		}

		public override void PostLoad()	{
			//
			On.SolutionEditorProgramPanel.method_221 += Method_221;
			On.CompiledProgramGrid.method_853 += Method_853;
			On.class_250.method_50 += class250_Method_50;
			//On.class_250.method_50 += GifRecorder_Method_50;

			On.Sim.method_1824 += Method_1824;//find area hexes covered by a part
			On.class_153.method_221 += c153_Method_221;//draw area hexes
		}
		public static void c153_Method_221(On.class_153.orig_method_221 orig, class_153 c153_self, float param_3616)
		{
			orig(c153_self, param_3616);
			AreaManager.c153_Method221(c153_self);
		}
		public static Maybe<Sim> Method_1824(On.Sim.orig_method_1824 orig, SolutionEditorBase param_5365)
		{
			AreaManager.Method1824(param_5365);

			return orig(param_5365);
		}




		public static int Method_853(On.CompiledProgramGrid.orig_method_853 orig, CompiledProgramGrid CPGSelf, int param_4510)
		{
			int ret = orig(CPGSelf, param_4510);
			return (ret == 0 ? 1 : ret);
		}

		public static void GifRecorder_Method_50(On.class_250.orig_method_50 orig, class_250 c250Self, float param_4165)
		{
			orig(c250Self, param_4165);

			var c250dyn = new DynamicData(c250Self);
			int field2028 = c250dyn.Get<int>("field_2028");
			int field2029 = c250dyn.Get<int>("field_2029");

			if (new DynamicData(c250Self).Get<bool>("field_2030") && new DynamicData(c250Self).Get<bool>("field_2026"))
			{
				if (class_115.method_193(0) && class_115.method_198(SDL.enum_160.SDLK_F1))
				{
					field2028 = Math.Max(0, (field2028 + 1) / 10);
					class_238.field_1991.field_1823.method_28(1f);
				}
				if (class_115.method_193(0) && class_115.method_198(SDL.enum_160.SDLK_F2))
				{
					if ((field2028 - 1) > field2029 / 10)
					{
						field2028 = field2029 - 1;
					}
					else
					{
						field2028 = Math.Max((field2028 - 1), 1) * 10;
					}
					
					class_238.field_1991.field_1823.method_28(1f);
				}
				if (class_115.method_193(0) && class_115.method_198(SDL.enum_160.SDLK_F3))
				{
					field2029 = Math.Max(field2028 + 1, (field2029 + 1) / 10);
					class_238.field_1991.field_1823.method_28(1f);
				}
				if (class_115.method_193(0) && class_115.method_198(SDL.enum_160.SDLK_F4))
				{
					if ((field2029 - 1) > int.MaxValue / 10)
					{
						field2029 = int.MaxValue - 1;
					}
					else
					{
						field2029 = (field2029 - 1) * 10;
					}
					class_238.field_1991.field_1823.method_28(1f);
				}


				int gifSpeed = c250dyn.Get<int>("field_2017");
				if (class_115.method_198(SDL.enum_160.SDLK_1))
				{
					gifSpeed = Math.Max(1, gifSpeed - 1);
					class_238.field_1991.field_1823.method_28(1f);
				}
				if (class_115.method_198(SDL.enum_160.SDLK_2))
				{
					gifSpeed = Math.Min(1000, gifSpeed + 1);
					class_238.field_1991.field_1823.method_28(1f);
				}
				Vector2 vector2_2 = (class_115.field_1433 / 2).Rounded() + new Vector2(270f, 355f);
				class_135.method_292("Gif Speed: " + gifSpeed.ToString(), vector2_2, class_238.field_1990.field_2144, class_181.field_1718, (enum_0)1, 1f, 0.25f, float.MaxValue, float.MaxValue, 0, new Color(), (class_256)null, int.MaxValue);
				c250dyn.Set("field_2017", gifSpeed);
			}

			c250dyn.Set("field_2028", field2028);
			c250dyn.Set("field_2029", (field2029 + 1 < 1) ? int.MaxValue-1 : field2029);
		}

		public static void class250_Method_50(On.class_250.orig_method_50 orig, class_250 c250Self, float param_4165)
		{
			//completely reimplements the original method
			var c250dyn = new DynamicData(c250Self);

			Vector2 FIELD2015 = new Vector2(802, 533);

			RenderTargetHandle FIELD2020 = c250dyn.Get<RenderTargetHandle>("field_2020");
			RenderTargetHandle FIELD2021 = c250dyn.Get<RenderTargetHandle>("field_2021");
			class_194 FIELD2022 = c250dyn.Get<class_194>("field_2022");
			Vector2 FIELD2023 = c250dyn.Get<Vector2>("field_2023");
			Vector2 FIELD2024 = c250dyn.Get<Vector2>("field_2024");
			bool FIELD2025 = c250dyn.Get<bool>("field_2025");
			bool FIELD2026 = c250dyn.Get<bool>("field_2026");
			int FIELD2027 = c250dyn.Get<int>("field_2027");
			int StartCycle = c250dyn.Get<int>("field_2028");
			int StopCycle = c250dyn.Get<int>("field_2029");
			bool AdvancedRecordingMode = c250dyn.Get<bool>("field_2030");
			class_92 FIELD2031 = c250dyn.Get<class_92>("field_2031");

			MethodInfo Method_671 = typeof(class_250).GetMethod("method_671", BindingFlags.NonPublic | BindingFlags.Instance);
			MethodInfo Method_672 = typeof(class_250).GetMethod("method_672", BindingFlags.NonPublic | BindingFlags.Instance);

			
			if (!FIELD2026 || FIELD2022.method_505())
			{
				Matrix4 matrix4_1 = Matrix4.method_1081(FIELD2023.FlooredToInt(), Renderer.method_1304());
				Matrix4 matrix4_2 = Matrix4.method_1069();
				class_95 class95_1 = FIELD2020.method_1351();
				using (class_226.method_598((interface_3)class95_1, class95_1.method_93(), matrix4_1, matrix4_2))
				{
					class_226.method_600(Color.White);
					Vector2 field2023 = FIELD2023;
					Bounds2 bounds2 = Bounds2.WithCorners(0.0f, 0.0f, field2023.X, field2023.Y);
					FIELD2022.method_1984(FIELD2024, bounds2, bounds2, false, (Maybe<List<Molecule>>)struct_18.field_1431, true);
					FIELD2022.method_508(!FIELD2025);
				}
				Matrix4 matrix4_3 = Matrix4.method_1081(FIELD2021.field_2987, !Renderer.method_1304());
				class_95 class95_2 = FIELD2021.method_1351();
				using (class_226.method_598((interface_3)class95_2, class95_2.method_93(), matrix4_3, matrix4_2))
				{
					Solution solution = FIELD2022.method_502();
					enum_133 key = solution.method_1934().field_2779.method_1085() ? enum_133.Instructions : enum_133.Footprint;
					Texture class256 = GifFrame;

					class_135.method_263(FIELD2020.method_1351().field_937, Color.White, new Vector2(12f, 57f), FIELD2015);
					Vector2 vector2 = new Vector2(0.0f, 0.0f);
					class_135.method_272(class256, vector2);
					//draw the solution name
					class_135.method_290(solution.method_1934().field_2767.method_1058().method_634(), new Vector2(26f, 26f), class_238.field_1990.field_2144, Color.White, (enum_0)0, 1f, 0.6f, float.MaxValue, 300f, -2, Color.Black, class_238.field_1989.field_99.field_706.field_751, int.MaxValue, false, true);
					//draw the metric names
					class_135.method_290("COST:", new Vector2(364f, 27f), class_238.field_1990.field_2142, Color.White, (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, -2, Color.Black, class_238.field_1989.field_99.field_706.field_751, int.MaxValue, false, true);
					class_135.method_290("CYCLES:", new Vector2(527f, 27f), class_238.field_1990.field_2142, Color.White, (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, -2, Color.Black, class_238.field_1989.field_99.field_706.field_751, int.MaxValue, false, true);
					class_135.method_290(solution.method_1934().field_2779.method_1085() ? "INSTRS:" : "AREA:", new Vector2(690f, 27f), class_238.field_1990.field_2142, Color.White, (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, -2, Color.Black, class_238.field_1989.field_99.field_706.field_751, int.MaxValue, false, true);
					//draw the metric scores
					class_135.method_290(class_140.method_319(enum_133.Cost, solution.field_3918[enum_133.Cost]), new Vector2(445f, 26f), class_238.field_1990.field_2144, class_181.field_1719, (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, 0, new Color(), (class_256)null, int.MaxValue, false, true);
					class_135.method_290(solution.field_3918[enum_133.Cycles].ToString(), new Vector2(608f, 26f), class_238.field_1990.field_2144, class_181.field_1719, (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, 0, new Color(), (class_256)null, int.MaxValue, false, true);
					class_135.method_290(solution.field_3918[key].ToString(), new Vector2(771f, 26f), class_238.field_1990.field_2144, class_181.field_1719, (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, 0, new Color(), (class_256)null, int.MaxValue, false, true);
				}
			}
			if (!FIELD2026 && FIELD2022.method_505())
			{
				FIELD2031.method_78();
				FIELD2026 = true;
			}
			if (FIELD2025 && !FIELD2026)
			{
				FIELD2031.method_76(Renderer.method_1313(FIELD2021.method_1351().field_937));
				if ((AdvancedRecordingMode ? (FIELD2022.method_500().method_1818() >= StopCycle ? 1 : 0) : ((int)Method_672.Invoke(c250Self, new object[] { }) != FIELD2027 ? 0 : ((int)Method_671.Invoke(c250Self, new object[] { }) > 1 ? 1 : 0))) != 0)
				{
					FIELD2031.method_77();
					FIELD2026 = true;
					StopCycle = FIELD2022.method_500().method_1818();
				}
			}
			if (!FIELD2025 & (AdvancedRecordingMode ? FIELD2022.method_500().method_1818() >= StartCycle : (int)Method_671.Invoke(c250Self, new object[] { }) > 0))
			{
				FIELD2025 = true;
				FIELD2027 = (int)Method_672.Invoke(c250Self, new object[] { });
				StartCycle = FIELD2022.method_500().method_1818();
			}
			if (!AdvancedRecordingMode && class_115.method_198(SDL.enum_160.SDLK_F10))
			{
				AdvancedRecordingMode = true;
				FIELD2025 = true;
				if (!FIELD2026)
				{
					FIELD2031.method_78();
					FIELD2026 = true;
					StartCycle = 0;
					StopCycle = 1;
				}
			}
			if (AdvancedRecordingMode && FIELD2026)
			{
				if (class_115.method_200(SDL.enum_160.SDLK_F1))
				{
					StartCycle = Math.Max(0, StartCycle - 1);
					class_238.field_1991.field_1823.method_28(1f);
				}
				if (class_115.method_200(SDL.enum_160.SDLK_F2))
				{
					StartCycle = Math.Min(StartCycle + 1, StopCycle - 1);
					class_238.field_1991.field_1823.method_28(1f);
				}
				if (class_115.method_200(SDL.enum_160.SDLK_F3))
				{
					StopCycle = Math.Max(StartCycle + 1, StopCycle - 1);
					class_238.field_1991.field_1823.method_28(1f);
				}
				if (class_115.method_200(SDL.enum_160.SDLK_F4))
				{
					++StopCycle;
					class_238.field_1991.field_1823.method_28(1f);
				}
				if (class_115.method_198(SDL.enum_160.SDLK_F5))
				{
					class_250 class250 = new class_250(FIELD2022.method_502());

					var class250dyn = new DynamicData(class250);
					class250dyn.Set("field_2028", StartCycle);
					class250dyn.Set("field_2029", StopCycle);
					class250dyn.Set("field_2030", true);
					GameLogic.field_2434.method_949();
					GameLogic.field_2434.method_946((IScreen)class250);
					class_238.field_1991.field_1821.method_28(1f);
				}
			}
			Vector2 vector2_1 = new Vector2(1115f, 735f);
			Vector2 vector2_2 = (class_115.field_1433 / 2 - vector2_1 / 2).Rounded() + new Vector2(0.0f, -10f);
			if ((double)class_115.field_1433.Y >= 1080.0)
				vector2_2.Y += 110f;
			Vector2 position = vector2_2 + new Vector2(78f, 88f);
			Vector2 size = vector2_1 + new Vector2(-152f, -158f);
			class_135.method_268(class_238.field_1989.field_102.field_810, Color.White, position, Bounds2.WithSize(position, size));
			class_135.method_276(class_238.field_1989.field_102.field_817, Color.White, vector2_2, vector2_1);
			class_140.method_317((string)class_134.method_253("Solution Recorder", string.Empty), vector2_2 + new Vector2(95f, 636f), 900, true, true);
			if (class_140.method_323(vector2_2, vector2_1, new Vector2(1011f, 637f)))
			{
				if (!FIELD2026)
					FIELD2031.method_78();
				GameLogic.field_2434.method_949();
				class_238.field_1991.field_1821.method_28(1f);
			}
			Vector2 vector2_3 = FIELD2015 * 0.8f;
			Vector2 vector2_4 = vector2_2 + new Vector2(243f, 173f);
			class_135.method_263(FIELD2020.method_1351().field_937, Color.White, vector2_4, vector2_3);
			class_135.method_276(class_238.field_1989.field_101.field_789, Color.White, vector2_4 + new Vector2(-9f, -9f), vector2_3 + new Vector2(17f, 17f));
			string str = !FIELD2022.method_505() ? (!FIELD2026 ? (string)class_134.method_253("Recording your solution...", string.Empty) : (string)class_134.method_253("An animated GIF has been saved to your desktop.", string.Empty)) : class_134.method_253("Error", string.Empty).method_1060() + ": " + (string)class_134.method_253("An impending collision or conflict in your solution has stopped the recording process.", string.Empty);
			if (AdvancedRecordingMode)
				str += string.Format("\n\nStart Cycle: {0} (F1/F2)  -  Stop Cycle: {1} (F3/F4)  -  Record (F5)", (object) StartCycle, (object) StopCycle);
			class_135.method_292(str, vector2_2 + new Vector2(562f, 131f), class_238.field_1990.field_2144, class_181.field_1718, (enum_0)1, 1f, 0.25f, float.MaxValue, float.MaxValue, 0, new Color(), (class_256)null, int.MaxValue);

			//
			c250dyn.Set("field_2020", FIELD2020);
			c250dyn.Set("field_2021", FIELD2021);
			c250dyn.Set("field_2022", FIELD2022);
			c250dyn.Set("field_2023", FIELD2023);
			c250dyn.Set("field_2024", FIELD2024);
			c250dyn.Set("field_2025", FIELD2025);
			c250dyn.Set("field_2026", FIELD2026);
			c250dyn.Set("field_2027", FIELD2027);
			c250dyn.Set("field_2028", StartCycle);
			c250dyn.Set("field_2029", StopCycle);
			c250dyn.Set("field_2030", AdvancedRecordingMode);
			c250dyn.Set("field_2031", FIELD2031);
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
			////////////////////
			//new code, part I//
			////////////////////
			float panel_y = 0;
			//update MetricDisplaySwitch and draw metricTabs
			if (keyPressed(SDL.enum_160.SDLK_6))
			{
				if (class_115.method_193((enum_143)1)) { MetricDisplaySwitch = 0; }
				else MetricDisplaySwitch += (class_115.method_193(0) ? -1 : 1);//subtract if SHIFT is held
			}
			MetricDisplaySwitch = (MetricDisplaySwitch + numberOfMetricDisplays) % numberOfMetricDisplays;

			float tabWidth = 48f;
			for (int i = numberOfMetricDisplays - 1; i >= 0; i--)
			{
				float t = (i == MetricDisplaySwitch) ? 1f : 0f;
				float x = (3 * metricTabHeights[i] + t) / 4;
				metricTabHeights[i] = x;
				class_135.method_272(ProgramPanelMetricTab, new Vector2(class_115.field_1433.X - 310f - (numberOfMetricDisplays - i) * tabWidth, panel_y + 250f + 20 * x));
			}

			/////////////////
			//original code//
			/////////////////
			orig(SEPPSelf, param_5658);
			/////////////////////
			//new code, part II//
			/////////////////////
			var SES = new DynamicData(SEPPSelf).Get<SolutionEditorScreen>("field_2007");
			var maybeSim = new DynamicData(SES).Get<Maybe<Sim>>("field_4022");
			bool SIM_exists = maybeSim.method_1085();
			Sim SIM = null;
			if (SIM_exists) SIM = maybeSim.method_1087();

			bool simRunning = (SES.method_503() != enum_128.Stopped);
			if (!simRunning) AreaManager.clear();

			//hide the display drawn by the game
			class_135.method_272(ProgramPanelMetricHider, new Vector2(class_115.field_1433.X - 944f, panel_y + 266f));

			///////////////////
			//compute metrics//
			///////////////////
			bool isProduction = SES.method_502().method_1934().field_2779.method_1085();
			int gold = SES.method_502().method_1954();
			int cycles = SES.field_4017.method_1090(SES.method_2127());
			int area = SES.field_4018.method_1090(SES.method_2128());

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
			puzzleSolved = (productsAccepted >= productsExpected);

			//area hashset
			if (!simRunning)
			{
				puzzleSolved = false;
				//compute height,width,area of parts manually instead of waiting for simulation to start////////////////////////////////////
				blueprintValue = AreaManager.fetchBlueprintMetric(SES);
				heightValue = 0;
				widthValue = 0;

			}
			if (simRunning)
			{
				if (SIM_exists)
				{
					HashSet<HexIndex> areaHashSet = SIM.field_3824;
					int y_min = int.MaxValue;
					int y_max = int.MinValue;
					int ww_min = int.MaxValue;
					int ww_max = int.MinValue;
					if (areaHashSet.Count > 0 && !puzzleSolved)
					{
						int ww, y;
						foreach (HexIndex hex in areaHashSet)
						{
							y = hex.R;
							ww = 2*hex.Q + hex.R;

							y_min = Math.Min(y_min, y);
							y_max = Math.Max(y_max, y);
							ww_min = Math.Min(ww_min, ww);
							ww_max = Math.Max(ww_max, ww);

							heightValue = y_max - y_min + 1;
							widthValue = ww_max - ww_min + 2;
						}
					}
				}
			}
			//parts list
			int arms = 0;
			int grippers = 0;
			int track = 0;
			int overlaps = 0;////////////////////////////////////

			var solution = new DynamicData(SES).Get<Solution>("field_4004");
			if (DEBUG) DEBUG_script(solution);
			DEBUG = false;
			foreach (Part part in solution.field_3919)
			{
				PartType partType = part.method_1159();
				if (partType.field_1533) arms += 1;
				grippers += partType.field_1534.Length;

				var HexList1 = new DynamicData(part).Get<List<HexIndex>>("field_2700");
				if (HexList1 != null) track += HexList1.Count;
			}
			//instruction grid
			int instructions = -1;
			int period = -1;
			int vintage_instructions = -1;
			int assembly_instructions = -1;

			maybeSim = Sim.method_1824((SolutionEditorBase)SES);

			if (maybeSim.method_1085())
			{
				CompiledProgramGrid compiledProgramGrid = maybeSim.method_1087().method_1820();
				var programGridDict = new DynamicData(compiledProgramGrid).Get<Dictionary<Part, CompiledProgram>>("field_2368");
				instructions = compiledProgramGrid.method_854();
				period = 0;
				vintage_instructions = 0;
				assembly_instructions = 0;
				foreach (var ENTRY in programGridDict)
				{
					//count assembly instructions
					Part part = ENTRY.Key;
					EditableProgram assemblyProgram = part.field_2697;
					var assemblyDict = new DynamicData(assemblyProgram).Get<SortedDictionary<int, InstructionType>>("field_2415");
					assembly_instructions += assemblyDict.Count;

					//find period and count vintage instructions
					CompiledInstruction[] compiledArray = ENTRY.Value.field_2367;
					int len = compiledArray.Length;
					period = Math.Max(period, len);
					foreach (var i in compiledArray)
					{
						switch (i.field_2364.field_2542)
						{
							//Vintage instructions
							case 'I'://empty
							case 'i'://  instruction
							case 'R'://rotate CW
							case 'r'://rotate CCW
							case 'G'://grab
							case 'g'://drop
							case 'O'://period override
							//case 'X'://reset
							//case 'C'://repeat
								break;

							//Non-vintage instructions
							case 'A'://advance (+)
							case 'a'://advance (-)
							case 'E'://extend
							case 'e'://retract
							case 'P'://pivot CW
							case 'p'://pivot CCW
							default:
								vintage_instructions += 1;
								break;
						}
					}
				}
			}

			

			///////////////////
			//display metrics//
			///////////////////
			
			string FUNC(int n)
			{
				return n >= 0 ? n.method_453() : "----";
			}

			string productFraction = simRunning ? string.Format("{0}/{1}", (object)productsAccepted, (object)productsExpected) : "----";
			double geomean = Math.Pow(gold * cycles * (isProduction ? instructions : area), 1.0 / 3.0);
			string geomeanString = Math.Round(geomean,2).ToString();

			int sum = gold + cycles + (isProduction ? instructions : area);

			switch (MetricDisplaySwitch)
			{
				default:
					DrawMetricTuple("Products", productFraction, CostMetric, gold.method_453() + "$", "Cycles", simRunning ? cycles.method_453() : "----", isProduction ? "Instrs" : "Area", isProduction ? FUNC(instructions) : (area >= 0 ? area.method_453() : "----"), panel_y);
					MetricDisplaySwitch = 0;
					break;
				case 1:
					DrawMetricTuple("Latency", simRunning ? latencyValue.method_453() : "----", "Sum", simRunning ? sum.method_453() : "----", "Sum4", simRunning ? (gold + cycles + area + instructions).method_453() : "----", !isProduction ? "Instrs" : "Area", !isProduction ? FUNC(instructions) : (area >= 0 ? area.method_453() : "----"), panel_y);
					break;
				case 2:
					DrawMetricTuple("Overlaps", /*overlaps.method_453()*/"<null>", "Track", track.method_453(), "Height", simRunning ? heightValue.method_453() : "----", "Width", simRunning ? (widthValue / 2).method_453() + (widthValue % 2==0 ? ".0" : ".5") : "----", panel_y);
					break;
				case 3:
					DrawMetricTuple("Period", FUNC(period), "Geo-mean", simRunning ? geomeanString : "----", "Vtg I", FUNC(vintage_instructions), "Assm I", FUNC(assembly_instructions), panel_y);
					break;
				case 4:
					DrawMetricTuple("Blueprint", blueprintValue.method_453(), "Arms", arms.method_453(), "Grippers", grippers.method_453(), "----", "----", panel_y);
					break;
				//case 5:
			}
		}

		public static void DEBUG_script(Solution solution)
		{
			Logger.Log("FTSIGCTU: Fetching " + solution.field_3919.Count + " parts:");
			foreach (Part part in solution.field_3919)
			{
				PartType partType = part.method_1159();
				Logger.Log("  PART(" + part.method_1161().Q + "," + part.method_1161().R + ")");//location

				if (partType == class_191.field_1760) Logger.Log("    Type = Input");
				if (partType == class_191.field_1761) Logger.Log("    Type = Output");
				if (partType == class_191.field_1762) Logger.Log("    Type = Output, Infinite");
				if (partType == class_191.field_1763) Logger.Log("    Type = Pipe");
				if (partType == class_191.field_1764) Logger.Log("    Type = Arm1");
				if (partType == class_191.field_1765) Logger.Log("    Type = Arm2");
				if (partType == class_191.field_1766) Logger.Log("    Type = Arm3");
				if (partType == class_191.field_1767) Logger.Log("    Type = Arm6");
				if (partType == class_191.field_1768) Logger.Log("    Type = Piston");
				if (partType == class_191.field_1769) Logger.Log("    Type = Gripper");
				if (partType == class_191.field_1770) Logger.Log("    Type = Track");
				if (partType == class_191.field_1771) Logger.Log("    Type = Berlo");
				if (partType == class_191.field_1772) Logger.Log("    Type = Bonder");
				if (partType == class_191.field_1773) Logger.Log("    Type = Unbonder");
				if (partType == class_191.field_1774) Logger.Log("    Type = Multibonder");
				if (partType == class_191.field_1775) Logger.Log("    Type = Triplex Bonder");
				if (partType == class_191.field_1776) Logger.Log("    Type = Calcifier");
				if (partType == class_191.field_1777) Logger.Log("    Type = Duplicator");
				if (partType == class_191.field_1778) Logger.Log("    Type = Projector");
				if (partType == class_191.field_1779) Logger.Log("    Type = Purifier");
				if (partType == class_191.field_1780) Logger.Log("    Type = Animismus");
				if (partType == class_191.field_1781) Logger.Log("    Type = Disposal");
				if (partType == class_191.field_1782) Logger.Log("    Type = Equilibrium");
				if (partType == class_191.field_1783) Logger.Log("    Type = Dispersion");
				if (partType == class_191.field_1784) Logger.Log("    Type = Unification");

				Logger.Log("    #ofArms = " + partType.field_1534.Length);
				Logger.Log("    Footprint = " + partType.field_1540.Length);
				Logger.Log("    Is Track = " + (partType.field_1542 ? "true" : "false"));
				Logger.Log("    Is Conduit = " + (partType.field_1543 ? "true" : "false"));
				Logger.Log("    Field1533 (Programmable?) = " + (partType.field_1533 ? "true" : "false"));
				Logger.Log("    Field1536 (Arm?) = " + (partType.field_1536 ? "true" : "false"));

				//if field_1533 and field_1536 then arm

				var HexList1 = new DynamicData(part).Get<List<HexIndex>>("field_2700");
				if (HexList1 != null && HexList1.Count > 0)
				{
					Logger.Log("    HexList1:");//hexes covered by track
					foreach (HexIndex hex in HexList1)
					{
						Logger.Log("      " + hex.Q + "," + hex.R);//location
					}
				}

				var HexList2 = part.method_1173();//hexes covered by pipes
				if (HexList2 != null && HexList2.Count > 0)
				{
					Logger.Log("    HexList2:");
					foreach (HexIndex hex in HexList2)
					{
						Logger.Log("      " + hex.Q + "," + hex.R);//location
					}
				}
			}
		}
	}
}