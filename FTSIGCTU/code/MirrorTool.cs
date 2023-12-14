//using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Quintessential;
using SDL2;
using System;
using System.Linq;
using System.Collections.Generic;
//using System.Reflection;

namespace FTSIGCTU;
using PartType = class_139;

public static class MirrorTool
{
	//data structs, enums, variables
	private static SDL.enum_160 mirrorVerticalKey = SDL.enum_160.SDLK_q;
	private static SDL.enum_160 mirrorHorizontalKey = SDL.enum_160.SDLK_e;

	private static Dictionary<PartType, Func<SolutionEditorScreen, Part, bool, HexIndex, bool>> mirrorRules;

	private static bool bondsMatch(class_277 a, class_277 b)
	{
		//check bond type
		if (a.field_2186 != b.field_2186) return false;

		//check bond hexes
		if (a.field_2187 == b.field_2187 && a.field_2188 == b.field_2188) return true;
		if (a.field_2187 == b.field_2188 && a.field_2188 == b.field_2187) return true;

		return false;
	}

	//---------------------------------------------------//
	//public APIs and resources

	/// <summary>
	/// Adds a mirroring rule to FTSIGCTU's rulebook for the given PartType, if one doesn't already exist.
	/// </summary>
	/// <param name="partType">The PartType that the mirror rule applies to.</param>
	/// <param name="rule">A function representing the mirror rule to apply for the given PartType. See mirrorSimplePart for information on the function inputs. The bool output should indicate if the part can be mirrored.</param>
	/// <returns></returns>
	public static void addRule(PartType partType, Func<SolutionEditorScreen, Part, bool, HexIndex, bool> rule)
	{
		if (mirrorRules.Keys.Contains(partType))
		{
			Logger.Log($"FTSIGCTU.MirrorTool.addRule: partType {partType.field_1529} already has a mirror rule, ignoring.");
		}
		else
		{
			mirrorRules.Add(partType, rule);
		}
	}

	#region MirroringHelpers
	/// <summary>
	/// Returns the hex obtained by mirroring the input hex across the specified axis.
	/// <param name="inputHex">The hex to mirror.</param>
	/// <param name="mirrorVert">Set to true to mirror vertically, set to false to mirror horizontally.</param>
	/// <param name="pivotHex">A hex that defines the mirror axis.</param>
	/// </summary>
	public static HexIndex mirrorHex(HexIndex inputHex, bool mirrorVert, HexIndex pivotHex)
	{
		int q, r, s, t, x, y;
		q = inputHex.Q;
		r = inputHex.R;
		s = pivotHex.Q;
		t = pivotHex.R;

		int j = q - s;
		int k = r - t;
		if (mirrorVert)
		{
			x = q + k;
			y = t - k;
		}
		else // mirror horizontally
		{
			x = s - j - k;
			y = r;
		}
		return new HexIndex(x, y);
	}

	public static HexIndex mirrorHexAcrossRow(HexIndex hex, int y)
	{
		int k = hex.R - y;
		return new HexIndex(hex.Q + k, y - k);
	}

	/// <summary>
	/// An action that mirrors the part's origin hex.
	/// </summary>
	public static void mirrorOrigin(Part part, bool mirrorVert, HexIndex pivotHex) => new DynamicData(part).Set("field_2692", mirrorHex(common.getPartOrigin(part), mirrorVert, pivotHex));

	/// <summary>
	/// An action that shifts the part's origin hex.
	/// </summary>
	public static void shiftOrigin(Part part, HexIndex shift) => new DynamicData(part).Set("field_2692", common.getPartOrigin(part) + shift);

	/// <summary>
	/// An action that mirrors the part's rotation.
	/// </summary>
	public static void mirrorRotation(Part part, bool mirrorVert) => new DynamicData(part).Set("field_2693", (part.method_1163() + (mirrorVert ? HexRotation.R0 : HexRotation.R180)).Negative());

