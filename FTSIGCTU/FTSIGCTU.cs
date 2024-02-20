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
	public override Type SettingsType => typeof(MySettings);

	public static QuintessentialMod MainClassAsMod;


	public class MySettings
	{
		public static MySettings Instance => MainClassAsMod.Settings as MySettings;

		/*
		[SettingsLabel("Enable Campaign Switcher")]
		public bool EnableCustomCampaigns = true;
		[SettingsLabel("Campaign Switcher Options:")]
		public CampaignSwitcherSettings SwitcherSettings = new();
		public class CampaignSwitcherSettings : SettingsGroup
		{
			public override bool Enabled => Instance.EnableCustomCampaigns;

			[SettingsLabel("Switch Campaign Left")]
			public Keybinding SwitchCampaignLeft = new() { Key = "K", Control = true };

			[SettingsLabel("Switch Campaign Right")]
			public Keybinding SwitchCampaignRight = new() { Key = "L", Control = true };
		}
		*/

		[SettingsLabel("Display Settings:")]
		public bool enableDisplaySettings = false;
		[SettingsLabel("")]
		public DisplaySettings displayEditingSettings = new();
		public class DisplaySettings : SettingsGroup
		{
			public override bool Enabled => Instance.enableDisplaySettings;

			[SettingsLabel("Use thicker lines when highlighting hexes.")]
			public bool drawThickHexes = false;
			[SettingsLabel("Use 'Gold' instead of 'Cost' in the metric display.")]
			public bool writeGoldNotCost = false;
			[SettingsLabel("Show the origin on the navigation map.")]
			public bool showCritelliOnMap = false;

			[SettingsLabel("Do not show the height and width simultaneously.")]
			public bool showHeightAndWidthSeparately = true;
			[SettingsLabel("Show Height of Solution")]
			public Keybinding KeyShowHeight = new() { Key = "U" };
			[SettingsLabel("Show Width of Solution")]
			public Keybinding KeyShowWidth = new() { Key = "I" };
			[SettingsLabel("Open Map")]
			public Keybinding KeyShowMap = new() { Key = "M" };
		}

		[SettingsLabel("Part-Placement Settings:")]
		public bool enablePartPlacementSettings = false;
		[SettingsLabel("")]
		public PartPlacementSettings partPlacementSettings = new();
		public class PartPlacementSettings : SettingsGroup
		{
			public override bool Enabled => Instance.enablePartPlacementSettings;
			[SettingsLabel("Disable the overlap-related part-placement restriction.")]
			public bool ignorePartOverlapPlacementRestrictions = false;
			[SettingsLabel("Allow track to overlap other parts (or itself) when dragging.")]
			public bool allowTrackOverlapDragging = false;
			[SettingsLabel("Disable cabinet-related part-placement restrictions.")]
			public bool ignoreCabinetPlacementRestrictions = false;
			[SettingsLabel("Ignore part allowances (i.e. permit multiple disposals, etc).")]
			public bool ignorePartAllowances = false;
			[SettingsLabel("Allow duplicate inputs and outputs.")]
			public bool allowMultipleIO = false;
			[SettingsLabel("Run simulations with the wrong number of outputs.")]
			public bool allowWrongNumberOfOutputs = false;
		}

		[SettingsLabel("Part-Editing Settings:")]
		public bool enablePartEditingSettings = false;
		[SettingsLabel("")]
		public PartEditingSettings partEditingSettings = new();
		public class PartEditingSettings : SettingsGroup
		{
			public override bool Enabled => Instance.enablePartEditingSettings;
			[SettingsLabel("When reversing a track, also reverse arms on the track.")]
			public bool alsoReverseArms = true;
			[SettingsLabel("Allow the creation of disjoint (i.e. 'quantum') tracks.")]
			public bool allowQuantumTracking = false;
			[SettingsLabel("Let conduits be created, destroyed, and swapped around.")]
			public bool allowConduitEditor = false;
		}

		[SettingsLabel("Miscellaneous Settings:")]
		public bool enableMiscellaneousSettings = false;
		[SettingsLabel("")]
		public MiscellaneousSettings miscellaneousEditingSettings = new();
		public class MiscellaneousSettings : SettingsGroup
		{
			public override bool Enabled => Instance.enableMiscellaneousSettings;
			[SettingsLabel("Show blank instruction sources in the programming tray.")]
			public bool drawBlanksOnProgrammingTray = false;
			[SettingsLabel("Allow multiple Period Override instructions.")]
			public bool allowMultipleOverrides = false;
			[SettingsLabel("Enable the Debugging Tools parts tray.")]
			public bool enableDebugTray = false;
			[SettingsLabel("Change the speedtray for ZoomTool compatibility.")]
			public bool speedtrayZoomtoolWorkaround = false;
		}
	}
	public override void ApplySettings()
	{
		base.ApplySettings();

		var SET = (MySettings)Settings;
		common.drawThickHexes = SET.displayEditingSettings.drawThickHexes;

		AreaDisplay.showHeightAndWidthSeparately = SET.displayEditingSettings.showHeightAndWidthSeparately;

		ConduitEditor.allowConduitEditor = SET.partEditingSettings.allowConduitEditor;

		DebugParts.enableDebugTray = SET.miscellaneousEditingSettings.enableDebugTray;

		InstructionEditor.ApplySettings(SET.miscellaneousEditingSettings.drawBlanksOnProgrammingTray, SET.miscellaneousEditingSettings.allowMultipleOverrides);

		Miscellaneous.allowWrongNumberOfOutputs = SET.partPlacementSettings.allowWrongNumberOfOutputs;

		MetricDisplay.writeGoldNotCost = SET.displayEditingSettings.writeGoldNotCost;

		Navigation.showCritelliOnMap = SET.displayEditingSettings.showCritelliOnMap;

		PartsPanel.ignorePartAllowances = SET.partPlacementSettings.ignorePartAllowances;
		PartsPanel.allowMultipleIO = SET.partPlacementSettings.allowMultipleIO;

		PartPlacement.ignorePartOverlapPlacementRestrictions = SET.partPlacementSettings.ignorePartOverlapPlacementRestrictions;
		PartPlacement.allowTrackOverlapDragging = SET.partPlacementSettings.allowTrackOverlapDragging;
		PartPlacement.ignoreCabinetPlacementRestrictions = SET.partPlacementSettings.ignoreCabinetPlacementRestrictions;

		SpeedTray.speedtrayZoomtoolWorkaround = SET.miscellaneousEditingSettings.speedtrayZoomtoolWorkaround;

		TrackEditor.alsoReverseArms = SET.partEditingSettings.alsoReverseArms;
		TrackEditor.allowQuantumTracking = SET.partEditingSettings.allowQuantumTracking;
	}
	public override void Load()
	{
		MainClassAsMod = this;
		Settings = new MySettings();
	}
	public override void LoadPuzzleContent()
	{
		TrackEditor.LoadPuzzleContent();
		ConduitEditor.LoadPuzzleContent();
		InstructionEditor.LoadPuzzleContent();
		MirrorTool.LoadPuzzleContent();
		Miscellaneous.LoadPuzzleContent();
		SpeedTray.LoadPuzzleContent();
		PartsPanel.LoadPuzzleContent();
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

		On.SolutionEditorProgramPanel.method_221 += SolutionEditorProgramPanel_Method_221;

		On.SolutionEditorScreen.method_511 += SES_Method_511;

		PartPlacement.PostLoad();
	}

	public static float SES_Method_511(On.SolutionEditorScreen.orig_method_511 orig, SolutionEditorScreen SES_self)
	{
		return orig(SES_self) * SpeedTray.SimSpeedFactor();
	}

	public void SolutionEditorProgramPanel_Method_221(On.SolutionEditorProgramPanel.orig_method_221 orig, SolutionEditorProgramPanel SEPP_self, float param_5658)
	{
		AreaDisplay.SEPP_method_221(SEPP_self);
		orig(SEPP_self, param_5658);
		MetricDisplay.SEPP_method_221(SEPP_self);
		SpeedTray.SEPP_method_221(SEPP_self);
	}
	public void SES_Method_50(On.SolutionEditorScreen.orig_method_50 orig, SolutionEditorScreen SES_self, float param_5703)
	{
		ConduitEditor.SolutionEditorScreen_method_50(SES_self);
		MirrorTool.SolutionEditorScreen_method_50(SES_self);
		InstructionEditor.SolutionEditorScreen_method_50(SES_self);
		//DebugParts.SolutionEditorScreen_method_50(SES_self);
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
}