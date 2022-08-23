using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using Quintessential.Settings;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU
{
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
			[SettingsLabel("Open group settings")]
			public bool boolean = true;

			[SettingsLabel("Group settings:")]
			public GroupOfSettings GroupOfSettingsThingy = new GroupOfSettings(this);

			public class GroupOfSettings : SettingsGroup
			{
				[YamlIgnore]
				private MySettings container;
				public GroupOfSettings(MySettings c) { container = c; }

				public override bool Enabled => container.boolean;

				[SettingsLabel("Option (Ctrl + O)")]
				public Keybinding OptionCtrlO = new Keybinding() { Key = "O", Control = true };
			}
		}
		public override void ApplySettings()
		{
			base.ApplySettings();
			//if (((MySettings)(Settings)).boolean){}
		}
		public override void Load()
		{
			Settings = new MySettings();
		}
		public override void LoadPuzzleContent() { }
		public override void Unload() { }
		public override void PostLoad() { }
	}
}