	/// <summary>
	/// An action that applies a rotation to the part.
	/// </summary>
	public static void shiftRotation(Part part, HexRotation rot) => new DynamicData(part).Set("field_2693", part.method_1163() + rot);

	/// <summary>
	/// An action that mirrors the Rotate and Pivot instructions of the part's instruction tape.
	/// </summary>
	public static void mirrorInstructions(Part part)
	{
		var tapeDyn = new DynamicData(part.field_2697);
		var sortedDict = tapeDyn.Get<SortedDictionary<int, InstructionType>>("field_2415");
		var newDict = new SortedDictionary<int, InstructionType>();

		InstructionType RCW = class_169.field_1657;
		InstructionType RCCW = class_169.field_1658;
		InstructionType PCW = class_169.field_1661;
		InstructionType PCCW = class_169.field_1662;
		var mirrorInstr = new Dictionary<InstructionType, InstructionType>
		{
			{RCW,	RCCW},
			{RCCW,	RCW},
			{PCW,	PCCW},
			{PCCW,	PCW},
		};
		foreach (var kvp in sortedDict)
		{
			var instruction = kvp.Value;
			if (mirrorInstr.ContainsKey(instruction))
			{
				instruction = mirrorInstr[instruction];
			}
			newDict.Add(kvp.Key, instruction);
		}
		tapeDyn.Set("field_2415", newDict);
	}

	/// <summary>
	/// Returns a dictionary of rotations and translations that transform the start footprint into the target footprint.
	/// The dictionary will be empty if no valid transformation exists.
	/// </summary>
	/// <param name="pivot">The hex to rotate around. Rotations are applied before translations.</param>
	public static Dictionary<HexRotation, HexIndex> getFootprintTransformations(List<HexIndex> startList, HexIndex pivot, List<HexIndex> targetList)
	{
		var ret = new Dictionary<HexRotation, HexIndex>();
		// if the footprints have different sizes, then no translation is possible, so return early
		if (startList.Count != targetList.Count) return ret;
		// if the footprint is null, then all translations work, so return early
		var count = targetList.Count;
		if (count == 0)
		{
			for (int i = 0; i < 6; i++)
			{
				ret.Add(new HexRotation(i), new HexIndex(0, 0));
			}
			return ret;
		}

		//otherwise, let the real work begin
		var targetSum = new HexIndex(0, 0);
		foreach (var hex in targetList)
		{
			targetSum += hex;
		}

		for (int i = 0; i < 6; i++)
		{
			var rotation = new HexRotation(i);
			List<HexIndex> rotList = new();
			foreach (var hex in startList)
			{
				rotList.Add(hex.RotatedAround(pivot, rotation));
			}

			// the question at hand: can we translate rotList into targetList?
			// if rotList can be translated into targetList,
			// it'd be by some unique vector V
			// but then we have [rotSum + count*V = targetSum]
			// and we can solve for V
			//     note: if rotList can NOT be translated into targetList,
			// this equation might work for some V anyway,
			// so we need to verify V once we find it
			var rotSum = new HexIndex(0, 0);
			foreach (var hex in rotList)
			{
				rotSum += hex;
			}

			var sum = targetSum - rotSum;
			if (sum.Q % count != 0 || sum.R % count != 0) continue;
			//else, we MIGHT have valid transformation!
			//check the translation to see if it works
			var translation = new HexIndex(sum.Q / count, sum.R / count);

			if (rotList.Any(x => !targetList.Contains(x + translation)))
			{
				//the translation doesn't work
				continue;
			}
			else
			{
				//the translation is valid!
				ret.Add(rotation, translation);
			}
		}
		return ret;
	}
	#endregion


