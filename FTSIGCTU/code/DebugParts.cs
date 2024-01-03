using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;
using PartType = class_139;
using Permissions = enum_149;
using PartTypes = class_191;
using Texture = class_256;

public static class DebugParts
{
	//data structs, enums, variables
	private static Texture[] textures;

	public static bool enableDebugTray = false;

	private static PartType Expunction;

	const int flashStep = 100;
	const int flashTime = 40;
	const int expunctionConfigCount = 7;

	private enum resource : byte
	{
		expunction_base,
		expunction_lower_fixed,
		expunction_lower_ring,
		expunction_lower_glow,
		expunction_upper_fixed,
		expunction_upper_rings,
		expunction_indicator_off,
		expunction_indicator_on,
		COUNT,
	}

	//---------------------------------------------------//
	//public methods
	public static void LoadPuzzleContent()
	{
		//


		On.SolutionEditorPartsPanel.class_428.method_2047 += method2047_AddDebugTray;

	}

	public static void PostLoad()
	{
		//




	}

	//---------------------------------------------------//
	//internal helper methods



	//---------------------------------------------------//
	//internal main methods

	private static void method2047_AddDebugTray(
		On.SolutionEditorPartsPanel.class_428.orig_method_2047 orig,
		SolutionEditorPartsPanel.class_428 class428_self,
		string trayName,
		List<PartTypeForToolbar> list)
	{
		orig(class428_self, trayName, list);
		if (!enableDebugTray) return;
		if (trayName != class_134.method_253("Glyphs", string.Empty)) return;

		//append Debugging Tray
		List<PartTypeForToolbar> toolbarList = new List<PartTypeForToolbar>();
		toolbarList.Add(PartTypeForToolbar.method_1225(PartTypes.field_1781, true, true));

		orig(class428_self, class_134.method_253("Debugging Tools", string.Empty), toolbarList);
	}

	//---------------------------------------------------//


