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
	public static bool ignoreVanillaMoleculeWarnings = false;
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
        orig(editor_self);

        if (!ignoreVanillaMoleculeWarnings)
        {
            // nothing to do, so we're done already
            return;
        }

        Maybe<LocString> maybeError = (Maybe<LocString>) MainClass.PrivateField<MoleculeEditorScreen>("field_2661").GetValue(editor_self);
        if (!maybeError.method_1085())
        {
            // no error, no problem!
            return;
        }

        LocString errorMessage = maybeError.method_1087();

        if (maybeError == class_134.method_253("There must be an atom at the center of the board.", string.Empty)
            || maybeError == class_134.method_253("Only one repetition placeholder may be used.", string.Empty)
            || maybeError == class_134.method_253("If used, the repetition placeholder must be to the right of the center atom.", string.Empty)
            || maybeError == class_134.method_253("All atoms must be connected.", string.Empty)
            || maybeError == class_134.method_253("Triplex bonds may only be created between fire atoms.", string.Empty)
            || maybeError == class_134.method_253("Repeating this pattern to the right would cause atoms to overlap.", string.Empty)
            )
        {
            // ignore the vanilla error
            MainClass.PrivateField<MoleculeEditorScreen>("field_2661").SetValue(editor_self, (Maybe<LocString>) struct_18.field_1431);
        }
    }
}