	#region VanillaMirroringRules
	/// <summary>
	/// A simple rule that mirrors the origin hex and the rotation.
	/// Works for any part that is symmetric across the horizontal line that passes through its origin hex.
	/// </summary>
	/// <param name="ses">The current SolutionEditorScreen. Some parts, such as inputs, need to reference the SES to determine whether they can be mirrored.</param>
	/// <param name="part">The part to be modified into its mirrored version.</param>
	/// <param name="mirrorVert">True if the part should be mirrored vertically, false if horizontally.</param>
	/// <param name="pivot">The hex to mirror across, i.e. the hex that the line of symmetry passes through.</param>
	public static bool mirrorSimplePart(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot)
	{
		mirrorOrigin(part, mirrorVert, pivot);
		mirrorRotation(part, mirrorVert);
		return true;
	}

	/// <summary>
	/// An always-returns-false mirror rule, for parts that cannot be mirrored.
	/// </summary>
	public static bool mirrorInvalid(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot)
	{
		return false;
	}

	/// <summary>
	/// A simple rule that only mirrors the origin hex.
	/// Used for glyphs that can be mirrored but should not be rotated, like single-hex glyphs (e.g. Equilibrium) or the Glyph of Disposal.
	/// </summary>
	public static bool mirrorSingleton(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot)
	{
		mirrorOrigin(part, mirrorVert, pivot);
		return true;
	}

	/// <summary>
	/// Creates a mirror rule for parts with symmetry across the specified vertical line (e.g. x = 0.5 for Glyph of Purification).
	/// (Here x = 0 is the vertical line the goes through the origin hex.)
	/// </summary>
	/// <param name="x">The vertical line of symmetry, which should be an multiple of 0.5.</param>
	public static Func<SolutionEditorScreen, Part, bool, HexIndex, bool> mirrorVerticalPart(float x) =>
	(ses, part, mirrorVert, pivot) =>
	{
		mirrorSimplePart(ses, part, mirrorVert, pivot);
		var shift = new HexIndex((int)(2 * x), 0).Rotated(common.getPartRotation(part));
		shiftOrigin(part, shift);
		shiftRotation(part, HexRotation.R180);
		return true;
	};
	public static bool mirrorVerticalPart0_5(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot) => mirrorVerticalPart(0.5f)(ses, part, mirrorVert, pivot);
	public static bool mirrorVerticalPart0_0(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot) // optimized
	{
		mirrorSimplePart(ses, part, mirrorVert, pivot);
		shiftRotation(part, HexRotation.R180);
		return true;
	}

	/// <summary>
	/// Creates a mirror rule for parts with symmetry across the specified horizontal line (e.g. y = 0 for Glyph of Projection).
	/// (Here y = 0 is the horizontal line the goes through the origin hex.)
	/// </summary>
	/// <param name="y">The horizontal line of symmetry.</param>
	public static Func<SolutionEditorScreen, Part, bool, HexIndex, bool> mirrorHorizontalPart(int y) =>
	(ses, part, mirrorVert, pivot) =>
	{
		mirrorSimplePart(ses, part, mirrorVert, pivot);
		var shift = new HexIndex(y, -2*y).Rotated(common.getPartRotation(part));
		shiftOrigin(part, shift);
		return true;
	};
	public static bool mirrorHorizontalPart0_0(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot) => mirrorSimplePart(ses, part, mirrorVert, pivot); // optimized

	/// <summary>
	/// A simple mirror rule for arms.
	/// Works for any arm part that is symmetric across the horizontal line that passes through its origin hex.
	/// </summary>
	public static bool mirrorVanillaArm(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot)
	{
		mirrorSimplePart(ses, part, mirrorVert, pivot);
		mirrorInstructions(part);
		return true;
	}

	public static bool mirrorVanBerlo(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot)
	{
		mirrorVanillaArm(ses, part, mirrorVert, pivot);
		shiftRotation(part, HexRotation.R180);
		return true;
	}

	public static bool mirrorTrack(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot)
	{
		var track = common.getTrackList(part);
		for (int i = 0; i < track.Count; i++)
		{
			track[i] = mirrorHex(track[i], mirrorVert, pivot);
		}
		new DynamicData(part).Set("field_2692", track[0]);
		common.setTrackList(part, track);
		return true;
	}

