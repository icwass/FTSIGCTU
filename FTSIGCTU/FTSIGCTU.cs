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
	public class MySettings
	{
		[SettingsLabel("Use thicker lines when highlighting hexes.")]
		public bool drawThickHexes = false;
		[SettingsLabel("When reversing a track, also reverse arms on the track.")]
		public bool alsoReverseArms = true;
		[SettingsLabel("Allow the creation of disjoint (i.e. 'quantum') tracks.")]
		public bool allowQuantumTracking = false;
	}
	public override void ApplySettings()
	{
		base.ApplySettings();

		common.ApplySettings(((MySettings)(Settings)).drawThickHexes);
		TrackEditor.ApplySettings(((MySettings)(Settings)).alsoReverseArms, ((MySettings)(Settings)).allowQuantumTracking);
	}
	public override void Load()
	{
		Settings = new MySettings();
	}
	public override void LoadPuzzleContent()
	{
		TrackEditor.LoadPuzzleContent();
	}
	public override void Unload() { }
	public override void PostLoad()
	{
		On.SolutionEditorScreen.method_50 += SES_Method_50;
		On.class_153.method_221 += c153_Method_221;
	}
	public void SES_Method_50(On.SolutionEditorScreen.orig_method_50 orig, SolutionEditorScreen SES_self, float param_5703)
	{
		orig(SES_self, param_5703);
		TrackEditor.SolutionEditorScreen_method_50(SES_self);
	}
	public static void c153_Method_221(On.class_153.orig_method_221 orig, class_153 c153_self, float param_3616)
	{
		orig(c153_self, param_3616);
		TrackEditor.class153_method_221(c153_self);
	}
}