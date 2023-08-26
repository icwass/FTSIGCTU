using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Settings;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;

//using PartType = class_139;
//using Permissions = enum_149;
//using BondType = enum_126;
//using BondSite = class_222;
//using AtomTypes = class_175;
//using PartTypes = class_191;
//using Texture = class_256;

public class MainClass : QuintessentialMod
{


	public static bool ignorePartPlacementRestrictions = false;
	
	public override Type SettingsType => typeof(MySettings);
	public class MySettings
	{
		//common
		[SettingsLabel("Use thicker lines when highlighting hexes.")]
		public bool drawThickHexes = false;

		//TrackEditor
		[SettingsLabel("When reversing a track, also reverse arms on the track.")]
		public bool alsoReverseArms = true;
		[SettingsLabel("Allow the creation of disjoint (i.e. 'quantum') tracks.")]
		public bool allowQuantumTracking = false;
		//ConduitEditor
		[SettingsLabel("Allow conduits to be created, destroyed, and swapped around.")]
		public bool allowConduitEditor = false;
		//InstructionEditor
		[SettingsLabel("Show blank instruction sources in the programming tray.")]
		public bool drawBlanksOnProgrammingTray = false;
		[SettingsLabel("Allow multiple Period Override instructions.")]
		public bool allowMultipleOverrides = false;

		//need to put this somewhere
		[SettingsLabel("Disable overlap detection and other part-placement restrictions.")]
		public bool ignorePartPlacementRestrictions = false;
		[SettingsLabel("Allow duplicate Disposals, Berlos, inputs and outputs.")]
		public bool allowDuplicateParts = false;
		[SettingsLabel("Run the simulation even if the number of outputs is wrong.")]
		public bool allowWrongNumberOfOutputs = false;
		[SettingsLabel("Use 'Gold' instead of 'Cost' in the metric display.")]
		public bool writeGoldNotCost = false;
		[SettingsLabel("Change the speedtray for ZoomTool compatibility.")]
		public bool speedtrayZoomtoolWorkaround = false;
		[SettingsLabel("Show the origin on the navigation map.")]
		public bool showCritelliOnMap = false;

	}
	public override void ApplySettings()
	{
		base.ApplySettings();

		var SET = (MySettings)Settings;
		common.ApplySettings(SET.drawThickHexes);
		TrackEditor.ApplySettings(SET.alsoReverseArms, SET.allowQuantumTracking);
		ConduitEditor.ApplySettings(SET.allowConduitEditor);
		InstructionEditor.ApplySettings(SET.drawBlanksOnProgrammingTray, SET.allowMultipleOverrides);
		Miscellaneous.ApplySettings(SET.allowDuplicateParts, SET.speedtrayZoomtoolWorkaround, SET.allowWrongNumberOfOutputs);
		MetricDisplay.ApplySettings(SET.writeGoldNotCost);
		Navigation.ApplySettings(SET.showCritelliOnMap);

		ignorePartPlacementRestrictions = SET.ignorePartPlacementRestrictions;
	}
	public override void Load()
	{
		Settings = new MySettings();
	}
	public override void LoadPuzzleContent()
	{
		TrackEditor.LoadPuzzleContent();
		ConduitEditor.LoadPuzzleContent();
		InstructionEditor.LoadPuzzleContent();
		MirrorTool.LoadPuzzleContent();
		Miscellaneous.LoadPuzzleContent();
		AreaDisplay.LoadPuzzleContent();
		DebugParts.LoadPuzzleContent();
		MetricDisplay.LoadPuzzleContent();
		Navigation.LoadPuzzleContent();
	}

	public override void Unload()
	{
		InstructionEditor.Unload();
	}

	//------------------------- END HOOKING -------------------------//
	public override void PostLoad()
	{
		On.SolutionEditorScreen.method_50 += SES_Method_50;
		On.class_153.method_221 += c153_Method_221;

		On.Solution.method_1948 += Solution_method_1948;
		On.SolutionEditorProgramPanel.method_221 += SolutionEditorProgramPanel_Method_221;

		On.SolutionEditorScreen.method_511 += SES_Method_511;
	}

	public static float SES_Method_511(On.SolutionEditorScreen.orig_method_511 orig, SolutionEditorScreen SES_self)
	{
		return orig(SES_self) * Miscellaneous.SimSpeedFactor();
	}

	public void SolutionEditorProgramPanel_Method_221(On.SolutionEditorProgramPanel.orig_method_221 orig, SolutionEditorProgramPanel SEPP_self, float param_5658)
	{
		AreaDisplay.SEPP_method_221(SEPP_self);
		orig(SEPP_self, param_5658);
		MetricDisplay.SEPP_method_221(SEPP_self);
		Miscellaneous.SEPP_method_221(SEPP_self);
	}
	public void SES_Method_50(On.SolutionEditorScreen.orig_method_50 orig, SolutionEditorScreen SES_self, float param_5703)
	{
		ConduitEditor.SolutionEditorScreen_method_50(SES_self);
		MirrorTool.SolutionEditorScreen_method_50(SES_self);
		InstructionEditor.SolutionEditorScreen_method_50(SES_self);
		Miscellaneous.SolutionEditorScreen_method_50(SES_self);
		DebugParts.SolutionEditorScreen_method_50(SES_self);
		Navigation.SolutionEditorScreen_method_50(SES_self);
		orig(SES_self, param_5703);
		TrackEditor.SolutionEditorScreen_method_50(SES_self);
	}
	public static void c153_Method_221(On.class_153.orig_method_221 orig, class_153 c153_self, float param_3616)
	{
		orig(c153_self, param_3616);
		TrackEditor.class153_method_221(c153_self);
		AreaDisplay.c153_method_221(c153_self);
	}

	public bool Solution_method_1948(On.Solution.orig_method_1948 orig,
		Solution solution_self,
		Part part,
		HexIndex hex1,
		HexIndex hex2,
		HexRotation rot,
		out string errorMessage)
	{
		if (ignorePartPlacementRestrictions)
		{
			errorMessage = null;
			return true;
		}
		else
		{
			bool ret = orig(solution_self, part, hex1, hex2, rot, out errorMessage);
			return ret;
		}
	}
}