	public static bool mirrorConduit(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot)
	{
		//vanilla conduits have an edge between every pair of adjacent hexes
		//so we need only check the hex footprint
		//(at least, until conduits with special edge sets become prolific)
		HexIndex origin = common.getPartOrigin(part);
		HexRotation rot = common.getPartRotation(part);
		var startFootprint = common.getConduitList(part);
		List<HexIndex> targetFootprint = new();
		foreach (var hex in startFootprint)
		{
			targetFootprint.Add(mirrorHex(hex, mirrorVert, pivot));
		}

		var transforms = getFootprintTransformations(startFootprint, origin, targetFootprint);
		bool canMirror = transforms.Count > 0;
		if (canMirror)
		{
			shiftRotation(part, transforms.First().Key);
			shiftOrigin(part, transforms.First().Value);
		}
		return canMirror;
	}

	public static bool mirrorStandardIO(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot)
	{
		//we can check the hex footprint to start,
		//but we also need to check atom types and bond types
		HexIndex origin = common.getPartOrigin(part);
		HexRotation rotation = common.getPartRotation(part);
		var solution = ses.method_502();
		//get the input/output molecule, as it exists in realspace, and extract the atom/bond data
		Molecule molecule = part.method_1185(solution).method_1115(rotation).method_1117(origin);
		var moleculeDyn = new DynamicData(molecule);
		var atomDict = moleculeDyn.Get<Dictionary<HexIndex, Atom>>("field_2642");
		var bondList = moleculeDyn.Get<List<class_277>>("field_2643");

		//first find footprint transformations - if none exist, we can end early
		List<HexIndex> startFootprint = new();
		List<HexIndex> targetFootprint = new();
		foreach (var hex in atomDict.Keys)
		{
			startFootprint.Add(hex);
			targetFootprint.Add(mirrorHex(hex, mirrorVert, pivot));
		}
		var transforms = getFootprintTransformations(startFootprint, origin, targetFootprint);

		//check the transforms
		foreach (var transform in transforms)
		{
			bool canMirror = true;
			var rot = transform.Key;
			var shift = transform.Value;

			//helper function
			HexIndex transformedMirrorHex(HexIndex hex)
			{
				var ret = mirrorHex(hex, mirrorVert, pivot);
				return (ret - shift).RotatedAround(origin,rot.Negative());
			}

			//check if the transform matches the mirror
			foreach (var pair in atomDict)
			{
				if (pair.Value.field_2275 != atomDict[transformedMirrorHex(pair.Key)].field_2275) //we know the transformed footprint matches up, so this dictionary check is always safe
				{
					canMirror = false;
					break;
				}
			}
			if (!canMirror) continue;

			//else the atoms match up - time to check bonds
			foreach (var bond in bondList)
			{
				var transformedmirroredBond = new class_277(bond.field_2186, transformedMirrorHex(bond.field_2187), transformedMirrorHex(bond.field_2188));
				bool foundBond = false;
				foreach (var comparand in bondList)
				{
					if (bondsMatch(comparand, transformedmirroredBond))
					{
						foundBond = true;
						break;
					}
				}
				if (!foundBond)
				{
					canMirror = false;
					break;
				}
			}
			if (!canMirror) continue;

			//else bonds match up - the transform works!
			shiftRotation(part, rot);
			shiftOrigin(part, shift);
			return true;
		}
		//none of the transforms will work - cannot mirror
		return false;
	}

	public static bool mirrorInfiniteOutput(SolutionEditorScreen ses, Part part, bool mirrorVert, HexIndex pivot)
	{
		//infiniteOutputs cannot be rotated, so the only transform that will work is a translation
		//so horizontal mirroring cannot happen
		if (!mirrorVert) return false;
		//i COULD write code optimized for infinities
		//but i'm lazy lmao :^) we just gonna do this:
		return mirrorStandardIO(ses, part, true, pivot);
	}
	#endregion

	//---------------------------------------------------//
	//internal main methods


