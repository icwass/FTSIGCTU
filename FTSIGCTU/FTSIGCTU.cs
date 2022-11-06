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


	public static bool disableOverlapDetection = false;
	
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
		[SettingsLabel("Allow duplicate Disposals, Berlos, inputs and outputs.")]
		public bool allowDuplicateParts = false;

		//need to put this somewhere
		[SettingsLabel("Disable overlap detection.")]
		public bool disableOverlapDetection = false;
	}
	public override void ApplySettings()
	{
		base.ApplySettings();

		var SET = (MySettings)Settings;
		common.ApplySettings(SET.drawThickHexes);
		TrackEditor.ApplySettings(SET.alsoReverseArms, SET.allowQuantumTracking);
		ConduitEditor.ApplySettings(SET.allowConduitEditor);
		InstructionEditor.ApplySettings(SET.drawBlanksOnProgrammingTray, SET.allowMultipleOverrides);
		Miscellaneous.ApplySettings(SET.allowDuplicateParts);

		disableOverlapDetection = SET.disableOverlapDetection;
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

		On.Solution.method_1947 += Solution_method_1947;
		On.Sim.method_1824 += Sim_method_1824;
		//(this == Solution) HashSet<HexIndex> method_1947(Maybe<Part> param_5487, enum_137 param_5488)
	}
	public void SES_Method_50(On.SolutionEditorScreen.orig_method_50 orig, SolutionEditorScreen SES_self, float param_5703)
	{
		ConduitEditor.SolutionEditorScreen_method_50(SES_self);
		MirrorTool.SolutionEditorScreen_method_50(SES_self);
		Miscellaneous.SolutionEditorScreen_method_50(SES_self);
		orig(SES_self, param_5703);
		TrackEditor.SolutionEditorScreen_method_50(SES_self);
	}
	public static void c153_Method_221(On.class_153.orig_method_221 orig, class_153 c153_self, float param_3616)
	{
		orig(c153_self, param_3616);
		TrackEditor.class153_method_221(c153_self);
	}


	public HashSet<HexIndex> Solution_method_1947(On.Solution.orig_method_1947 orig, Solution solution_self, Maybe<Part> param_5487, enum_137 param_5488)
	{
		if (disableOverlapDetection)
		{
			return new HashSet<HexIndex>();
		}
		else
		{
			return orig(solution_self, param_5487, param_5488);
		}
	}


	public static Maybe<Sim> Sim_method_1824(On.Sim.orig_method_1824 orig, SolutionEditorBase param_5365)
	{
		var maybeRet = orig(param_5365);
		if (!disableOverlapDetection || !maybeRet.method_1085())
		{
			return maybeRet;
		}

		var ret = maybeRet.method_1087();
		//method_1947 was disabled, so we need to add some area hexes manually
		HashSet<HexIndex> hashSet = new();
		//hashSet = param_5365.method_502().method_1947((Maybe<Part>)struct_18.field_1431, (enum_137)0)
		{
			HashSet<HexIndex> hexIndexSet1 = new HashSet<HexIndex>();
			var THIS = param_5365.method_502();
			foreach (Part part in THIS.field_3919)
			{
				if ((Maybe<Part>)part != (Maybe<Part>)struct_18.field_1431)
				{
					HashSet<HexIndex> hexIndexSet2 = part.method_1187(THIS, (enum_137)0, part.method_1161(), part.method_1163());
					hexIndexSet1.UnionWith((IEnumerable<HexIndex>)hexIndexSet2);
				}
			}
			hashSet = hexIndexSet1;
		}
		foreach (HexIndex hexIndex in hashSet)
		{
			ret.field_3824.Add(hexIndex);
		}

		return ret;
	}
}