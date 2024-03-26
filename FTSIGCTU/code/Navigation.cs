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
	public static bool showCritelliOnMap = true;
	static bool PressedMapKey() => MainClass.MySettings.Instance.displayEditingSettings.KeyShowMap.Pressed();

	//private static Vector2 screenPosition = new Vector2(0f, 0f);
	//private static bool screenIsHoming = false;
	//private static float screenHomingParameter = 0f;

	//---------------------------------------------------//
	//public APIs and resources

	//---------------------------------------------------//
	//internal helper methods

	//---------------------------------------------------//
	//internal main methods

	//---------------------------------------------------//

	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		if (PressedMapKey())
		{
			Sound ui_paper = class_238.field_1991.field_1874;
			common.playSound(ui_paper);
			GameLogic.field_2434.method_946(Input.IsShiftHeld() ? new PartsMap(SES_self) : new PartsMap(SES_self));
		}
	}

	//public static void SEPP_method_221(SolutionEditorProgramPanel sepp_self)
	//{
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
	//}


	public static void LoadPuzzleContent()
	{
		PartsMap.addPartHexRule(common.MechanismArm1()		, PartsMap.armHexRule);
		PartsMap.addPartHexRule(common.MechanismArm2()		, PartsMap.armHexRule);
		PartsMap.addPartHexRule(common.MechanismArm3()		, PartsMap.armHexRule);
		PartsMap.addPartHexRule(common.MechanismArm6()		, PartsMap.armHexRule);
		PartsMap.addPartHexRule(common.MechanismPiston()	, PartsMap.armHexRule);
		PartsMap.addPartHexRule(common.MechanismBerlo()     , PartsMap.armHexRule);
		PartsMap.addPartHexRule(common.MechanismTrack()		, PartsMap.partHexRulemaker((ses, part) => common.getTrackList(part), PartsMap.c_track, PartsMap.p_track));
		
		PartsMap.addPartHexRule(common.GlyphEquilibrium()	, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphCalcification()	, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphDisposal()		, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphBonder()		, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphUnbonder()		, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphMultiBonder()	, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphDuplication()	, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphProjection()	, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphTriplexBonder()	, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphPurification()	, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphAnimismus()		, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphUnification()	, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphDispersion()	, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.MechanismTrack()		, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.GlyphEquilibrium()	, PartsMap.glyphRule);
		PartsMap.addPartHexRule(common.MechanismTrack()		, PartsMap.glyphRule);

		List<HexIndex> ioHexRule(SolutionEditorScreen ses, Part part)
		{
			List<HexIndex> ret = new();
			var solution = ses.method_502();
			Molecule molecule = part.method_1185(solution).method_1115(common.getPartRotation(part)).method_1117(common.getPartOrigin(part));
			var atomDict = new DynamicData(molecule).Get<Dictionary<HexIndex, Atom>>("field_2642");
			return atomDict.Keys.ToList();
		}

		PartsMap.addPartHexRule(common.IOConduit()			, PartsMap.partHexRulemaker((ses, part) => common.getConduitList(part), PartsMap.c_conduit, PartsMap.p_glyph));
		PartsMap.addPartHexRule(common.IOInput()			, PartsMap.partHexRulemaker(ioHexRule, PartsMap.c_input, PartsMap.p_glyph));
		PartsMap.addPartHexRule(common.IOOutputStandard()	, PartsMap.partHexRulemaker(ioHexRule, PartsMap.c_output, PartsMap.p_glyph));
		PartsMap.addPartHexRule(common.IOOutputInfinite()	, PartsMap.partHexRulemaker(ioHexRule, PartsMap.c_output, PartsMap.p_glyph));
	}





















	public abstract class MapBase : IScreen
	{
		public MapBase(SolutionEditorScreen ses)
		{
			this.ses = ses;
			this.whitePixel = class_238.field_1989.field_71;
		}
		// internal data
		internal readonly SolutionEditorScreen ses;
		internal readonly Texture whitePixel;
		internal Vector2 mapResolution = new Vector2(950, 550);
		Vector2 mapOrigin => 0.5f * (Input.ScreenSize() - common.textureDimensions(letter6)) + mapOffset + new Vector2(15, 15);

		// internal functions for derived classes
		internal abstract void drawFunction(float deltaTime, Vector2 mouseMapPos);


		internal void drawTexture(Texture texture, Vector2 position, Vector2 scaling) => drawTexture(texture, position, scaling, Color.White);
		internal void drawTexture(Texture texture, Vector2 position, Vector2 scaling, Color color)
		{
			Matrix4 translationMatrix = Matrix4.method_1070((position + mapOrigin).ToVector3(0f));
			Matrix4 scalingMatrix = Matrix4.method_1074(new Vector2(scaling.X * texture.field_2056.X, scaling.Y * texture.field_2056.Y).ToVector3(0f));
			class_135.method_262(texture, color, translationMatrix * scalingMatrix);
		}
		internal void drawRectangle(float x, float y, float w, float h, Color color)
		{
			drawTexture(whitePixel, new Vector2(x, y), new Vector2(w, h), color);
		}
		internal void drawViewport(Vector2 position, Vector2 dimensions)
		{
			// assumes position and dimensions are scaled already
			void rect(float x, float y, float w, float h) => drawRectangle(x + position.X, y + position.Y, w, h, Color.Black.WithAlpha(0.2f));
			float W = dimensions.X;
			float H = dimensions.Y;
			float b = 10f; // border
			rect(-b, -b, b, b + H);
			rect(-b, H, b + W, b);
			rect(0, -b, b + W, b);
			rect(W, 0, b, b + H);
		}

		// private data
		Texture letter6 => class_238.field_1989.field_85.field_571;
		Vector2 mapOffset = new Vector2(155, 155);

		// IScreen interface implementation
		public bool method_1037() => false;
		public void method_47(bool _) => GameLogic.field_2434.field_2464 = true;
		public void method_48() { }
		public void method_50(float deltaTime)
		{
			bool returnToEditorKeypress =
				Input.IsSdlKeyPressed(SDL.enum_160.SDLK_TAB)
				|| Input.IsSdlKeyPressed(SDL.enum_160.SDLK_SPACE)
				|| Input.IsSdlKeyPressed(SDL.enum_160.SDLK_ESCAPE)
				|| PressedMapKey()
			;
			bool returnToEditor = returnToEditorKeypress;
			Sound ui_paper_back = class_238.field_1991.field_1875;
			if (returnToEditor)
			{
				GameLogic.field_2434.field_2464 = false;
				common.playSound(ui_paper_back);
				GameLogic.field_2434.method_949();
			}

			// draw
			class_135.method_272(letter6, mapOrigin - mapOffset);
			drawFunction(deltaTime, Input.MousePos() - mapOrigin);
		}
	}













	public class PartsMap : MapBase
	{
		// public API
		static Color Color_RGBA(int r, int g, int b, float alpha = 1f) => Color.FromHex(r * 256 * 256 + g * 256 + b).WithAlpha(alpha);
		static Texture hexagon;

		public static readonly Color c_chamber	= Color_RGBA(128,  64,   0, 0.75f);
		public static readonly Color c_equil	= Color_RGBA(128, 128, 128, 0.75f);
		public static readonly Color c_glyph	= Color_RGBA( 64,  64,  64, 0.75f);
		public static readonly Color c_input	= Color_RGBA(  0, 128, 192, 0.75f);
		public static readonly Color c_output	= Color_RGBA(  0, 128,  64, 0.75f);
		public static readonly Color c_conduit	= Color_RGBA(128,   0, 128, 0.75f);
		public static readonly Color c_track	= Color_RGBA(  0,   0,   0, 0.75f);
		public static readonly Color c_arm		= Color_RGBA(192, 192, 192, 0.75f);
		public static readonly Color c_critelli	= Color_RGBA(128,   0,   0, 0.75f);

		public static readonly int p_board = -1000000;
		public static readonly int p_equil = -1000;
		public static readonly int p_glyph = 0;
		public static readonly int p_track = 1000;
		public static readonly int p_arm = 1000000;

		public static void addPartHexRule(PartType partType, Func<SolutionEditorScreen, Part, List<mapHex>> rule, bool overwrite = false)
		{
			if (partHexRules.Keys.Contains(partType))
			{
				if (!overwrite)
				{
					Logger.Log($"FTSIGCTU.Navigation.PartsMap.addPartHexRule: partType {partType.field_1529} already has a rule, ignoring new rule.");
					return;
				}
				Logger.Log($"FTSIGCTU.Navigation.PartsMap.addPartHexRule: partType {partType.field_1529} already has a rule, overwriting with new rule.");
			}
			partHexRules.Add(partType, rule);
		}

		public static Func<SolutionEditorScreen, Part, List<mapHex>> partHexRulemaker(Func<SolutionEditorScreen, Part, List<HexIndex>> hexFinder, Color color, int priority)
		{
			return (ses, part) =>
			{
				List<mapHex> ret = new();
				foreach (var hex in hexFinder(ses, part))
				{
					ret.Add(new mapHex(hex, color, priority));
				}
				return ret;
			};
		}

		public static Func<SolutionEditorScreen, Part, List<mapHex>> armHexRule => partHexRulemaker((ses, part) => new List<HexIndex> { part.method_1161() }, c_arm, p_arm);
		public static Func<SolutionEditorScreen, Part, List<mapHex>> glyphRule = partHexRulemaker((ses, part) => common.getFootprintList(part), c_glyph, p_glyph);

		// private data and functions
		readonly Dictionary<HexIndex, mapHex> stationaryMapHexes;
		static Dictionary<PartType, Func<SolutionEditorScreen, Part, List<mapHex>>> partHexRules = new();
		float mapScalingFactor, texScalingFactor, viewScalingFactor;
		Vector2 boardhexDimensions => class_187.field_1742.field_1744;//= new Vector2(80, 70);

		Func<HexIndex, Vector2> positionConverter;
		Func<Vector2, SolutionEditorScreen, Vector2> screenpositionConverter;
		Func<Vector2, Vector2> repositionScreen;

		// constructor
		public PartsMap(SolutionEditorScreen ses) : base(ses)
		{
			// load hexagon shape as needed
			hexagon ??= class_235.method_615("ftsigctu/textures/solution_editor/navigation/hexagon");

			///////////////////////////////////////////////////
			// determine the stationary hexes we need to draw
			this.stationaryMapHexes = new();
			Solution solution = ses.method_502();

			void addStationaryHex(mapHex hex)
			{
				if (!stationaryMapHexes.ContainsKey(hex.index) || stationaryMapHexes[hex.index].priority < hex.priority)
				{
					stationaryMapHexes[hex.index] = hex;
				}
			}

			// get critelli
			addStationaryHex(new mapHex(new HexIndex(0, 0), c_critelli, showCritelliOnMap ? int.MaxValue : int.MinValue));

			// get chamber hexes
			Puzzle puzzle = solution.method_1934();
			var maybeChambers = puzzle.field_2779;

			if (maybeChambers.method_1085())
			{
				foreach (var chamber in maybeChambers.method_1087().field_2071)
				{
					foreach (var hex in chamber.field_1747.field_1729)
					{
						addStationaryHex(new mapHex(hex + chamber.field_1746, c_chamber, p_board));
					}
				}
			}

			// get hexes for parts
			var partsList = solution.field_3919;
			foreach (var part in partsList)
			{
				var partType = common.getPartType(part);

				if (partHexRules.Keys.Contains(partType))
				{
					foreach (var hex in partHexRules[partType](ses, part))
					{
						addStationaryHex(hex);
					}
				}
				else
				{
					// undefined part type - ignore for now
				}
			}

			///////////////////////////////////////////////////////////////
			// not drawing anything animated, currently, so this is blank

			///////////////////////////////////////
			// determine how big of a map we need
			Vector2 convertvector(Vector2 vec, SolutionEditorScreen ses)
			{
				Vector2 vec2 = vec - ses.field_4009;
				float x = vec2.X / boardhexDimensions.X;
				float y = vec2.Y / boardhexDimensions.Y;
				return new Vector2(x, y);
			}
			double minX = convertvector(new Vector2(0, 0), ses).X;
			double minY = convertvector(new Vector2(0, 0), ses).Y;
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

			double widthFactor = mapResolution.X / ((maxX - minX) * boardhexDimensions.X);
			double heightFactor = mapResolution.Y / ((maxY - minY) * boardhexDimensions.Y);

			mapScalingFactor = (float)Math.Min(widthFactor, heightFactor);
			texScalingFactor = Math.Max(mapScalingFactor, 0.05f);
			viewScalingFactor = Math.Max(mapScalingFactor, 0.005f);

			Vector2 convertScreenPosition(Vector2 vec, SolutionEditorScreen ses)
			{
				Vector2 vec2 = vec - ses.field_4009;
				double x = (vec2.X / boardhexDimensions.X - (maxX + minX) / 2) * boardhexDimensions.X;
				double y = (vec2.Y / boardhexDimensions.Y - (maxY + minY) / 2) * boardhexDimensions.Y;
				return mapResolution / 2 + new Vector2((float)x, (float)y) * mapScalingFactor;
			}

			Vector2 repositionScreenPosition(Vector2 vec)
			{
				Vector2 vec2 = (vec - mapResolution / 2) / mapScalingFactor;
				double x = (maxX + minX) / 2 + vec2.X / boardhexDimensions.X;
				double y = (maxY + minY) / 2 + vec2.Y / boardhexDimensions.Y;
				return Input.ScreenSize() / 2 - new Vector2((float)x * boardhexDimensions.X, (float)y * boardhexDimensions.Y);
			}

			this.screenpositionConverter = convertScreenPosition;
			this.repositionScreen = repositionScreenPosition;

			Vector2 convertPosition(HexIndex hex)
			{
				double x = (hex.Q + hex.R / 2f - (maxX + minX) / 2) * boardhexDimensions.X;
				double y = (hex.R - (maxY + minY) / 2) * boardhexDimensions.Y;
				return mapResolution / 2 + new Vector2((float)x, (float)y) * mapScalingFactor;
			}

			this.positionConverter = convertPosition;
		}

		internal override void drawFunction(float deltaTime, Vector2 mapMousePos)
		{
			// draw map hexes
			foreach (var kvp in stationaryMapHexes)
			{
				var hex = kvp.Value;
				var position = positionConverter(hex.index) - common.textureDimensions(hexagon) / 2 * texScalingFactor;
				var scaling = new Vector2(1, 1) * texScalingFactor;
				drawTexture(hexagon, position, scaling, hex.color);
			}

			// update and draw viewport
			if (Input.IsLeftClickHeld()) ses.field_4009 = repositionScreen(mapMousePos);

			Vector2 viewBase = screenpositionConverter(Input.ScreenSize() / 2, ses) - Input.ScreenSize() / 2 * viewScalingFactor;
			drawViewport(viewBase, Input.ScreenSize() * viewScalingFactor);
		}

		public struct mapHex
		{
			public HexIndex index;
			public Color color;
			public int priority;
			public mapHex(HexIndex _index, Color _color, int _priority = 0)
			{
				this.index = _index;
				this.color = _color;
				this.priority = _priority;
			}
		}
	}
}