	//---------------------------------------------------//
	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		var current_interface = SES_self.field_4010;
		bool inDraggingMode = current_interface.GetType() == new PartDraggingInputMode().GetType();
		bool mirrorHorz = Input.IsSdlKeyPressed(mirrorHorizontalKey);
		bool mirrorVert = Input.IsSdlKeyPressed(mirrorVerticalKey);

		// exit early if wrong mode
		if (!inDraggingMode) return;
		// exit early if not trying to mirror
		if (!mirrorHorz && !mirrorVert) return;

		// time to mirror the selection!
		var interfaceDyn = new DynamicData(current_interface);
		var draggedParts = interfaceDyn.Get<List<PartDraggingInputMode.DraggedPart>>("field_2712");
		var cursorHex = interfaceDyn.Get<HexIndex>("field_2715");

		var mirroredDraggedParts = new List<PartDraggingInputMode.DraggedPart>();

		foreach (var draggedPart in draggedParts)
		{
			var part = draggedPart.field_2722;
			var partType = common.getPartType(part);
			var clonedPart = common.clonePart(SES_self.method_502(), part);

			if (!mirrorRules.Keys.Contains(partType) || !mirrorRules[partType](SES_self, clonedPart, mirrorVert, cursorHex)) // can we mirror the part?
			{
				//could not mirror the part, so the whole operation fails and we must return early
				common.playSound(class_238.field_1991.field_1872, 0.2f);  // 'sounds/ui_modal'
				return;
			}
			//else the part was mirrored, so continue
			var mirrorDraggedPart = new PartDraggingInputMode.DraggedPart()
			{
				field_2722 = clonedPart,
				field_2723 = draggedPart.field_2723,
				field_2207 = draggedPart.field_2207,
			};
			mirroredDraggedParts.Add(mirrorDraggedPart);
		}
		interfaceDyn.Set("field_2712", mirroredDraggedParts);
		common.playSound(class_238.field_1991.field_1877, 0.2f);  // 'sounds/ui_transition_back'
	}

	public static void LoadPuzzleContent()
	{
		mirrorRules = new();
		//add vanilla mirror rules

		//mechanisms
		addRule(common.MechanismArm1(), mirrorVanillaArm);
		addRule(common.MechanismArm2(), mirrorVanillaArm);
		addRule(common.MechanismArm3(), mirrorVanillaArm);
		addRule(common.MechanismArm6(), mirrorVanillaArm);
		addRule(common.MechanismPiston(), mirrorVanillaArm);
		addRule(common.MechanismBerlo(), mirrorVanBerlo);
		addRule(common.MechanismTrack(), mirrorTrack);

		//simple glyphs
		addRule(common.GlyphEquilibrium(), mirrorSingleton);
		addRule(common.GlyphCalcification(), mirrorSingleton);
		addRule(common.GlyphDisposal(), mirrorSingleton);

		addRule(common.GlyphBonder(), mirrorSimplePart);
		addRule(common.GlyphUnbonder(), mirrorSimplePart);
		addRule(common.GlyphMultiBonder(), mirrorSimplePart);
		addRule(common.GlyphDuplication(), mirrorSimplePart);
		addRule(common.GlyphProjection(), mirrorSimplePart);
		
		//advanced glyphs
		addRule(common.GlyphTriplexBonder(), mirrorVerticalPart0_5);
		addRule(common.GlyphPurification(), mirrorVerticalPart0_5);
		addRule(common.GlyphAnimismus(), mirrorVerticalPart0_5);
		addRule(common.GlyphUnification(), mirrorSimplePart);
		addRule(common.GlyphDispersion(), mirrorVerticalPart0_0);

		//parts that may or may not be mirror-able
		addRule(common.IOConduit(), mirrorConduit);
		addRule(common.IOInput(), mirrorStandardIO);
		addRule(common.IOOutputStandard(), mirrorStandardIO);
		addRule(common.IOOutputInfinite(), mirrorInfiniteOutput);
	}
	//---------------------------------------------------//
}