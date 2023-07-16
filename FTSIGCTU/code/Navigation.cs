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
using PartType = class_139;

public static class Navigation
{
	//data structs, enums, variables
	private static bool showCritelliOnMap = true;

	//private static Vector2 screenPosition = new Vector2(0f, 0f);
	//private static bool screenIsHoming = false;
	//private static float screenHomingParameter = 0f;

	//---------------------------------------------------//
	//public APIs and resources

	//---------------------------------------------------//
	//internal helper methods

	//---------------------------------------------------//
	//internal main methods
	public static void ApplySettings(bool _showCritelliOnMap)
	{
		showCritelliOnMap = _showCritelliOnMap;
	}

	//---------------------------------------------------//

	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		if (Input.IsSdlKeyPressed(SDL.enum_160.SDLK_m))
		{
			Sound ui_paper = class_238.field_1991.field_1874;
			common.playSound(ui_paper);
			GameLogic.field_2434.method_946(new Map(SES_self));
		}
	}

	public static void SEPP_method_221(SolutionEditorProgramPanel sepp_self)
	{
		/*
		var seppDyn = new DynamicData(sepp_self);
		SolutionEditorScreen ses = seppDyn.Get<SolutionEditorScreen>("field_2007");

		Vector2 homePosition = 0.5f * Input.ScreenSize() + new Vector2(140, 180);

		if (Input.IsRightClickPressed())
		{
			screenIsHoming = Input.IsControlHeld();
			if (screenIsHoming)
			{
				screenHomingParameter = 0f;
				screenPosition = ses.field_4009;
				class_158.method_376(class_238.field_1991.field_1869.field_4061, class_269.field_2109, false);
			}
		}

		if (screenIsHoming)
		{
			screenHomingParameter += 0.02f;
			float arg = 0.5f + 0.5f * (float)Math.Cos(Math.PI * (1 - screenHomingParameter));
			ses.field_4009 = arg * homePosition + (1 - arg) * screenPosition;

			if (screenHomingParameter >= 1f)
			{
				ses.field_4009 = homePosition;
				screenIsHoming = false;
			}
		}
		*/
	}


	public static void LoadPuzzleContent()
	{
		string path = "ftsigctu/textures/solution_editor/navigation/";
		Map.t_hexagon = class_235.method_615(path + "hexagon");
		Map.t_armbase = class_235.method_615(path + "armbase");
		Map.t_square = class_235.method_615(path + "square");

		//DEBUG -- how do we draw arms on top of track? do we even bother?
		Map.t_armbase = Map.t_hexagon;


		Func<SolutionEditorScreen, Part, List<Map.hex>> mapHexRulemaker(Func<SolutionEditorScreen, Part, List<HexIndex>> hexFinder, Texture texture, Color color, int priority)
		{
			List<Map.hex> func(SolutionEditorScreen ses, Part part)
			{
				List<Map.hex> ret = new();
				foreach (var hex in hexFinder(ses,part))
				{
					ret.Add(new Map.hex(hex, texture, color, priority));
				}
				return ret;
			}
			return func;
		}

		List<HexIndex> ioHexRule(SolutionEditorScreen ses, Part part)
		{
			List<HexIndex> ret = new();
			var solution = ses.method_502();
			Molecule molecule = part.method_1185(solution).method_1115(common.getPartRotation(part)).method_1117(common.getPartOrigin(part));
			var atomDict = new DynamicData(molecule).Get<Dictionary<HexIndex, Atom>>("field_2642");
			return atomDict.Keys.ToList();
		}

		var armRule = mapHexRulemaker((ses, part) => new List<HexIndex> { part.method_1161() }, Map.t_armbase, Map.c_arm, Map.p_arm);
		var glyphRule = mapHexRulemaker((ses, part) => common.getFootprintList(part), Map.t_hexagon, Map.c_glyph, Map.p_glyph);

		Map.addHexRule(common.MechanismArm1()		, armRule);
		Map.addHexRule(common.MechanismArm2()		, armRule);
		Map.addHexRule(common.MechanismArm3()		, armRule);
		Map.addHexRule(common.MechanismArm6()		, armRule);
		Map.addHexRule(common.MechanismPiston()		, armRule);
		Map.addHexRule(common.MechanismBerlo()		, armRule);
		Map.addHexRule(common.MechanismTrack()		, mapHexRulemaker((ses, part) => common.getTrackList(part), Map.t_hexagon, Map.c_track, Map.p_track));

		Map.addHexRule(common.GlyphEquilibrium()	, mapHexRulemaker((ses, part) => new List<HexIndex> { part.method_1161() }, Map.t_hexagon, Map.c_equilibrium, Map.p_equilibrium));
		Map.addHexRule(common.GlyphCalcification()	, glyphRule);
		Map.addHexRule(common.GlyphDisposal()		, glyphRule);
		Map.addHexRule(common.GlyphBonder()			, glyphRule);
		Map.addHexRule(common.GlyphUnbonder()		, glyphRule);
		Map.addHexRule(common.GlyphMultiBonder()	, glyphRule);
		Map.addHexRule(common.GlyphDuplication()	, glyphRule);
		Map.addHexRule(common.GlyphProjection()		, glyphRule);
		Map.addHexRule(common.GlyphTriplexBonder()	, glyphRule);
		Map.addHexRule(common.GlyphPurification()	, glyphRule);
		Map.addHexRule(common.GlyphAnimismus()		, glyphRule);
		Map.addHexRule(common.GlyphUnification()	, glyphRule);
		Map.addHexRule(common.GlyphDispersion()		, glyphRule);

		Map.addHexRule(common.IOConduit()			, mapHexRulemaker((ses, part) => common.getConduitList(part), Map.t_hexagon, Map.c_conduit, Map.p_glyph));
		Map.addHexRule(common.IOInput()				, mapHexRulemaker(ioHexRule, Map.t_hexagon, Map.c_input, Map.p_glyph));
		Map.addHexRule(common.IOOutputStandard()	, mapHexRulemaker(ioHexRule, Map.t_hexagon, Map.c_output, Map.p_glyph));
		Map.addHexRule(common.IOOutputInfinite()	, mapHexRulemaker(ioHexRule, Map.t_hexagon, Map.c_output, Map.p_glyph));
	}


	public sealed class Map : IScreen
	{
		private readonly SolutionEditorScreen ses;
		private readonly Dictionary<HexIndex, Map.hex> stationaryMapHexes;
		Func<HexIndex, Vector2> positionConverter;
		Func<Vector2, SolutionEditorScreen, Vector2> screenpositionConverter;
		Func<Vector2, Vector2>  repositionScreen;
		Texture letter6;
		Vector2 mapResolution = new Vector2(950, 550);
		Vector2 mapOffset = new Vector2(155, 155);
		Vector2 mapHexDimensions => class_187.field_1742.field_1744;//= new Vector2(80, 70);
		Vector2 mapOrigin;
		float mapFactor;

		Vector2 boardhexDimensions => class_187.field_1742.field_1744;


		static Dictionary<PartType, Func<SolutionEditorScreen, Part, List<Map.hex>>> hexRules = new();

		static Color Color_RGBA(int r, int g, int b, float alpha = 1f)
		{
			return Color.FromHex(r * 256 * 256 + g * 256 + b).WithAlpha(alpha);
		}

		public static readonly Color c_chamber		= Color_RGBA(128,  64,   0, 0.75f);
		public static readonly Color c_equilibrium	= Color_RGBA(128, 128, 128, 0.75f);
		public static readonly Color c_glyph		= Color_RGBA( 64,  64,  64, 0.75f);
		public static readonly Color c_input		= Color_RGBA(  0, 128, 192, 0.75f);
		public static readonly Color c_output		= Color_RGBA(  0, 128,  64, 0.75f);
		public static readonly Color c_conduit		= Color_RGBA(128,   0, 128, 0.75f);
		public static readonly Color c_track		= Color_RGBA(  0,   0,   0, 0.75f);
		public static readonly Color c_arm			= Color_RGBA(192, 192, 192, 0.75f);
		public static readonly Color c_critelli		= Color_RGBA(128,   0,   0, 0.75f);

		public static readonly int p_board = -1000000;
		public static readonly int p_equilibrium = -1000;
		public static readonly int p_glyph = 0;
		public static readonly int p_track = 1000;
		public static readonly int p_arm = 1000000;

		public static Texture t_hexagon, t_armbase, t_square;

		public static void addHexRule(PartType partType, Func<SolutionEditorScreen, Part, List<Map.hex>> rule)
		{
			if (hexRules.Keys.Contains(partType))
			{
				Logger.Log($"FTSIGCTU.Navigation.Map.addRule: partType {partType.field_1529} already has a hexRule, ignoring.");
			}
			else
			{
				hexRules.Add(partType, rule);
			}
		}

		public Map(SolutionEditorScreen _ses)
		{
			this.ses = _ses;
			this.stationaryMapHexes = new();
			Solution solution = ses.method_502();

			Puzzle puzzle = solution.method_1934();
			var maybeChambers = puzzle.field_2779;
			if (maybeChambers.method_1085())
			{
				foreach (var chamber in maybeChambers.method_1087().field_2071)
				{
					foreach (var hex in chamber.field_1747.field_1729)
					{
						var wallHex = new Map.hex(hex + chamber.field_1746, Map.t_hexagon, Map.c_chamber, Map.p_board);
						wallHex.updateDict(stationaryMapHexes);
					}
				}
			}

			var partsList = solution.field_3919;
			foreach (var part in partsList.Where(x => hexRules.Keys.Contains(common.getPartType(x))))
			{
				foreach (var hex in hexRules[common.getPartType(part)](ses, part))
				{
					hex.updateDict(stationaryMapHexes);
				}
			}

			if (showCritelliOnMap || stationaryMapHexes.Count == 0)
			{
				Map.hex hex = new(new HexIndex(0,0), Map.t_armbase, Map.c_critelli, p_arm);
				hex.updateDict(stationaryMapHexes);
			}

			Vector2 convertvector(Vector2 vec, SolutionEditorScreen ses)
			{
				Vector2 vec2 = vec - ses.field_4009;
				float x = vec2.X / boardhexDimensions.X;
				float y = vec2.Y / boardhexDimensions.Y;
				return new Vector2(x, y);
			}
			double minX = convertvector(new Vector2(0,0), ses).X;
			double minY = convertvector(new Vector2(0,0), ses).Y;
			double maxX = convertvector(Input.ScreenSize(), ses).X;
			double maxY = convertvector(Input.ScreenSize(), ses).Y;

			foreach (var hex in stationaryMapHexes.Keys)
			{
				double x = hex.Q + hex.R / 2f;
				double y = hex.R;
				minX = Math.Min(minX, x);
				minY = Math.Min(minY, y);
				maxX = Math.Max(maxX, x);
				maxY = Math.Max(maxY, y);
			}

			double widthFactor = mapResolution.X / ((maxX - minX) * mapHexDimensions.X);
			double heightFactor = mapResolution.Y / ((maxY - minY) * mapHexDimensions.Y);

			mapFactor = (float) Math.Min(widthFactor, heightFactor);


			Vector2 convertScreenPosition(Vector2 vec, SolutionEditorScreen ses)
			{
				Vector2 vec2 = vec - ses.field_4009;
				double x = (vec2.X / boardhexDimensions.X - (maxX + minX) / 2) * mapHexDimensions.X;
				double y = (vec2.Y / boardhexDimensions.Y - (maxY + minY) / 2) * mapHexDimensions.Y;
				return mapOrigin + mapResolution / 2 + new Vector2((float)x, (float)y) * mapFactor;
			}

			Vector2 repositionScreenPosition(Vector2 vec)
			{
				Vector2 vec2 = (vec - mapOrigin - mapResolution / 2) / mapFactor;
				double y = (maxY + minY) / 2 + vec2.Y / mapHexDimensions.Y;
				double x = (maxX + minX) / 2 + vec2.X / mapHexDimensions.X;
				return Input.ScreenSize()/2 - new Vector2((float)x * boardhexDimensions.X, (float)y * boardhexDimensions.Y);
			}

			this.screenpositionConverter = convertScreenPosition;
			this.repositionScreen = repositionScreenPosition;


			letter6 = class_238.field_1989.field_85.field_571;
			mapOrigin = new Vector2(15, 15) + (Input.ScreenSize() / 2) - (common.textureDimensions(letter6) / 2) + mapOffset;

			Vector2 convertPosition(HexIndex hex)
			{
				double x = (hex.Q + hex.R / 2f - (maxX + minX) / 2) * mapHexDimensions.X;
				double y = (hex.R - (maxY + minY) / 2) * mapHexDimensions.Y;
				return mapOrigin + mapResolution/2 + new Vector2((float) x, (float) y) * mapFactor;
			}

			this.positionConverter = convertPosition;
		}


		public bool method_1037() => false;
		public void method_47(bool param_4183) => GameLogic.field_2434.field_2464 = true;
		public void method_48() { }
		public void method_50(float param_4184)
		{
			bool returnToEditorKeypress =
				Input.IsSdlKeyPressed(SDL.enum_160.SDLK_TAB)
				|| Input.IsSdlKeyPressed(SDL.enum_160.SDLK_SPACE)
				|| Input.IsSdlKeyPressed(SDL.enum_160.SDLK_ESCAPE)
				|| Input.IsSdlKeyPressed(SDL.enum_160.SDLK_m)
			;
			bool returnToEditor = returnToEditorKeypress;
			Sound ui_paper_back = class_238.field_1991.field_1875;
			if (returnToEditor)
			{
				GameLogic.field_2434.field_2464 = false;
				common.playSound(ui_paper_back);
				GameLogic.field_2434.method_949();
			}

			if (Input.IsLeftClickHeld())
			{
				ses.field_4009 = repositionScreen(Input.MousePos());
			}

			// draw map backdrop
			class_135.method_272(letter6, mapOrigin.Rounded() - mapOffset);

			// draw viewport
			float texScaling = Math.Max(mapFactor, 0.05f);
			float viewportScaling = Math.Max(mapFactor, 0.005f);

			Vector2 viewBase = screenpositionConverter(Input.ScreenSize() / 2, ses) - Input.ScreenSize() / 2 * viewportScaling;

			Vector2[] translations = new Vector2[]
			{
				viewBase + new Vector2(-10,-10),
				viewBase + new Vector2(-10,-10),
				viewBase + new Vector2(-10, Input.ScreenSize().Y * viewportScaling),
				viewBase + new Vector2(Input.ScreenSize().X * viewportScaling, -10)
			};
			Vector2[] scalings = new Vector2[]
			{
				new Vector2(20 + Input.ScreenSize().X * viewportScaling, 10),
				new Vector2(10, 20 + Input.ScreenSize().Y * viewportScaling),
				new Vector2(20 + Input.ScreenSize().X * viewportScaling, 10),
				new Vector2(10, 20 + Input.ScreenSize().Y * viewportScaling),
			};
			for(int i = 0; i < translations.Length; i++)
			{
				Matrix4 translationMatrix = Matrix4.method_1070(translations[i].ToVector3(0.0f));
				Matrix4 scalingMatrix = Matrix4.method_1074(scalings[i].ToVector3(0.0f));
				class_135.method_262(Map.t_square, Color.Black.WithAlpha(0.2f), translationMatrix * scalingMatrix);
			}

			// draw map hexes
			foreach (var kvp in stationaryMapHexes)
			{
				var hex = kvp.Value;
				var tex = hex.texture;
				Vector2 translation = positionConverter(hex.index) - common.textureDimensions(tex)/2* texScaling;
				Matrix4 translationMatrix = Matrix4.method_1070(translation.ToVector3(0.0f));
				Vector2 scaling = common.textureDimensions(tex) * texScaling;
				Matrix4 scalingMatrix = Matrix4.method_1074(scaling.ToVector3(0.0f));
				class_135.method_262(tex, hex.color, translationMatrix * scalingMatrix);
			}
		}


		public struct hex
		{
			public HexIndex index;
			public Texture texture;
			public Color color;
			public int priority;
			public hex(HexIndex _index, Texture _texture, Color _color, int _priority = 0)
			{
				this.index = _index;
				this.texture = _texture;
				this.color = _color;
				this.priority = _priority;
			}
			public HexIndex getIndex() => index;
			public void updateDict(Dictionary<HexIndex, Map.hex> dict)
			{
				if (!dict.ContainsKey(index))
				{
					dict.Add(index, this);
				}
				else if (dict[index].priority <= priority)
				{
					dict[index] = this;
				}
			}
		}

	}







}