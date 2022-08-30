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
	private static Sound[] sounds;
	private static SDL.enum_160 mirrorVerticalKey = SDL.enum_160.SDLK_g;
	private static SDL.enum_160 mirrorHorizontalKey = SDL.enum_160.SDLK_h;

	private static Dictionary<PartType, mirrorRule> mirrorRules;

	private enum resource : byte
	{
		success,
		failure,
		COUNT,
	}

	public class mirrorRule
	{
		public Action<Part, int> rule;
		public Func<Part, bool> validator;
		public mirrorRule(Action<Part, int> rule, Func<Part, bool> validator)
		{
			this.rule = rule;
			this.validator = validator;
		}
		public mirrorRule(Action<Part, int> rule)
		{
			this.rule = rule;
			this.validator = x => true;
		}
		public mirrorRule()
		{
			this.rule = (part, y) => {
				mirrorOrigin(part, y);
			};
			this.validator = x => true;
		}

	}

	//---------------------------------------------------//
	//public APIs and resources

	/// <summary>
	/// Adds a mirroring rule to FTSIGCTU's rulebook for the given PartType, if one doesn't already exist.
	/// </summary>
	/// <param name="partType">The PartType that the mirror rule applies to.</param>
	/// <param name="rule">The mirror rule to use for the given PartType.</param>
	/// <returns></returns>
	public static void addRule(PartType partType, mirrorRule rule)
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

	/// <summary>
	/// Returns the input hex after mirroring it across the given horizontal axis.
	/// </summary>
	public static HexIndex mirrorHexAcrossRow(HexIndex hex, int y)
	{
		int k = hex.R - y;
		return new HexIndex(hex.Q + k, y - k);
	}

	/// <summary>
	/// An action that mirrors the part's origin hex.
	/// </summary>
	public static void mirrorOrigin(Part part, int y) => new DynamicData(part).Set("field_2692", mirrorHexAcrossRow(common.getPartOrigin(part), y));

	/// <summary>
	/// An action that shifts the part's origin hex.
	/// </summary>
	public static void shiftOrigin(Part part, HexIndex shift) => new DynamicData(part).Set("field_2692", common.getPartOrigin(part) + shift);

	/// <summary>
	/// An action that mirrors the part's rotation.
	/// </summary>
	public static void mirrorRotation(Part part) => new DynamicData(part).Set("field_2693", part.method_1163().Negative());

	/// <summary>
	/// An action that applies a rotation to the part.
	/// </summary>
	public static void shiftRotation(Part part, HexRotation rot) => new DynamicData(part).Set("field_2693", part.method_1163() + rot);

	/// <summary>
	/// An action that mirrors the Rotate and Pivot instructions of the part's instruction tape.
	/// </summary>
	public static void mirrorInstructions(Part part, int y)
	{
		var tapeDyn = new DynamicData(part.field_2697);
		var sortedDict = tapeDyn.Get<SortedDictionary<int, InstructionType>>("field_2415");
		var newDict = new SortedDictionary<int, InstructionType>();
		InstructionType InstructionRCW = class_169.field_1657;
		InstructionType InstructionRCCW = class_169.field_1658;
		InstructionType InstructionPCW = class_169.field_1661;
		InstructionType InstructionPCCW = class_169.field_1662;
		foreach (var kvp in sortedDict)
		{
			var instruction = kvp.Value;
			if (instruction == InstructionRCW)
			{
				instruction = InstructionRCCW;
			}
			else if (instruction == InstructionRCCW)
			{
				instruction = InstructionRCW;
			}
			else if (instruction == InstructionPCW)
			{
				instruction = InstructionPCCW;
			}
			else if (instruction == InstructionPCCW)
			{
				instruction = InstructionPCW;
			}
			newDict.Add(kvp.Key, instruction);
		}
		tapeDyn.Set("field_2415", newDict);
	}

	/// <summary>
	/// A simple mirror rule that mirrors the origin hex and the rotation.
	/// </summary>
	public static mirrorRule mirrorRuleSimple = new mirrorRule(
		(part, y) =>
		{
			mirrorOrigin(part, y);
			mirrorRotation(part);
		}
	);

	/// <summary>
	/// Like mirrorRuleSimple, but the mirrored part gets an extra 180-degree rotation (needed for e.g. Glyph of Dispersion).
	/// </summary>
	public static mirrorRule mirrorRuleSimple180 = new mirrorRule(
		(part, y) =>
		{
			mirrorOrigin(part, y);
			mirrorRotation(part);
			shiftRotation(part, HexRotation.R180);
		}
	);

	/// <summary>
	/// Creates a mirror rule for parts with vertical symmetry across the specified line (e.g. x = 0.5 for Glyph of Purification).
	/// </summary>
	/// <param name="x">The vertical line of symmetry, which should be an multiple of 0.5.</param>
	public static mirrorRule mirrorRuleVerticalSymmetry(float x) => new mirrorRule(
		(part, y) =>
		{
			mirrorOrigin(part, y);
			mirrorRotation(part);
			var shift = new HexIndex((int) (2 * x), 0).Rotated(common.getPartRotation(part));
			shiftRotation(part, HexRotation.R180);
			shiftOrigin(part, shift);
		}
	);
	public static mirrorRule mirrorRuleVerticalSymmetry0_5 = mirrorRuleVerticalSymmetry(0.5f);

	/// <summary>
	/// The mirror rule for arms.
	/// </summary>
	public static mirrorRule mirrorRuleArm = new mirrorRule(
		(part, y) =>
		{
			mirrorOrigin(part, y);
			mirrorRotation(part);
			mirrorInstructions(part, y);
		}
	);

	/// <summary>
	/// The mirror rule for the Berlo arm. Like a normal arm, but with a different rotation rule.
	/// </summary>
	public static mirrorRule mirrorRuleBerlo = new mirrorRule(
		(part, y) =>
		{
			mirrorOrigin(part, y);
			mirrorRotation(part);
			shiftRotation(part, HexRotation.R180);
			mirrorInstructions(part, y);
		}
	);

	/// <summary>
	/// The mirror rule for track.
	/// </summary>
	public static mirrorRule mirrorRuleTrack = new mirrorRule(
		(part, y) =>
		{
			var track = common.getTrackList(part);
			for (int i = 0; i < track.Count; i++)
			{
				track[i] = mirrorHexAcrossRow(track[i], y);
			}
			var partDyn = new DynamicData(part);
			partDyn.Set("field_2692", track[0]);
			common.setTrackList(part, track);
		}
	);

	//---------------------------------------------------//
	//internal helper methods
	//private static bool PartIsTrack(Part part) => part.method_1159() == class_191.field_1770;
	private static PartType getDraggedPartType(PartDraggingInputMode.DraggedPart draggedPart) => common.getPartType(draggedPart.field_2722);

	//---------------------------------------------------//
	//internal main methods


	//---------------------------------------------------//
	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		var current_interface = SES_self.field_4010;
		bool inDraggingMode = current_interface.GetType() == (new PartDraggingInputMode()).GetType();
		bool mirrorHorz = Input.IsSdlKeyPressed(mirrorHorizontalKey);
		bool mirrorVert = Input.IsSdlKeyPressed(mirrorVerticalKey);

		if (inDraggingMode && (mirrorHorz || mirrorVert))
		{
			var interfaceDyn = new DynamicData(current_interface);
			var draggedParts = interfaceDyn.Get<List<PartDraggingInputMode.DraggedPart>>("field_2712");
			var cursorHex = interfaceDyn.Get<HexIndex>("field_2715");

			bool horizontalVersusInfinite = mirrorHorz && draggedParts.Any(x => getDraggedPartType(x) == common.IOOutputInfinite());
			bool somePartCannotBeMirrored = draggedParts.Any(x => !mirrorRules.Keys.Contains(getDraggedPartType(x)) || !mirrorRules[getDraggedPartType(x)].validator(x.field_2722));
			if (horizontalVersusInfinite || somePartCannotBeMirrored)
			{
				common.playSound(sounds[(int)resource.failure], 0.2f);
				return;
			}
			//otherwise the selection can be mirrored
			var mirroredDraggedParts = new List<PartDraggingInputMode.DraggedPart>();
			foreach (var draggedPart in draggedParts)
			{
				PartType draggedPartType = getDraggedPartType(draggedPart);
				var part = draggedPart.field_2722;
				var clonedPart = common.clonePart(part);
				mirrorRules[draggedPartType].rule(clonedPart, cursorHex.R);
				if (mirrorHorz)
				{
					//internal loop of method_1215
					clonedPart.method_1195(clonedPart.method_1161().RotatedAround(cursorHex, HexRotation.R180));
					clonedPart.method_1197(SES_self.method_502(), HexRotation.R180);
				}
				var mirrorDraggedPart = new PartDraggingInputMode.DraggedPart()
				{
					field_2722 = clonedPart,
					field_2723 = draggedPart.field_2723,
					field_2207 = draggedPart.field_2207,
				};
				mirroredDraggedParts.Add(mirrorDraggedPart);
			}

			interfaceDyn.Set("field_2712", mirroredDraggedParts);
			common.playSound(sounds[(int)resource.success], 0.2f);
		}
	}
	public static void LoadPuzzleContent()
	{
		//load textures and sounds
		sounds = new Sound[(int)resource.COUNT];

		sounds[(int)resource.failure] = class_238.field_1991.field_1872;  // 'sounds/ui_modal'
		sounds[(int)resource.success] = class_238.field_1991.field_1877;  // 'sounds/ui_transition_back'

		//load mirror rules
		mirrorRules = new();

		//parts that have no rotation
		addRule(common.GlyphEquilibrium(), new mirrorRule());
		addRule(common.GlyphCalcification(), new mirrorRule());
		addRule(common.GlyphDisposal(), new mirrorRule());

		//mechanisms
		//(no rule needed for common.MechanismGripper())
		addRule(common.MechanismArm1(), mirrorRuleArm);
		addRule(common.MechanismArm2(), mirrorRuleArm);
		addRule(common.MechanismArm3(), mirrorRuleArm);
		addRule(common.MechanismArm4(), mirrorRuleArm);
		addRule(common.MechanismPiston(), mirrorRuleArm);
		addRule(common.MechanismBerlo(), mirrorRuleBerlo);
		addRule(common.MechanismTrack(), mirrorRuleTrack);

		//simple glyphs
		addRule(common.GlyphBonder(), mirrorRuleSimple);
		addRule(common.GlyphUnbonder(), mirrorRuleSimple);
		addRule(common.GlyphMultiBonder(), mirrorRuleSimple);
		addRule(common.GlyphDuplication(), mirrorRuleSimple);
		addRule(common.GlyphProjection(), mirrorRuleSimple);

		addRule(common.GlyphTriplexBonder(), mirrorRuleVerticalSymmetry0_5);
		addRule(common.GlyphPurification(), mirrorRuleVerticalSymmetry0_5);
		addRule(common.GlyphAnimismus(), mirrorRuleVerticalSymmetry0_5);
		addRule(common.GlyphUnification(), mirrorRuleSimple);
		addRule(common.GlyphDispersion(), mirrorRuleSimple180);

		//parts that may or may not be mirror-able

		//addRule(common.IOConduit(), new mirrorRule());

		//addRule(common.IOInput(), new mirrorRule());
		//addRule(common.IOOutputStandard(), new mirrorRule());
		//addRule(common.IOOutputInfinite(), new mirrorRule());
	}
	//---------------------------------------------------//
}