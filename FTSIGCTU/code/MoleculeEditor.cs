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
	//---------------------------------------------------//
	//public methods
	public static void LoadPuzzleContent()
	{
		hook_MoleculeEditor_ErrorMessage = new Hook(PrivateMethod<MoleculeEditorScreen>("method_1132"),CheckIfMoleculeLegal);
	}

	//---------------------------------------------------//
	//internal main methods

	private static void CheckIfMoleculeLegal(orig_MoleculeEditorScreen_method_1132 orig, MoleculeEditorScreen editor_self)
	{

    if (!allowDisjointMolecules) {
      orig(editor_self)
      return;
    }
		field_2661 = struct_18.field_1431;
		if (!field_2656.method_1100().ContainsKey(new HexIndex(0, 0)))
		{
			field_2661 = class_134.method_253("There must be an atom at the center of the board.", string.Empty);
			return;
		}
		if (field_2656.method_1100().Values.Where(class_301.field_2343.method_1137).Count() > 1)
		{
			field_2661 = class_134.method_253("Only one repetition placeholder may be used.", string.Empty);
			return;
		}
		Maybe<HexIndex> maybe = field_2656.method_1100().Where(class_301.field_2343.method_1138).Select(class_301.field_2343.method_1139)
			.method_430();
		if (maybe.method_1085() && (maybe.method_1087().Q <= 0 || maybe.method_1087().R != 0))
		{
			field_2661 = class_134.method_253("If used, the repetition placeholder must be to the right of the center atom.", string.Empty);
			return;
		}
		HashSet<HexIndex> hashSet = new HashSet<HexIndex>();
		Queue<HexIndex> queue = new Queue<HexIndex>();
		queue.Enqueue(new HexIndex(0, 0));
		while (queue.Count > 0)
		{
			class_354 class_357 = new class_354();
			class_357.field_2662 = queue.Dequeue();
			hashSet.Add(class_357.field_2662);
			HexIndex[] adjacentOffsets = HexIndex.AdjacentOffsets;
			foreach (HexIndex hexIndex in adjacentOffsets)
			{
				class_355 class_358 = new class_355();
				class_358.field_2664 = class_357;
				class_358.field_2663 = class_358.field_2664.field_2662 + hexIndex;
				if (!hashSet.Contains(class_358.field_2663) && !queue.Contains(class_358.field_2663) && field_2656.method_1101().Any(class_358.method_1135))
				{
					queue.Enqueue(class_358.field_2663);
				}
			}
		}
		if (hashSet.Count != field_2656.method_1100().Count && !allowDisjointMolecules)
		{
			field_2661 = class_134.method_253("All atoms must be connected.", string.Empty);
			return;
		}
		using (field_2656.method_1101().GetEnumerator())
		{
		}
		if (!maybe.method_1085())
		{
			return;
		}
		int q = maybe.method_1087().Q;
		foreach (KeyValuePair<HexIndex, Atom> item in field_2656.method_1100())
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
					if (field_2656.method_1100().TryGetValue(key, out var value) && value.field_2275 != class_175.field_1689)
					{
						field_2661 = class_134.method_253("Repeating this pattern to the right would cause atoms to overlap.", string.Empty);
						return;
					}
				}
			}
		}
	}
}
