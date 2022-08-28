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

public static class common
{
	//
	public static bool drawThickHexes = false;
	//---------------------------------------------------//
	public static void playSound(Sound SOUND, float VOLUME)
	{
		class_158.method_376(SOUND.field_4061, class_269.field_2109 * VOLUME, false);
	}
	//---------------------------------------------------//
	//part types
	public static PartType IOInput() => class_191.field_1760;
	public static PartType IOOutputStandard() => class_191.field_1761;
	public static PartType IOOutputInfinite() => class_191.field_1762;
	public static PartType IOConduit() => class_191.field_1763;
	public static PartType MechanismArm1() => class_191.field_1764;
	public static PartType MechanismArm2() => class_191.field_1765;
	public static PartType MechanismArm3() => class_191.field_1766;
	public static PartType MechanismArm4() => class_191.field_1767;
	public static PartType MechanismPiston() => class_191.field_1768;
	public static PartType MechanismGripper() => class_191.field_1769;
	public static PartType MechanismTrack() => class_191.field_1770;
	public static PartType MechanismBerlo() => class_191.field_1771;
	public static PartType GlyphBonder() => class_191.field_1772;
	public static PartType GlyphUnbonder() => class_191.field_1773;
	public static PartType GlyphMultiBonder() => class_191.field_1774;
	public static PartType GlyphTriplexBonder() => class_191.field_1775;
	public static PartType GlyphCalcification() => class_191.field_1776;
	public static PartType GlyphDuplication() => class_191.field_1777;
	public static PartType GlyphProjection() => class_191.field_1778;
	public static PartType GlyphPurification() => class_191.field_1779;
	public static PartType GlyphAnimismus() => class_191.field_1780;
	public static PartType GlyphDisposal() => class_191.field_1781;
	public static PartType GlyphEquilibrium() => class_191.field_1782;
	public static PartType GlyphUnification() => class_191.field_1783;
	public static PartType GlyphDispersion() => class_191.field_1784;

	//---------------------------------------------------//
	//drawing helpers
	public static HexIndex getHexFromPoint(Vector2 point, SolutionEditorScreen SES)
	{
		return class_187.field_1742.method_493(point, SES.field_4009);
	}
	public static Vector2 getPointFromHex(HexIndex hex, SolutionEditorScreen SES)
	{
		return class_187.field_1742.method_491(hex, SES.field_4009);
	}
	//---------------------------------------------------//
	//input helpers
	public static bool withinBoardEditingBounds(Vector2 point, SolutionEditorScreen SES)
	{
		var SES_dyn = new DynamicData(SES);
		var sepp = SES_dyn.Get<SolutionEditorPartsPanel>("field_4001");
		Vector2 bottomLeft = new Vector2(310f, 268f) + new Vector2(class_162.method_417(0.0f, -360f, new DynamicData(sepp).Get<float>("field_3966")), 0.0f);
		Vector2 resolution = class_115.field_1433;
		Bounds2 fullBounds = Bounds2.WithCorners(bottomLeft, resolution);
		Bounds2 exitBounds = Bounds2.WithSize(resolution + new Vector2(-70f, -75f), new Vector2(70f, 75f));

		return fullBounds.Contains(point) && !exitBounds.Contains(point);
	}
	//---------------------------------------------------//
	//part-editing helpers
	public static HexIndex getPartOrigin(Part part) => part.method_1161();
	public static void setPartOrigin(Part part, HexIndex hex) => new DynamicData(part).Set("field_2692", hex); //easier than trying to invoke method_1162, which is private
	public static HexRotation getPartRotation(Part part) => part.method_1163();
	public static PartType getPartType(Part part) => part.method_1159();
	public static List<HexIndex> getTrackList(Part part)
	{
		var part_dyn = new DynamicData(part);
		var trackList = part_dyn.Get<List<HexIndex>>("field_2700");
		var origin = common.getPartOrigin(part);
		var list = new List<HexIndex>();
		foreach (HexIndex hex in trackList)
		{
			list.Add(hex + origin);
		}
		return list;
	}
	public static void setTrackList(Part part, List<HexIndex> list)
	{
		var origin = common.getPartOrigin(part);
		var trackList = new List<HexIndex>();
		foreach (HexIndex hex in list)
		{
			trackList.Add(hex - origin);
		}
		new DynamicData(part).Set("field_2700", trackList);
	}

	public static Part clonePart(Part orig)
	{
		//based off method_1175
		class_162.method_403(!orig.method_1171(), "Fixed parts cannot be cloned.");
		Part part = new Part(orig.method_1159(), false);
		var partDyn = new DynamicData(part);
		partDyn.Set("field_2692", orig.method_1161());
		partDyn.Set("field_2693", orig.method_1163());
		partDyn.Set("field_2694", orig.method_1165());
		part.field_2695 = (Maybe<Part>)struct_18.field_1431;
		part.field_2697 = orig.field_2697.method_897();
		partDyn.Set("field_2698", orig.method_1167());
		partDyn.Set("field_2699", orig.method_1169());
		part.field_2702 = orig.field_2702;
		if (orig.method_1159().field_1542)
		{
			part.method_1194();
			foreach (HexIndex hexIndex in new DynamicData(orig).Get<List<HexIndex>>("field_2700"))
				part.method_1192(hexIndex);
		}
		if (orig.method_1159().field_1543)
		{
			part.field_2703 = orig.field_2703;
			part.method_1204(new List<HexIndex>(orig.method_1173()));
		}
		//part.method_1200();
		{
			for (int index = 0; index < part.field_2696.Length; ++index)
			{
				var subPart = part.field_2696[index];
				new DynamicData(subPart).Set("field_2692", Sim.class_284.method_230(part, index));
			}
		}
		return part;
	}

	public static void addUndoHistoryCheckpoint(SolutionEditorScreen SES_self)
	{
		SES_self.method_502().method_1961();
	}
	//---------------------------------------------------//
	public static void ApplySettings(bool _drawThickHexes)
	{
		drawThickHexes = _drawThickHexes;
	}
}