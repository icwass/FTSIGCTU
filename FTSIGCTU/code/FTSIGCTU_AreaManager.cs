using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU
{
	using Texture = class_256;

	public static class AreaManager
	{
		public static bool enabled = false;
		public static bool thickHexes = false;
		public static HashSet<HexIndex> AreaHashSet_Glyph = new HashSet<HexIndex>();// no collision here yet, only covered by some glyph or other part
		public static HashSet<HexIndex> AreaHashSet_ArmShaft = new HashSet<HexIndex>();// no collision here yet, only covered by an arm's shaft
		public static HashSet<HexIndex> AreaHashSet_ArmBase = new HashSet<HexIndex>();// would collide with an atom, but not an arm base
		public static HashSet<HexIndex> AreaHashSet_Atom = new HashSet<HexIndex>();// would collide with anything
		public static Texture[] areaTex = new Texture[6];


		public static void init() // should be called during LoadPuzzleContent()
		{
			areaTex[0] = class_238.field_1989.field_90.field_172;
			areaTex[1] = class_235.method_615("textures/parts/areahex_blank");
			areaTex[2] = class_235.method_615("textures/parts/areahex_atom");
			areaTex[3] = class_235.method_615("textures/parts/areahex_armbase");
			areaTex[4] = class_235.method_615("textures/parts/areahex_armshaft");
			areaTex[5] = class_235.method_615("textures/parts/areahex_glyph");
			clear();
		}

		public static void clear()
		{
			AreaHashSet_Glyph = new HashSet<HexIndex>();
			AreaHashSet_ArmShaft = new HashSet<HexIndex>();
			AreaHashSet_ArmBase = new HashSet<HexIndex>();
			AreaHashSet_Atom = new HashSet<HexIndex>();
		}

		public static void drawHex(HashSet<HexIndex> hexIndexSet, HexIndex hexIndex, Vector2 vec2)
		{
			Texture texture = areaTex[0];
			Vector2 vector2 = class_187.field_1742.method_491(hexIndex, vec2) - areaTex[0].field_2056.ToVector2() / 2;
			if (!enabled)
			{
				if (thickHexes) class_135.method_271(areaTex[0], Color.White.WithAlpha(1f/*a*/), vector2.Rounded());
				return;
			}

			if (AreaHashSet_Atom.Contains(hexIndex))
			{
				texture = areaTex[2];
			}
			else if (AreaHashSet_ArmBase.Contains(hexIndex))
			{
				texture = areaTex[3];
			}
			else if (AreaHashSet_ArmShaft.Contains(hexIndex))
			{
				texture = areaTex[4];
			}
			else
			{
				texture = areaTex[5];
			}

			class_135.method_271(texture, Color.White.WithAlpha(1f), vector2.Rounded());
			if (thickHexes) class_135.method_271(texture, Color.White.WithAlpha(1f), vector2.Rounded());
		}

		public static void checkHexes(Sim Sim_self, List<Sim.struct_122> struct122List, double dist_comparand, HashSet<HexIndex> hashSet) // based on method_1840
		{
			foreach (var item in struct122List)
			{
				Vector2 param_5376 = item.field_3851;
				//almost reimplements the original method
				var sim_dyn = new DynamicData(Sim_self);
				float atomRadius = sim_dyn.Get<float>("field_3831");
				float armbaseRadius = sim_dyn.Get<float>("field_3833");
				HexIndex hexIndex1 = class_187.field_1742.method_493(param_5376, Vector2.Zero);
				foreach (HexIndex andAdjacentOffset in HexIndex.HereAndAdjacentOffsets)
				{
					HexIndex hexIndex2 = hexIndex1 + andAdjacentOffset;
					Vector2 b = class_187.field_1742.method_491(hexIndex2, Vector2.Zero);
					double dist = (double)Vector2.Distance(param_5376, b);
					if (dist < dist_comparand) hashSet.Add(hexIndex2);
				}
			}
		}

		public static void addHex(Sim.struct_122 item, HashSet<HexIndex> hashSet)
		{
			HexIndex hexIndex1 = class_187.field_1742.method_493(item.field_3851, Vector2.Zero);
			hashSet.Add(hexIndex1);
		}

		public static int fetchBlueprintMetric(SolutionEditorBase SEB)
		{
			HashSet<HexIndex> hashSet = new HashSet<HexIndex>();

			foreach (HexIndex hexIndex in SEB.method_502().method_1947(struct_18.field_1431, 0)) hashSet.Add(hexIndex);

			foreach (Part part in SEB.method_502().method_1937().Where(part => part.method_1201()))
			{
				PartSimState partSimState = SEB.method_507().method_481(part);
				int num = part.method_1165();
				if (part.method_1159().field_1535) num = partSimState.field_2725;

				for (int index = 0; index < part.field_2696.Length; ++index)
				{
					HexRotation rotation = part.method_1163();
					for (int q = 1; q <= num; ++q) hashSet.Add(part.method_1161() + new HexIndex(q, 0).Rotated(rotation));
				}
			}
			return hashSet.Count();
		}

		public static void Method1824(SolutionEditorBase param_5365)
		{
			//hexes covered by a part
			foreach (HexIndex hexIndex in param_5365.method_502().method_1947(struct_18.field_1431, 0)) AreaHashSet_Glyph.Add(hexIndex);
		}
		public static void Method1835(Sim Sim_self)
		{
			//almost reimplements the original method
			var sim_dyn = new DynamicData(Sim_self);
			var SEB = sim_dyn.Get<SolutionEditorBase>("field_3818");
			double atomRadius = (double)sim_dyn.Get<float>("field_3831");
			double armbaseRadius = (double)sim_dyn.Get<float>("field_3833");
			var FIELD_3827 = sim_dyn.Get<float>("field_3827");


			List<Sim.struct_122> struct122List = new List<Sim.struct_122>();
			List<Sim.struct_122> struct122List2 = new List<Sim.struct_122>();

			//find production chambers (pulled out of the loop for optimization)
			class_261 class261;
			if (SEB.method_502().method_1934().field_2779.method_99<class_261>(out class261))
			{
				foreach (class_189 class189 in class261.field_2071)//chambers
				{
					foreach (HexIndex hexIndex1 in class189.field_1747.field_1729)
					{
						HexIndex hexIndex2 = class189.field_1746 + hexIndex1;
						Sim.struct_122 struct122 = new Sim.struct_122();
						struct122.field_3850 = (Sim.enum_190)3;
						struct122.field_3851 = class_187.field_1742.method_492(hexIndex2);
						struct122.field_3852 = sim_dyn.Get<float>("field_3835"); ;
						Sim.struct_122 struct122_3 = struct122;
						struct122List.Add(struct122_3);
					}
				}
			}
			//add chamber walls
			foreach (var item in struct122List) addHex(item, AreaHashSet_ArmBase);
			struct122List.Clear();

			//find produced atoms (pulled out of the loop for optimization, fall-through processes it with the other atoms)
			struct122List.AddRange(sim_dyn.Get<List<Sim.struct_122>>("field_3826"));

			for (float field3827 = FIELD_3827; (double)field3827 <= 1.0; field3827 += FIELD_3827)
			{
				//find molecule atoms
				foreach (Molecule molecule in SEB.method_507().method_483())
				{
					Part part;
					Vector2 moleculeOrigin;
					HexIndex hexIndex;
					float radians;
					if (SEB.method_1985(molecule).method_99<Part>(out part))
					{
						PartSimState partSimState = SEB.method_507().method_481(part);
						class_236 class236 = SEB.method_1990(part, Vector2.Zero, field3827);
						moleculeOrigin = class236.field_1984;
						hexIndex = partSimState.field_2724;
						radians = class236.field_1987;
					}
					else
					{
						moleculeOrigin = Vector2.Zero;
						hexIndex = new HexIndex(0, 0);
						radians = 0.0f;
					}
					foreach (KeyValuePair<HexIndex, Atom> keyValuePair in (IEnumerable<KeyValuePair<HexIndex, Atom>>)molecule.method_1100())
					{
						HexIndex key = keyValuePair.Key;
						Vector2 vector2_2 = moleculeOrigin + class_187.field_1742.method_492(key - hexIndex).Rotated(radians);
						struct122List.Add(new Sim.struct_122((Sim.enum_190)0, vector2_2));
					}
				}
				//find berlo atoms
				foreach (Part part in SEB.method_502().method_1937().Where(part => part.method_1202()).ToArray<Part>())
				{
					class_236 class236 = SEB.method_1990(part, Vector2.Zero, field3827);
					foreach (HexIndex key in part.method_1159().field_1544.Keys)
					{
						Vector2 vector2_3 = class_187.field_1742.method_492(key);
						Vector2 vector2_4 = class236.field_1984 + vector2_3.Rotated(class236.field_1985);
						struct122List.Add(new Sim.struct_122((Sim.enum_190)0, vector2_4));
					}
				}
				//check atoms
				checkHexes(Sim_self, struct122List, 2.0 * atomRadius, AreaHashSet_ArmBase);
				checkHexes(Sim_self, struct122List, atomRadius + armbaseRadius, AreaHashSet_Atom);
				struct122List.Clear();

				//find arm bases and grippers
				bool Method_1872(Part param_3749) { return param_3749.method_1159().field_1533;} //is programmable part, i.e. an arm
				Sim.struct_122 struct122;
				foreach (Part part1 in SEB.method_502().field_3919.Where(part => part.method_1159().field_1533).ToArray<Part>())
				{
					class_236 class236_1 = SEB.method_1990(part1, Vector2.Zero, field3827);
					//check arm base
					addHex(new Sim.struct_122((Sim.enum_190)1, class236_1.field_1984), AreaHashSet_ArmBase);
					//find grippers
					foreach (Part part2 in part1.field_2696)
					{
						class_236 class236_2 = SEB.method_1990(part2, Vector2.Zero, field3827);
						struct122 = new Sim.struct_122((Sim.enum_190)2, class236_2.field_1984);
						struct122.field_3852 = sim_dyn.Get<float>("field_3834");
						struct122List.Add(struct122);
					}
				}
				//check grippers
				checkHexes(Sim_self, struct122List, 2.0 * atomRadius, AreaHashSet_ArmShaft);
				struct122List.Clear();
			}
		}
		public static void Method1838(Sim Sim_self)
		{
			var sim_dyn = new DynamicData(Sim_self);
			var SEB = sim_dyn.Get<SolutionEditorBase>("field_3818");
			var FIELD_3827 = sim_dyn.Get<float>("field_3827");

			List<Sim.struct_122> struct122List = new List<Sim.struct_122>();
			Sim.struct_122 struct122;

			for (float field3827 = FIELD_3827; (double)field3827 <= 1.0; field3827 += FIELD_3827)
			{
				foreach (Part part in SEB.method_502().method_1937().Where(part => part.method_1201()))
				{
					PartSimState partSimState = SEB.method_507().method_481(part);
					class_236 class236 = SEB.method_1990(part, Vector2.Zero, field3827);
					int num = part.method_1165();
					if (part.method_1159().field_1535) num = partSimState.field_2725;

					for (int index = 0; index < part.field_2696.Length; ++index)
					{
						HexRotation rotation = part.method_1159().method_311(index).method_1087();
						for (int q = 1; q <= num; ++q)
						{
							HexIndex hexIndex = new HexIndex(q, 0).Rotated(rotation);
							Vector2 vector2 = class_187.field_1742.method_492(hexIndex);

							class_236 class236_2 = SEB.method_1990(part, Vector2.Zero, field3827);
							struct122 = new Sim.struct_122((Sim.enum_190)2, class236.field_1984 + vector2.Rotated(class236.field_1985));
							struct122.field_3852 = sim_dyn.Get<float>("field_3834");
							struct122List.Add(struct122);

							float atomRadius = sim_dyn.Get<float>("field_3831");
							checkHexes(Sim_self, struct122List, 2.0 * atomRadius, AreaHashSet_ArmShaft);
							struct122List.Clear();
						}
					}
				}
			}
		}
		public static void c153_Method221(class_153 c153_self)
		{
			var c153_dyn = new DynamicData(c153_self);
			var SES = c153_dyn.Get<SolutionEditorScreen>("field_2007");

			if (SES.field_4015)
			{
				HashSet<HexIndex> hexIndexSet = new HashSet<HexIndex>();
				foreach (Part part in SES.method_502().method_1937().Where(part => !part.method_1159().field_1537)) hexIndexSet.UnionWith(part.method_1186(SES.method_502()));
				foreach (HexIndex hexIndex in SES.method_507().method_484()) drawHex(hexIndexSet, hexIndex, c153_self.method_359());
			}
		}
	}
}