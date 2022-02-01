using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU
{
	using PartType = class_139;
	//using Permissions = enum_149;
	//using BondType = enum_126;
	//using BondSite = class_222;
	//using AtomTypes = class_175;
	//using PartTypes = class_191;
	using Texture = class_256;

	public class FTSIGCTU : QuintessentialMod
	{
		//global variables and assets
		public static bool DEBUG = true;

		public static int MetricDisplaySwitch = 0;
		public static bool puzzleSolved = false;
		public static bool latencyFound = false;
		public static int latencyValue = 0;
		public static int heightValue = 0;
		public static int widthValue = 0;
		public static int blueprintValue = 0;
		public static string CostMetric = "Cost";

		public static Texture ProgramPanelMetricHider;
		public static Texture ProgramPanelMetricTab;
		public const int numberOfMetricDisplays = 5;
		public static float[] metricTabHeights = new float[numberOfMetricDisplays];

		//upgrade to new version of Quintessential that has built-in methods for this keyboard crap/////////////////////////////////////////////
		public static bool keyHeld(SDL2.SDL.enum_160 KEY) => class_115.method_192(KEY);
		public static bool keyPressed(SDL2.SDL.enum_160 KEY) => class_115.method_198(KEY);
		public static bool keyReleased(SDL2.SDL.enum_160 KEY) => class_115.method_199(KEY);

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
			ProgramPanelMetricTab = class_235.method_615("textures/solution_editor/program_panel/metric_tab");
		}

		public override void Unload()
		{
			//
		}

		public override void PostLoad()	{
			//
			On.SolutionEditorProgramPanel.method_221 += Method_221;
			On.CompiledProgramGrid.method_853 += Method_853;
			On.class_250.method_50 += GifRecorder_Method_50;
		}

		public static int Method_853(On.CompiledProgramGrid.orig_method_853 orig, CompiledProgramGrid CPGSelf, int param_4510)
		{
			//completely reimplements the original method
			var programGridDict = new DynamicData(CPGSelf).Get<Dictionary<Part, CompiledProgram>>("field_2368");
			
			int period = 1;
			foreach (var entry in programGridDict)
			{
				period = Math.Max(period,entry.Value.field_2367.Length);
				break;//only need to check a single part
			}

			return param_4510 % period;
		}



		public static void GifRecorder_Method_50(On.class_250.orig_method_50 orig, class_250 c250Self, float param_4165)
		{
			orig(c250Self, param_4165);

			var c250dyn = new DynamicData(c250Self);
			int field2028 = c250dyn.Get<int>("field_2028");
			int field2029 = c250dyn.Get<int>("field_2029");

			if (new DynamicData(c250Self).Get<bool>("field_2030") && new DynamicData(c250Self).Get<bool>("field_2026"))
			{
				if (class_115.method_193(0) && class_115.method_198(SDL2.SDL.enum_160.SDLK_F1))
				{
					field2028 = Math.Max(0, (field2028 + 1) / 10);
					class_238.field_1991.field_1823.method_28(1f);
				}
				if (class_115.method_193(0) && class_115.method_198(SDL2.SDL.enum_160.SDLK_F2))
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
				if (class_115.method_193(0) && class_115.method_198(SDL2.SDL.enum_160.SDLK_F3))
				{
					field2029 = Math.Max(field2028 + 1, (field2029 + 1) / 10);
					class_238.field_1991.field_1823.method_28(1f);
				}
				if (class_115.method_193(0) && class_115.method_198(SDL2.SDL.enum_160.SDLK_F4))
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
			}

			c250dyn.Set("field_2028", field2028);
			c250dyn.Set("field_2029", (field2029 + 1 < 1) ? int.MaxValue-1 : field2029);
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
			if (keyPressed(SDL2.SDL.enum_160.SDLK_6))
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

			//hide the display drawn by the game
			class_135.method_272(ProgramPanelMetricHider, new Vector2(class_115.field_1433.X - 944f, panel_y + 266f));

			///////////////////
			//compute metrics//
			///////////////////
			bool simRunning = (SES.method_503() != enum_128.Stopped);
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
				blueprintValue = 0;
				heightValue = 0;
				widthValue = 0;
			}
			if (simRunning)
			{
				if (cycles == 0) { blueprintValue = area; }//compute area of parts up above////////////////////////////////////

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
			int arms = 0;////////////////////////////////////
			int grippers = 0;////////////////////////////////////
			int track = 0;////////////////////////////////////
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
			CompiledProgramGrid programGrid = Sim.method_1824((SolutionEditorBase)SES).method_1087().method_1820();
			var programGridDict = new DynamicData(programGrid).Get<Dictionary<Part, CompiledProgram>>("field_2368");
			int instructions = programGrid.method_854();
			int period = 0;
			int vintage_instructions = 0;
			int assembly_instructions = 0;
			if (programGridDict.Count > 0)
			{
				//
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
			string productFraction = simRunning ? string.Format("{0}/{1}", (object)productsAccepted, (object)productsExpected) : "----";
			switch (MetricDisplaySwitch)
			{
				default:
					DrawMetricTuple("Products", productFraction, CostMetric, gold.method_453() + "$", "Cycles", simRunning ? cycles.method_453() : "----", isProduction ? "Instrs" : "Area", isProduction ? instructions.method_453() : (area >= 0 ? area.method_453() : "----"), panel_y);
					MetricDisplaySwitch = 0;
					break;
				case 1:
					DrawMetricTuple("Latency", simRunning ? latencyValue.method_453() : "----", "Sum", simRunning ? (gold + cycles + (isProduction ? instructions : area)).method_453() : "----", "Sum4", simRunning ? (gold + cycles + area + instructions).method_453() : "----", !isProduction ? "Instrs" : "Area", !isProduction ? instructions.method_453() : (area >= 0 ? area.method_453() : "----"), panel_y);
					break;
				case 2:
					DrawMetricTuple("Overlaps", /*overlaps.method_453()*/"<null>", "Track", track.method_453(), "Height", simRunning ? heightValue.method_453() : "----", "Width", simRunning ? (widthValue / 2).method_453() + (widthValue % 2==0 ? ".0" : ".5") : "----", panel_y);
					break;
				case 3:
					DrawMetricTuple("Period", period.method_453(), "----", "----", "Vtg I", vintage_instructions.method_453(), "Assm I", assembly_instructions.method_453(), panel_y);
					break;
				case 4:
					DrawMetricTuple("Blueprint", simRunning ? blueprintValue.method_453() : "----", "Arms", arms.method_453(), "Grippers", grippers.method_453(), "----", "----", panel_y);
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