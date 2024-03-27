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
			GameLogic.field_2434.method_946(Input.IsShiftHeld() ? new InstructionsMap(SES_self) : new PartsMap(SES_self));
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



		InstructionsMap.addInstructionColor(class_169.field_1652, InstructionsMap.c_override); // override
		InstructionsMap.addInstructionColor(class_169.field_1653, InstructionsMap.c_blank); // blank 1
		InstructionsMap.addInstructionColor(class_169.field_1654, InstructionsMap.c_comment); // blank 2 / comment
		InstructionsMap.addInstructionColor(class_169.field_1655, InstructionsMap.c_advance); // advance
		InstructionsMap.addInstructionColor(class_169.field_1656, InstructionsMap.c_retreat); // retreat
		InstructionsMap.addInstructionColor(class_169.field_1657, InstructionsMap.c_rotateR); // rotate CW
		InstructionsMap.addInstructionColor(class_169.field_1658, InstructionsMap.c_rotateL); // rotate CCW
		InstructionsMap.addInstructionColor(class_169.field_1659, InstructionsMap.c_extend); // extend
		InstructionsMap.addInstructionColor(class_169.field_1660, InstructionsMap.c_retract); // retract
		InstructionsMap.addInstructionColor(class_169.field_1661, InstructionsMap.c_pivotR); // pivot CW
		InstructionsMap.addInstructionColor(class_169.field_1662, InstructionsMap.c_pivotL); // pivot CCW
		InstructionsMap.addInstructionColor(class_169.field_1663, InstructionsMap.c_grab); // grab
		InstructionsMap.addInstructionColor(class_169.field_1664, InstructionsMap.c_drop); // drop
		InstructionsMap.addInstructionColor(class_169.field_1665, InstructionsMap.c_reset); // reset
		InstructionsMap.addInstructionColor(class_169.field_1666, InstructionsMap.c_repeat); // repeat
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
		internal static Color Color_RGBA(int r, int g, int b, float alpha = 1f) => Color.FromHex(r * 256 * 256 + g * 256 + b).WithAlpha(alpha);
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


	public class InstructionsMap : MapBase
	{
		// public API
		public static readonly Color c_transparent	= Color_RGBA(255, 255, 255, 0f);
		public static readonly Color c_override	= Color_RGBA(128, 128, 128, 1f);
		public static readonly Color c_blank	= Color_RGBA(192, 192, 192, 1f);
		public static readonly Color c_comment	= Color_RGBA(  0,   0, 192, 1f);

		public static readonly Color c_grab		= Color_RGBA(192, 128,   0, 1f);
		public static readonly Color c_drop		= Color_RGBA(128,  64,   0, 1f);

		public static readonly Color c_rotateL	= Color_RGBA(  0, 128, 192, 1f);
		public static readonly Color c_rotateR	= Color_RGBA(  0,  64, 128, 1f);
		public static readonly Color c_pivotL	= Color_RGBA(  0, 192, 128, 1f);
		public static readonly Color c_pivotR	= Color_RGBA(  0, 128,  64, 1f);

		public static readonly Color c_advance	= Color_RGBA(192, 192,   0, 1f);
		public static readonly Color c_retreat	= Color_RGBA(128, 128,   0, 1f);

		public static readonly Color c_extend	= Color_RGBA(192,   0, 192, 1f);
		public static readonly Color c_retract	= Color_RGBA(128,   0, 128, 1f);
		public static readonly Color c_reset	= Color_RGBA(192,   0,   0, 1f);
		public static readonly Color c_repeat	= Color_RGBA(128,   0,   0, 1f);

		public static void addInstructionColor(InstructionType instructionType, Color color, bool overwrite = false)
		{
			if (InstructionColors.Keys.Contains(instructionType))
			{
				if (!overwrite)
				{
					Logger.Log($"FTSIGCTU.Navigation.InstructionsMap.addInstructionColor: instructionType {instructionType.field_2543} already has a color, ignoring new color.");
					return;
				}
				Logger.Log($"FTSIGCTU.Navigation.InstructionsMap.addInstructionColor: instructionType {instructionType.field_2543} already has a color, overwriting with new color.");
			}
			InstructionColors.Add(instructionType, color);
		}

		// private data and functions
		readonly SolutionEditorProgramPanel sepp;
		readonly DynamicData sepp_dyn;
		Vector2 viewportDimensions => new Vector2(Input.ScreenSize().X - 382f, SolutionEditorProgramPanel.field_3982.Y * 6);
		Vector2 viewportPosition => sepp_dyn.Get<Vector2>("field_3988");
		static Vector2 instructionTileDimensions => SolutionEditorProgramPanel.field_3982; //= new Vector2(41f, 38f);

		float mapScalingFactor, texScalingFactor, viewScalingFactor;

		Func<int, int, Vector2> positionConverter;
		Action<Vector2> repositionViewport;


		List<mapTile> mapTiles = new();
		List<mapTile> mapExtents = new();

		static Dictionary<InstructionType, Color> InstructionColors = new();

		// constructor
		public InstructionsMap(SolutionEditorScreen ses) : base(ses)
		{
			this.sepp = ses.field_4003;
			this.sepp_dyn = new DynamicData(sepp);

			// add the origin and the bottom-right corner of the current view
			mapTiles.Add(new mapTile(0, 0, c_transparent));

			Vector2 bottomRight = -viewportPosition + viewportDimensions;

			int minX = 0;
			int minY = 0;
			int maxX = (int)(Math.Ceiling(bottomRight.X) / instructionTileDimensions.X);
			int maxY = (int)(Math.Ceiling(bottomRight.Y) / instructionTileDimensions.Y);

			mapTiles.Add(new mapTile(maxX, maxY, c_transparent));

			// find all the instruction tiles
			var solution = ses.method_502();
			var programmableParts = solution.method_1941();

			maxY = Math.Max(maxY, programmableParts.Count);

			for (int j = 0; j < programmableParts.Count; j++)
			{
				var part = programmableParts[j];
				var editableProgram = part.field_2697;
				
				class_188 class188 = editableProgram.method_910(part, solution.method_1942(part));
				var programDictionary = class188.field_1745;

				foreach (var kvp in programDictionary)
				{
					int i = kvp.Key;
					class_14 val = kvp.Value;
					var instructionType = val.field_56;
					var extentsList = val.field_57;
					var instructionLengthInTray = extentsList.Length;

					InstructionType[] list = new InstructionType[1] { instructionType };

					if (instructionLengthInTray > 1)
					{
						list = val.field_57;

						for (int k = 0; k < list.Length; k++)
						{
							if (InstructionColors.ContainsKey(instructionType)) mapTiles.Add(new mapTile(i + k, j, InstructionColors[instructionType]));
							if (InstructionColors.ContainsKey(list[k])) mapExtents.Add(new mapTile(i + k, j, InstructionColors[list[k]]));
						}
					}
					else
					{
						if (InstructionColors.ContainsKey(instructionType)) mapTiles.Add(new mapTile(i, j, InstructionColors[instructionType]));
					}

					maxX = Math.Max(maxX, i + list.Length - 1);
				}
			}

			///////////////////////////////////////////////////////////////
			// not drawing anything animated, currently, so this is blank

			///////////////////////////////////////
			// determine how big of a map we need
			double widthFactor = mapResolution.X / ((maxX - minX) * instructionTileDimensions.X);
			double heightFactor = mapResolution.Y / ((maxY - minY) * instructionTileDimensions.Y);

			mapScalingFactor = (float)Math.Min(widthFactor, heightFactor);
			texScalingFactor = Math.Max(mapScalingFactor, 0.05f);
			viewScalingFactor = Math.Max(mapScalingFactor, 0.005f);

			float centerX = 0.5f * (maxX + minX);
			float centerY = 0.5f * (maxY + minY);

			Vector2 PositionConverter(int i, int j)
			{
				//converts tile coordinates to mapSpace coordinates
				float x = (i - centerX) * instructionTileDimensions.X;
				float y = (j - centerY) * instructionTileDimensions.Y;
				return 0.5f * mapResolution + new Vector2(x, -y) * mapScalingFactor;
			}

			this.positionConverter = PositionConverter;

			void RepositionViewport(Vector2 mapMousePos)
			{
				//
				Vector2 u = -PositionConverter(0, 0);
				Vector2 v = 0.5f * mapScalingFactor * new Vector2(-viewportDimensions.X, viewportDimensions.Y);
				Vector2 ret = u + mapMousePos + v;
				ret = -ret / mapScalingFactor;

				float nearestColumnPos = 0 * instructionTileDimensions.X;
				float nearestRowPos = 0 * instructionTileDimensions.Y;
				float farthestColumnPos = (maxX+1) * instructionTileDimensions.X - viewportDimensions.X;
				float farthestRowPos = maxY * instructionTileDimensions.Y - viewportDimensions.Y;
				Vector2 newPosition = new Vector2(Math.Max(-farthestColumnPos, Math.Min(nearestColumnPos, ret.X)), Math.Min(farthestRowPos, Math.Max(ret.Y, nearestRowPos)));

				sepp_dyn.Set("field_3988", newPosition);
			}

			this.repositionViewport = RepositionViewport;
		}

		internal override void drawFunction(float deltaTime, Vector2 mapMousePos)
		{
			// draw instruction squares
			foreach (var tile in mapTiles)
			{
				var scaling = instructionTileDimensions * texScalingFactor;
				var position = positionConverter(tile.x, tile.y+1);// - 0.5f * scaling;
				drawRectangle(position.X, position.Y, scaling.X, scaling.Y, tile.color);
			}

			// draw extents (smaller squares from resets, repeats, etc)
			foreach (var tile in mapExtents)
			{
				var scaling = instructionTileDimensions * texScalingFactor;
				var position = positionConverter(tile.x, tile.y+1);// - 0.5f * scaling;
				drawRectangle(position.X, position.Y, scaling.X, scaling.Y * 0.5f, tile.color);
			}

			// update and draw viewport
			if (Input.IsLeftClickHeld()) repositionViewport(mapMousePos);

			Vector2 viewBase = positionConverter(0,0) / viewScalingFactor - viewportPosition + new Vector2(0f, -viewportDimensions.Y);
			drawViewport(viewBase * viewScalingFactor, viewportDimensions * viewScalingFactor);
		}

		public struct mapTile
		{
			public int x, y;
			public Color color;
			public mapTile(int x, int y, Color color)
			{
				this.x = x;
				this.y = y;
				this.color = color;
			}
		}
	}

	public class PartsMap : MapBase
	{
		// public API
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
		static Texture hexagon;
		readonly Dictionary<HexIndex, mapHex> stationaryMapHexes;
		static Dictionary<PartType, Func<SolutionEditorScreen, Part, List<mapHex>>> partHexRules = new();
		float mapScalingFactor, texScalingFactor, viewScalingFactor;
		Vector2 boardhexDimensions => class_187.field_1742.field_1744;//= new Vector2(80, 70);

		Func<HexIndex, Vector2> positionConverter;
		Func<Vector2, SolutionEditorScreen, Vector2> screenpositionConverter;
		Action<Vector2> repositionViewport;

		// constructor
		public PartsMap(SolutionEditorScreen ses) : base(ses)
		{
			// load hexagon shape as needed
			hexagon ??= class_235.method_615("ftsigctu/textures/solution_editor/navigation/hexagon");

			///////////////////////////////////////////////////
			// determine the stationary hexes we need to draw
			this.stationaryMapHexes = new();
			Solution solution = ses.method_502();

			// define helper
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

			double centerX = (maxX + minX) / 2;
			double centerY = (maxY + minY) / 2;

			Vector2 convertScreenPosition(Vector2 vec, SolutionEditorScreen ses)
			{
				Vector2 vec2 = vec - ses.field_4009;
				double x = (vec2.X / boardhexDimensions.X - centerX) * boardhexDimensions.X;
				double y = (vec2.Y / boardhexDimensions.Y - centerY) * boardhexDimensions.Y;
				return mapResolution / 2 + new Vector2((float)x, (float)y) * mapScalingFactor;
			}

			void RepositionViewport(Vector2 vec)
			{
				Vector2 vec2 = (vec - mapResolution / 2) / mapScalingFactor;
				double x = centerX + vec2.X / boardhexDimensions.X;
				double y = centerY + vec2.Y / boardhexDimensions.Y;
				Vector2 newPosition = Input.ScreenSize() / 2 - new Vector2((float)x * boardhexDimensions.X, (float)y * boardhexDimensions.Y);

				ses.field_4009 = newPosition;
			}

			this.screenpositionConverter = convertScreenPosition;
			this.repositionViewport = RepositionViewport;

			Vector2 PositionConverter(HexIndex hex)
			{
				double x = (hex.Q + hex.R / 2f - centerX) * boardhexDimensions.X;
				double y = (hex.R - centerY) * boardhexDimensions.Y;
				return mapResolution / 2 + new Vector2((float)x, (float)y) * mapScalingFactor;
			}

			this.positionConverter = PositionConverter;
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
			if (Input.IsLeftClickHeld()) repositionViewport(mapMousePos);

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