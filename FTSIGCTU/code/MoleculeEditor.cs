using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;

namespace FTSIGCTU;

public static class MoleculeEditor
{
	//data structs, enums, variables
	public static bool allowDisjointMolecules = false;
	private static IDetour hook_MoleculeEditorScreen_method_1132;

    private delegate void orig_MoleculeEditorScreen_method_1132(MoleculeEditorScreen self);
	//---------------------------------------------------//
	//public methods
	public static void LoadPuzzleContent()
	{
		hook_MoleculeEditorScreen_method_1132 = new Hook(
			MainClass.PrivateMethod<MoleculeEditorScreen>("method_1132"),
			CheckIfMoleculeLegal);
	}

	public static void Unload()
	{
		hook_MoleculeEditorScreen_method_1132.Dispose();
	}

	//---------------------------------------------------//
	//internal main methods

	private static bool CheckIfRepetition(Atom param_4870)
	{
		return param_4870.field_2275 == class_175.field_1689;
	}

	private static bool CheckRepetitionLocation(KeyValuePair<HexIndex, Atom> param_4871)
	{
		return param_4871.Value.field_2275 == class_175.field_1689;
	}

	private static HexIndex GetHexIndexKey(KeyValuePair<HexIndex, Atom> param_4872)
	{
		return param_4872.Key;
	}

	private static void CheckIfMoleculeLegal(orig_MoleculeEditorScreen_method_1132 orig, MoleculeEditorScreen editor_self)
	{

		if (!allowDisjointMolecules) {
			orig(editor_self);
			return;
		}
		
		Maybe<LocString> error_message = struct_18.field_1431;
		Molecule molecule = (Molecule)MainClass.PrivateField<MoleculeEditorScreen>("field_2656").GetValue(editor_self);
	

		if (!molecule.method_1100().ContainsKey(new HexIndex(0, 0)))
		{
			error_message = class_134.method_253("There must be an atom at the center of the board.", string.Empty);
			goto OutputValue;
		}
		if (molecule.method_1100().Values.Count(CheckIfRepetition) > 1)
		{
			error_message = class_134.method_253("Only one repetition placeholder may be used.", string.Empty);
			goto OutputValue;
		}
		Maybe<HexIndex> maybe = molecule.method_1100().Where(CheckRepetitionLocation).Select(GetHexIndexKey)
			.method_430();
		if (maybe.method_1085() && (maybe.method_1087().Q <= 0 || maybe.method_1087().R != 0))
		{
			error_message = class_134.method_253("If used, the repetition placeholder must be to the right of the center atom.", string.Empty);
			goto OutputValue;
		}
		HashSet<HexIndex> hashSet = new HashSet<HexIndex>();
		Queue<HexIndex> queue = new Queue<HexIndex>();
		queue.Enqueue(new HexIndex(0, 0));
		while (queue.Count > 0)
		{
			MoleculeEditorScreen.class_354 class_357 = new MoleculeEditorScreen.class_354();
			class_357.field_2662 = queue.Dequeue();
			hashSet.Add(class_357.field_2662);
			HexIndex[] adjacentOffsets = HexIndex.AdjacentOffsets;
			foreach (HexIndex hexIndex in adjacentOffsets)
			{
				MoleculeEditorScreen.class_355 class_358 = new MoleculeEditorScreen.class_355();
				class_358.field_2664 = class_357;
				class_358.field_2663 = class_358.field_2664.field_2662 + hexIndex;

				MethodInfo method_1135 = MainClass.PrivateMethod<MoleculeEditorScreen.class_355>("method_1135");

				// i'm not sure how to acccess method_1135 for the purposes of this, but technically i don't actually need to! since the only time this code will run will be if disjoint molecules are allowed,
				// i can just sort of... ignore it, for now. it is something that absolutely needs to be fixed if this was expanded to disable more warnings, but i don't think any of the other warnings actually
				// benefit from being disabled yet. 

				//if (!hashSet.Contains(class_358.field_2663) && !queue.Contains(class_358.field_2663) && molecule.method_1101().Any((Func<class_277, bool>)method_1135.CreateDelegate(typeof(Func<class_277, bool>))))
				//{
				//	queue.Enqueue(class_358.field_2663);
				//}
			}
		}
		if (hashSet.Count != molecule.method_1100().Count && !allowDisjointMolecules)
		{
			error_message = class_134.method_253("All atoms must be connected.", string.Empty);
			goto OutputValue;
		}
		using (molecule.method_1101().GetEnumerator())
		{
		}
		if (!maybe.method_1085())
		{
			goto OutputValue;
		}
		int q = maybe.method_1087().Q;
		foreach (KeyValuePair<HexIndex, Atom> item in molecule.method_1100())
		{
			if (item.Value.field_2275 == class_175.field_1689)
			{
				continue;
			}
			for (int j = -2; j <= 2; j++)
			{
				if (j != 0)
				{
					HexIndex key = item.Key + new HexIndex(j * q, 0);
					if (molecule.method_1100().TryGetValue(key, out var value) && value.field_2275 != class_175.field_1689)
					{
						error_message = class_134.method_253("Repeating this pattern to the right would cause atoms to overlap.", string.Empty);
						goto OutputValue;
					}
				}
			}
		}

		OutputValue:
		MainClass.PrivateField<MoleculeEditorScreen>("field_2661").SetValue(editor_self, error_message);
		return;
	
	}
}
