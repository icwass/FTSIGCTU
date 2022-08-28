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
using Texture = class_256;

public static class MirrorTool
{
	//data structs, enums, variables
	private static Sound[] sounds;

	private static Dictionary<PartType, mirrorRule> mirrorRules;

	private enum resource : byte
	{
		success,
		failure,
		COUNT,
	}

	public class mirrorRule
	{
		public Func<Part, int, Part> rule;
		public Func<Part, bool> validator;
		public mirrorRule(Func<Part, int, Part> rule, Func<Part, bool> validator)
		{
			this.rule = rule;
			this.validator = validator;
		}
		public mirrorRule(Func<Part, int, Part> rule)
		{
			this.rule = rule;
			this.validator = x => true;
		}
		public mirrorRule()
		{
			this.rule = (x,y) => {
				var ret = common.clonePart(x);
				var hex = mirrorHexAcrossRow(common.getPartOrigin(ret),y);
				common.setPartOrigin(ret, hex);
				return ret;
			};
			this.validator = x => true;
		}

	}

	//---------------------------------------------------//
	//public APIs


	/// <summary>
	/// Returns the input hex after mirroring it across the given horizontal axis.
	/// </summary>
	/// <param name="hex">The input hex.</param>
	/// <param name="y">The coordinate of the horizontal axis to mirror across.</param>
	/// <returns></returns>
	public static HexIndex mirrorHexAcrossRow(HexIndex hex, int y)
	{
		int k = hex.R - y;
		return new HexIndex(hex.Q + k, y - k);
	}

	/// <summary>
	/// Returns the input rotation after mirroring it across a horizontal axis.
	/// </summary>
	/// <param name="hex">The input hex.</param>
	/// <param name="y">The coordinate of the horizontal axis to mirror across.</param>
	/// <returns></returns>
	public static HexRotation mirrorRotationVertically(HexRotation rot)
	{
		return rot.Negative();
	}

	/// <summary>
	/// A mirror rule that mirrors the origin hex, mirrors the rotation, mirrors the instruction tape and mirrors any track hexes. For parts that can always be mirrored.
	/// </summary>
	public static mirrorRule basicMirrorRule = new mirrorRule(
		(part, y) =>
		{
			var ret = common.clonePart(part);
			var retDyn = new DynamicData(ret);
			//mirror origin and rotation
			retDyn.Set("field_2693", mirrorRotationVertically(ret.method_1163()));
			retDyn.Set("field_2692", mirrorHexAcrossRow(common.getPartOrigin(ret), y));

			//mirror instruction tape

			void MirrorTape(EditableProgram tape)
			{
				var tape_dyn = new DynamicData(tape);
				var sortedDict = tape_dyn.Get<SortedDictionary<int, InstructionType>>("field_2415");
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
				tape_dyn.Set("field_2415", newDict);
			}
			MirrorTape(part.field_2697);
			MirrorTape(ret.field_2697);

			//mirror track hexes, if they exist
			if (common.getPartType(ret) == common.MechanismTrack())
			{
				var track = common.getTrackList(ret);
				for (int i = 0; i < track.Count; i++)
				{
					track[i] = mirrorHexAcrossRow(track[i], y);
				}
				common.setTrackList(ret, track);
			}

			return ret;
		}
	);

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

	//---------------------------------------------------//
	//internal helper methods
	//private static bool PartIsTrack(Part part) => part.method_1159() == class_191.field_1770;
	private static PartType getDraggedPartType(PartDraggingInputMode.DraggedPart draggedPart)
	{
		return common.getPartType(draggedPart.field_2722);
	}

	//---------------------------------------------------//
	//internal main methods


	//---------------------------------------------------//
	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		var current_interface = SES_self.field_4010;
		bool inDraggingMode = current_interface.GetType() == (new PartDraggingInputMode()).GetType();

		if (inDraggingMode && Input.IsRightClickPressed())
		{
			var interfaceDyn = new DynamicData(current_interface);
			var draggedParts = interfaceDyn.Get<List<PartDraggingInputMode.DraggedPart>>("field_2712");
			if (draggedParts.Any(x => !mirrorRules.Keys.Contains(getDraggedPartType(x)) && true))
			{
				common.playSound(sounds[(int)resource.failure], 0.2f);
				return;
			}
			//otherwise the selection can be mirrored
			var mirroredDraggedParts = new List<PartDraggingInputMode.DraggedPart>();
			foreach (var draggedPart in draggedParts)
			{
				PartType draggedPartType = getDraggedPartType(draggedPart);
				var mirrorDraggedPart = new PartDraggingInputMode.DraggedPart()
				{
					field_2722 = mirrorRules[draggedPartType].rule(draggedPart.field_2722, interfaceDyn.Get<HexIndex>("field_2715").R),
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

		addRule(common.GlyphEquilibrium(), new mirrorRule());
		addRule(common.GlyphCalcification(),new mirrorRule());
		addRule(common.GlyphDisposal(), new mirrorRule());
		addRule(common.GlyphMultiBonder(), new mirrorRule());

		addRule(common.MechanismArm1(), basicMirrorRule);
		addRule(common.MechanismArm2(), basicMirrorRule);
		addRule(common.MechanismArm3(), basicMirrorRule);
		addRule(common.MechanismArm4(), basicMirrorRule);
		addRule(common.MechanismPiston(), basicMirrorRule);
		addRule(common.MechanismTrack(), basicMirrorRule);
		addRule(common.MechanismBerlo(), basicMirrorRule);
		addRule(common.GlyphBonder(), basicMirrorRule);
		addRule(common.GlyphUnbonder(), basicMirrorRule);
		addRule(common.GlyphDuplication(), basicMirrorRule);
		addRule(common.GlyphProjection(), basicMirrorRule);

		//addRule(common.IOInput(), new mirrorRule());
		//addRule(common.IOOutputStandard(), new mirrorRule());
		//addRule(common.IOOutputInfinite(), new mirrorRule());
		//addRule(common.IOConduit(), new mirrorRule());
		//addRule(common.MechanismGripper(), new mirrorRule());
		//addRule(common.GlyphTriplexBonder(),new mirrorRule());
		//addRule(common.GlyphPurification(), new mirrorRule());
		//addRule(common.GlyphAnimismus(), new mirrorRule());
		//addRule(common.GlyphUnification(), new mirrorRule());
		//addRule(common.GlyphDispersion(), new mirrorRule());
	}
	//---------------------------------------------------//
}