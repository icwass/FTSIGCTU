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
	//
	//private static float[] simspeed_factor = new float[6]{ 0.1f, 0.25f, 1.0f, 4.0f, 10.0f, 100.0f };

	//private static Vector2 screenPosition = new Vector2(0f, 0f);
	//private static bool screenIsHoming = false;
	//private static float screenHomingParameter = 0f;


	private static Dictionary<PartType, Func<SolutionEditorScreen, Part, List<HexIndex>>> mapHexRules;

	private static Texture[] textures;

	private enum hexType : byte
	{
		blank,
		io,
		arm,
		track,
		glyph,
		chamber,
		COUNT,
	}

	//---------------------------------------------------//
	//public APIs and resources

	/// <summary>
	/// Adds a map-hex rule to FTSIGCTU's rulebook for the given PartType, if one doesn't already exist.
	/// </summary>
	/// <param name="partType">The PartType that the map-hex rule applies to.</param>
	/// <param name="rule">A function that, given a Part of the specified PartType, returns the list of hexes that should be drawn on the navigation map. See mapHexSimplePart for information on the function inputs.</param>
	/// <returns></returns>
	public static void addRule(PartType partType, Func<SolutionEditorScreen, Part, List<HexIndex>> rule)
	{
		if (mapHexRules.Keys.Contains(partType))
		{
			Logger.Log($"FTSIGCTU.Navigation.addRule: partType {partType.field_1529} already has a map-hex rule, ignoring.");
		}
		else
		{
			mapHexRules.Add(partType, rule);
		}
	}

	#region VanillaMapHexRules
	/// <summary>
	/// A simple rule that draws only the origin hex of a part
	/// </summary>
	/// <param name="ses">The current SolutionEditorScreen. Some parts, such as inputs, need to reference the SES to determine what hexes need to be drawn.</param>
	/// <param name="part">The part to be modified into its mirrored version.</param>
	public static List<HexIndex> mapHexSimplePart(SolutionEditorScreen ses, Part part)
	{
		return new List<HexIndex>() { common.getPartOrigin(part) };
	}

	/// <summary>
	/// A rule that returns an empty list of hexes, for parts that need not be drawn.
	/// </summary>
	public static List<HexIndex> mapHexEmpty(SolutionEditorScreen ses, Part part)
	{
		return new List<HexIndex>();
	}
	public static List<HexIndex> mapHexVanBerlo(SolutionEditorScreen ses, Part part)
	{
		var origin = common.getPartOrigin(part);
		return new List<HexIndex>() {
			origin + new HexIndex(1,0),
			origin + new HexIndex(-1,0),
			origin + new HexIndex(0,1),
			origin + new HexIndex(0,-1),
			origin + new HexIndex(1,-1),
			origin + new HexIndex(-1,1)
		};
	}
	public static List<HexIndex> mapHexGlyph(SolutionEditorScreen ses, Part part)
	{
		return common.getFootprintList(part);
	}
	public static List<HexIndex> mapHexTrack(SolutionEditorScreen ses, Part part)
	{
		return common.getTrackList(part);
	}
	public static List<HexIndex> mapHexConduit(SolutionEditorScreen ses, Part part)
	{
		return common.getConduitList(part);
	}
	public static List<HexIndex> mapHexIO(SolutionEditorScreen ses, Part part)
	{
		HexIndex origin = common.getPartOrigin(part);
		HexRotation rotation = common.getPartRotation(part);
		var solution = ses.method_502();
		Molecule molecule = part.method_1185(solution).method_1115(rotation).method_1117(origin);
		var moleculeDyn = new DynamicData(molecule);
		var atomDict = moleculeDyn.Get<Dictionary<HexIndex, Atom>>("field_2642");
		List<HexIndex> ret = new();
		foreach (var hex in atomDict.Keys)
		{
			ret.Add(hex);
		}

		return ret;
	}
	#endregion


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
			//GameLogic.field_2434.method_945(new NavigationMap(true), (Maybe<class_124>)struct_18.field_1431, (Maybe<class_124>)Transitions.field_4104);

			GameLogic.field_2434.method_946(new NavigationMap(SES_self));
		}
	}

	public static void SEPP_method_221(SolutionEditorProgramPanel sepp_self)
	{
		/*
		var seppDyn = new DynamicData(sepp_self);
		SolutionEditorScreen ses = seppDyn.Get<SolutionEditorScreen>("field_2007");

		Vector2 homePosition = 0.5f * class_115.field_1433 + new Vector2(140, 180);

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
		textures = new Texture[(int)hexType.COUNT];
		//string path = "ftsigctu/textures/solution_editor/navigation_map";
		textures[(int)hexType.blank] = class_238.field_1989.field_82.field_650;
		//textures[(int)hexType.blank] = class_235.method_615(path + "blank");

		mapHexRules = new();
		//add vanilla mirror rules

		//mechanisms
		addRule(common.MechanismArm1(), mapHexSimplePart);
		addRule(common.MechanismArm2(), mapHexSimplePart);
		addRule(common.MechanismArm3(), mapHexSimplePart);
		addRule(common.MechanismArm6(), mapHexSimplePart);
		addRule(common.MechanismPiston(), mapHexSimplePart);
		addRule(common.MechanismBerlo(), mapHexVanBerlo);
		addRule(common.MechanismTrack(), mapHexTrack);

		//simple glyphs
		addRule(common.GlyphEquilibrium(), mapHexGlyph);
		addRule(common.GlyphCalcification(), mapHexGlyph);
		addRule(common.GlyphDisposal(), mapHexGlyph);

		addRule(common.GlyphBonder(), mapHexGlyph);
		addRule(common.GlyphUnbonder(), mapHexGlyph);
		addRule(common.GlyphMultiBonder(), mapHexGlyph);
		addRule(common.GlyphDuplication(), mapHexGlyph);
		addRule(common.GlyphProjection(), mapHexGlyph);

		//advanced glyphs
		addRule(common.GlyphTriplexBonder(), mapHexGlyph);
		addRule(common.GlyphPurification(), mapHexGlyph);
		addRule(common.GlyphAnimismus(), mapHexGlyph);
		addRule(common.GlyphUnification(), mapHexGlyph);
		addRule(common.GlyphDispersion(), mapHexGlyph);

		//parts that may or may not be mirror-able
		addRule(common.IOConduit(), mapHexConduit);
		addRule(common.IOInput(), mapHexIO);
		addRule(common.IOOutputStandard(), mapHexIO);
		addRule(common.IOOutputInfinite(), mapHexIO);
	}


	public sealed class NavigationMap : IScreen
	{
		private readonly SolutionEditorScreen ses;
		private readonly List<HexIndex> stationaryMapHexes;
		public NavigationMap(SolutionEditorScreen _ses)
		{
			ses = _ses;
			stationaryMapHexes = new();

			Solution solution = ses.method_502();
			var partsList = solution.field_3919;

			// add critelli, if needed
			if (showCritelliOnMap)
			{
				stationaryMapHexes.Add(new HexIndex(0, 0));
			}
			// find the hexes for each part
			foreach (var part in partsList)
			{
				var partType = common.getPartType(part);
				if (mapHexRules.ContainsKey(partType))
				{
					stationaryMapHexes.AddRange(mapHexRules[partType](ses,part));
				}
			}
			// find chamber walls
			////////////////////////////////////////////////////////////////


			// find the appropriate bounding box - aspect ratio needs to be 10:6







		}


		public bool method_1037() => false;
		public void method_47(bool param_4183) => GameLogic.field_2434.field_2464 = true;
		public void method_48() { }
		public void method_50(float param_4184)
		{
			Texture letter6 = class_238.field_1989.field_85.field_571;
			Vector2 origin = new Vector2(15,15) + (class_115.field_1433 / 2) - (common.textureDimensions(letter6) / 2);

			class_135.method_272(letter6, origin.Rounded());

			Vector2 mapOrigin = origin + new Vector2(130, 130);
			Vector2 mapResolution = new Vector2(1000, 600);

			Texture hexagon = textures[(int)hexType.blank];


			Vector2 translation = mapOrigin + mapResolution / 2;
			Matrix4 translationMatrix = Matrix4.method_1070(translation.ToVector3(0.0f));

			Vector2 scaling = common.textureDimensions(hexagon) * 0.7f;
			Matrix4 scalingMatrix = Matrix4.method_1074(scaling.ToVector3(0.0f));

			class_135.method_262(hexagon, Color.Red, translationMatrix * scalingMatrix);


			bool returnToEditorKeypress = class_115.method_198(SDL.enum_160.SDLK_TAB) || class_115.method_198(SDL.enum_160.SDLK_SPACE) || class_115.method_198(SDL.enum_160.SDLK_ESCAPE);

			bool returnToEditor = returnToEditorKeypress;

			Sound ui_paper_back = class_238.field_1991.field_1875;

			if (returnToEditor)
			{
				GameLogic.field_2434.field_2464 = false;
				common.playSound(ui_paper_back);
				GameLogic.field_2434.method_949();
			}





			/*
			Texture window_background = class_238.field_1989.field_102.field_810;
			Texture window_buttonPanelCompletion = class_238.field_1989.field_102.field_811;
			Texture window_frameNoClose = class_238.field_1989.field_102.field_818;
			string puzzleName = (string)solutionEditorScreen.method_502().method_1934().field_2767;

			Vector2 dimensions = new Vector2(1115f, 735f);
			Vector2 base_position = (class_115.field_1433 / 2 - dimensions / 2).Rounded() + new Vector2(0.0f, -10f);
			if (class_115.field_1433.Y >= 1080.0f) base_position.Y += 110f;

			Vector2 position = base_position + new Vector2(78f, 88f);
			class_135.method_268(window_background, Color.White, position, Bounds2.WithSize(position, dimensions + new Vector2(-152f, -158f)));
			class_135.method_276(window_frameNoClose, Color.White, base_position, dimensions);
			class_135.method_272(window_buttonPanelCompletion, base_position + new Vector2(82f, 93f));
			class_140.method_317(puzzleName, base_position + new Vector2(95f, 636f), 900, false, true);

			string msg = verified ? "Your detector has successfully passed all unit tests." : "Your detector has successfully passed this unit test.";
			class_135.method_290((string)class_134.method_253(msg, string.Empty), base_position + new Vector2(559f, 588f), class_238.field_1990.field_2145, class_181.field_1718, (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, 0, new Color(), (class_256)null, int.MaxValue, false, true);

			class_135.method_290((string)class_134.method_253("Histograms and leaderboards are not available for this tournament puzzle.", string.Empty), base_position + new Vector2(545f, 365f), class_238.field_1990.field_2145, class_181.field_1718, (enum_0)1, 1f, 0.6f, float.MaxValue, float.MaxValue, 0, new Color(), (class_256)null, int.MaxValue, false, true);

			bool continueEditingMouseclick = class_140.method_313((string)class_134.method_253(verified ? "Return to Editor" : "Continue Editing", string.Empty), base_position + new Vector2(238f, 100f), 230, 46).method_824(true, true);
			
			bool recordGif = class_140.method_313((string)class_134.method_253("Record GIF", string.Empty), base_position + new Vector2(469f, 100f), 179, 46).method_824(!verified, true);
			bool returnToMenu = class_140.method_313((string)class_134.method_253("Return to Menu", string.Empty), base_position + new Vector2(649f, 100f), 230, 46).method_824(true, true);

			Sound ui_clickButton = class_238.field_1991.field_1821;
			Sound ui_fade = class_238.field_1991.field_1871;
			Sound ui_modalClose = class_238.field_1991.field_1873;

			if (continueEditingMouseclick || continueEditingKeypress)
			{
				solutionEditorScreen.method_2098(true);
				GameLogic.field_2434.field_2464 = false;
				GameLogic.field_2434.method_949();
				common.playSound(ui_modalClose);
				if (verified)
				{
					new DynamicData(solutionEditorScreen).Set("field_4023", enum_128.Paused);
					new DynamicData(solutionEditorScreen).Get<Maybe<Sim>>("field_4022").method_1087().method_1827();
				}
			}
			if (recordGif)
			{
				GameLogic.field_2434.method_946(new class_250(solutionEditorScreen.method_502()));
				class_238.field_1991.field_1821.method_28(1f);
				common.playSound(ui_clickButton);
			}
			if (returnToMenu)
			{
				GameLogic.field_2434.field_2464 = false;
				GameLogic.field_2434.method_949();
				GameLogic.field_2434.method_949(); // is this supposed to be here, or is it a typo?
				class_238.field_1991.field_1871.method_28(1f);
				common.playSound(ui_fade);
				common.playSound(ui_clickButton);
			}
			*/
		}



	}







}