	public static void oldLoadPuzzleContent()
	{
		//load textures

		textures = new Texture[(int)resource.COUNT];
		var RNG = new Random();

		string path = "ftsigctu/textures/parts/expunction/";
		textures[(int)resource.expunction_base] = class_235.method_615(path + "base");
		textures[(int)resource.expunction_lower_fixed] = class_235.method_615(path + "lower_fixed");
		textures[(int)resource.expunction_lower_glow] = class_235.method_615(path + "lower_glow");
		textures[(int)resource.expunction_lower_ring] = class_235.method_615(path + "lower_ring");
		textures[(int)resource.expunction_upper_fixed] = class_235.method_615(path + "upper_fixed");
		textures[(int)resource.expunction_upper_rings] = class_235.method_615(path + "upper_rings");

		//debug?
		textures[(int)resource.expunction_indicator_off] = class_235.method_615(path + "indicator_off");
		textures[(int)resource.expunction_indicator_on] = class_235.method_615(path + "indicator_on");



		path = "ftsigctu/textures/parts/icons/";

		Expunction = new PartType()
		{
			/*ID*/field_1528 = "ftsigctu-debug-expunction",
			/*Name*/field_1529 = class_134.method_253("[DEBUG] Glyph of Expunction", string.Empty),
			/*Desc*/field_1530 = class_134.method_253("Complete removes all atoms that get caught in its expunction field. Use 'W' and 'S' to configure the field's range.", string.Empty),
			/*Cost*/field_1531 = 100000,
			/*Is a Glyph?*/field_1539 = true,
			/*Hex Footprint*/field_1540 = new HexIndex[7]
			{
				new HexIndex(0, 0),
				new HexIndex(1, 0),
				new HexIndex(0, 1),
				new HexIndex(-1, 1),
				new HexIndex(-1, 0),
				new HexIndex(0, -1),
				new HexIndex(1, -1)
			},
			/*Icon*/field_1547 = class_235.method_615(path + "expunction"),
			/*Hover Icon*/field_1548 = class_235.method_615(path + "expunction_hover"),
			/*Glow (Shadow)*/field_1549 = class_238.field_1989.field_97.field_372,
			/*Stroke (Outline)*/field_1550 = class_238.field_1989.field_97.field_373,
			/*Permissions*/field_1551 = Permissions.None
		};

		QApi.AddPartType(Expunction, (part, pos, editor, renderer) => {
			//draw code, based off disposal
			class_236 class236 = editor.method_1989(part, pos); // needed so we can fetch the current simulation timestep
			PartSimState partSimState = editor.method_507().method_481(part); // needed so we can tell if the glyph is firing, and with what inputs/outputs

			var partIndex = common.getPartIndex(part); // contains both animation_frame and config_data information
			if (partIndex > flashStep) common.setPartIndex(part, partIndex - flashStep);
			int flashStrength = partIndex / flashStep; // animation_frame
			float flashFactor = flashStrength / (float)flashTime;
			float shakeFactor = 0.1f* flashFactor;

			partIndex = partIndex % flashStep; // config_data
			if (editor.method_503() == enum_128.Stopped) common.setPartIndex(part, partIndex);
			if (common.getPartIndex(part) == 0) shakeFactor = 0f;
			Vector2 shake() => new Vector2(RNG.Next(-15, 15) * shakeFactor, RNG.Next(-15, 15) * shakeFactor) + new Vector2(1f, 1f);

			Texture tex = textures[(int)resource.expunction_base];//base
			renderer.method_521(tex, tex.method_691() + new Vector2(1f, 1f));
			renderer.method_528(textures[(int)resource.expunction_lower_ring], new HexIndex(0, 0), shake() + new Vector2(1f, 1f));
			tex = textures[(int)resource.expunction_lower_fixed];
			renderer.method_521(tex, tex.method_691() + shake());

			//draw the red glow (based on method_523)
			tex = textures[(int)resource.expunction_lower_glow];
			Matrix4 matrixA = Matrix4.method_1070(renderer.field_1797.ToVector3(0.0f));
			Matrix4 matrixB = Matrix4.method_1073(renderer.field_1798);
			Matrix4 matrixC = Matrix4.method_1070(-(tex.method_691() + shake()).ToVector3(0.0f));
			Matrix4 matrixD = Matrix4.method_1074(tex.field_2056.ToVector3(0.0f));
			Matrix4 matrix42 = Matrix4.method_1074(tex.field_2056.ToVector3(0.0f));
			class_135.method_262(tex, Color.White.WithAlpha(flashFactor * flashFactor), matrixA * matrixB * matrixC * matrixD);

			tex = textures[(int)resource.expunction_upper_fixed];
			renderer.method_521(tex, tex.method_691() + shake());
			renderer.method_528(textures[(int)resource.expunction_upper_rings], new HexIndex(0, 0), shake());

			//draw indicators for the type of expunging

			for (int i = 1; i < expunctionConfigCount; i++)
			{
				tex = i <= partIndex ? textures[(int)resource.expunction_indicator_on] : textures[(int)resource.expunction_indicator_off];
			renderer.method_529(tex, new HexIndex(1, 0).Rotated(new HexRotation(i - 1)), new Vector2(0, 0f));
			}
		});

		//temporarily disabled, for now
		//QApi.AddPartTypeToPanel(Expunction, PartTypes.field_1782);//inserts part type after equilibrium (near the bottom)

		QApi.RunAfterCycle((sim_self, flag) =>
		{
			var sim_dyn = new DynamicData(sim_self);
			var SEB = sim_dyn.Get<SolutionEditorBase>("field_3818");
			var solution = SEB.method_502();
			var partList = solution.field_3919;
			var partSimStates = sim_dyn.Get<Dictionary<Part, PartSimState>>("field_3821");

			var moleculeList = sim_dyn.Get<List<Molecule>>("field_3823");

			//////// BOILER PLATE

			List<Part> gripperList = new List<Part>();
			foreach (Part part in partList)
			{
				foreach (Part key in part.field_2696)//for each gripper
				{
					if (partSimStates[key].field_2729.method_1085())//if part is holding onto a molecule
					{
						gripperList.Add(key);//add gripper to gripperList
						//expanded version of sim_self.method_1842(key); //release molecule from the gripper
						PartSimState partSimState = partSimStates[key];
						partSimState.field_2728 = false;
						partSimState.field_2729 = struct_18.field_1431;
					}
				}
			}
			//////// actual code
			foreach (Part part in partList)
			{
				PartSimState partSimState1 = partSimStates[part];

				//we now use Reflection jank to use a private method
				Type simType = typeof(Sim);
				MethodInfo Method_1850 = simType.GetMethod("method_1850", BindingFlags.NonPublic | BindingFlags.Instance);//atom exists at location
				MethodInfo Method_1856 = simType.GetMethod("method_1856", BindingFlags.NonPublic | BindingFlags.Instance);

				if (common.getPartType(part) == Expunction)
				{
					HexIndex pos = common.getPartOrigin(part);
					HexRotation rotation = common.getPartRotation(part);

					var configData = common.getPartIndex(part) % flashStep;
					Func<HexIndex, bool> onDeathRow;

					switch (configData)
					{
						default:
						case 0: onDeathRow = hex => hex.Q == 0 && hex.R == 0; break;
						case 1: onDeathRow = hex => hex.Q >= 0 && hex.R == 0; break;
						case 2: onDeathRow = hex => hex.Q >= 0 && hex.R >= 0; break;
						case 3: onDeathRow = hex => hex.Q + hex.R >= 0 && hex.R >= 0; break;
						case 4: onDeathRow = hex => hex.R >= 0; break;
						case 5: onDeathRow = hex => hex.Q <= 0 || hex.R >= 0; break;
						case 6: onDeathRow = hex => hex.Q + hex.R <= 0 || hex.R >= 0; break;
					}

					HashSet<HexIndex> deathSet = new();
					HashSet<HexIndex> killedSet = new();
					foreach (var molecule in moleculeList)
					{
						foreach (var hex in molecule.method_1100().Keys)
						{
							if (onDeathRow(hex.RotatedAround(pos, rotation.Negative()) - pos)) deathSet.Add(hex);
						}
					}

					bool expunctionActivated = false;
					foreach (var hex in deathSet)
					{
						AtomReference atomReference;
						Maybe<AtomReference> maybeAtomReference = (Maybe<AtomReference>)struct_18.field_1431;
						foreach (var molecule in moleculeList)
						{
							Atom atom;
							if (molecule.method_1100().TryGetValue(hex, out atom))
							{
								maybeAtomReference = (Maybe<AtomReference>)new AtomReference(molecule, hex, atom.field_2275, atom, flag);
								break;
							}
						}

						bool atomExists = maybeAtomReference.method_99<AtomReference>(out atomReference);
						if (atomExists)
						{
							expunctionActivated = true;
							var arg = class_187.field_1742.method_492(part.method_1161());
							atomReference.field_2277.method_1107(atomReference.field_2278);
							killedSet.Add(hex);

							common.setPartIndex(part, common.getPartIndex(part) % (flashStep) + flashTime * flashStep);
						}
					}
					if (expunctionActivated)
					{
						common.playSound(class_238.field_1991.field_1842, 0.1f); // disposal sound

						foreach (var target in killedSet)
						{
							var arg = class_187.field_1742.method_492(target);
							//SEB.field_3935.Add(new class_228(SEB, (enum_7)1, arg + new Vector2(147f, 47f), class_238.field_1989.field_90.field_242, 30f, Vector2.Zero, 0.0f)); // disposal smoke
							SEB.field_3936.Add(new class_228(SEB, (enum_7)1, arg + new Vector2(80f, 0.0f), class_238.field_1989.field_90.field_240, 30f, Vector2.Zero, 0.0f)); // disposal flash
						}
					}
				}
			}

			//////// MORE BOILER PLATE

			List<Molecule> source1 = new List<Molecule>();

			foreach (Molecule molecule9 in moleculeList)
			{
				if (molecule9.field_2638)
				{
					HashSet<HexIndex> source2 = new HashSet<HexIndex>(molecule9.method_1100().Keys);
					Queue<HexIndex> hexIndexQueue = new Queue<HexIndex>();
					while (source2.Count > 0)
					{
						if (hexIndexQueue.Count == 0)
						{
							HexIndex key = source2.First<HexIndex>();
							source2.Remove(key);
							hexIndexQueue.Enqueue(key);
							source1.Add(new Molecule());
							source1.Last<Molecule>().method_1105(molecule9.method_1100()[key], key);
						}
						HexIndex hexIndex = hexIndexQueue.Dequeue();
						foreach (class_277 class277 in (IEnumerable<class_277>)molecule9.method_1101())
						{
							Maybe<HexIndex> maybe = (Maybe<HexIndex>)struct_18.field_1431;
							if (class277.field_2187 == hexIndex)
								maybe = (Maybe<HexIndex>)class277.field_2188;
							else if (class277.field_2188 == hexIndex)
								maybe = (Maybe<HexIndex>)class277.field_2187;
							if (maybe.method_1085() && source2.Contains(maybe.method_1087()))
							{
								source2.Remove(maybe.method_1087());
								hexIndexQueue.Enqueue(maybe.method_1087());
								source1.Last<Molecule>().method_1105(molecule9.method_1100()[maybe.method_1087()], maybe.method_1087());
							}
						}
					}
					foreach (class_277 class277 in (IEnumerable<class_277>)molecule9.method_1101())
					{
						foreach (Molecule molecule10 in source1)
						{
							if (molecule10.method_1100().ContainsKey(class277.field_2187))
							{
								molecule10.method_1111(class277.field_2186, class277.field_2187, class277.field_2188);
								break;
							}
						}
					}
				}
			}
			moleculeList.RemoveAll(Sim.class_301.field_2479 ?? (mol => mol.field_2638));
			moleculeList.AddRange((IEnumerable<Molecule>)source1);

			foreach (Part part in gripperList)
			{
				//expanded version of sim_self.method_1841(part);//give gripper a molecule back
				PartSimState partSimState = partSimStates[part];
				HexIndex field2724 = partSimState.field_2724;
				partSimState.field_2728 = true;
				partSimState.field_2729 = sim_self.method_1848(field2724);
			}

			//
			sim_dyn.Set("field_3821", partSimStates);
			sim_dyn.Set("field_3823", moleculeList);
		});


	}

	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		//var SES_self_dyn = new DynamicData(SES_self);
		//bool isStopped = (SES_self.method_503() == enum_128.Stopped);

		var current_interface = SES_self.field_4010;
		if (current_interface.GetType() == (new PartDraggingInputMode()).GetType())
		{
			//we are dragging! do some stuff
			var DraggedParts = new DynamicData(current_interface).Get<List<PartDraggingInputMode.DraggedPart>>("field_2712");

			if (DraggedParts.Count == 1) //then we can edit the part however we want!
			{
				Part part = DraggedParts[0].field_2722;
				PartType partType = part.method_1159();

				void helperMethod(int offset, int modulo = 1)
				{
					var configData = (common.getPartIndex(part) + offset + modulo) % modulo;
					common.setPartIndex(part, configData); // only happens when simulation is stopped, so no need to check animation frame
					GameLogic.field_2434.field_2451.method_588(true);
					common.playSound(class_238.field_1991.field_1854, 0.2f); // sounds/piece_modify
				}

				if (partType == Expunction)
				{
					if (Input.IsSdlKeyPressed(SDL.enum_160.SDLK_w))
					{
						helperMethod(1, expunctionConfigCount);
					}
					else if (Input.IsSdlKeyPressed(SDL.enum_160.SDLK_s))
					{
						helperMethod(-1, expunctionConfigCount);
					}
				}
			}
		}
	}
	//---------------------------------------------------//
}