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
using Texture = class_256;

public static class common
{
	//
	public static bool drawThickHexes = false;
	//---------------------------------------------------//
	public static void playSound(Sound SOUND, float VOLUME = 1f)
	{
		//class_158.method_376(SOUND.field_4061, class_269.field_2109 * VOLUME, false);
		SOUND.method_28(VOLUME);
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
	public static PartType MechanismArm6() => class_191.field_1767;
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
	//debug helpers

	private static string atomtypeToChar(AtomType type)
	{
		string[] table = new string[17] { "…", "🜔", "🜁", "🜃", "🜂", "🜄", "☿", "☉", "☽", "♀", "♂", "♃", "♄", "🜍", "🜞", "…", "✶" };
		if (type.field_2283 < 17 && type.field_2283 >= 0) return table[type.field_2283];
		Logger.Log("printMolecule: Unknown atom type '" + type.field_2284 + "' (byteID: " + type.field_2283 + ")");
		return "?";
	}
	private static char bondToChar(enum_126 type, Pair<int, int> location)
	{
		int index = (location.Right % 2 == 0) ? 0 : ((location.Right - location.Left) % 4 == 0) ? 1 : 2;
		string table = "—\\/~}{";
		if (type == (enum_126)0) {
			return ' ';
		} else if (type == (enum_126)1) {
			return table[index];
		} else if (((int) type & 14) == (int)type) {
			return table[index+3];
		}
		Logger.Log("printMolecule: Unknown bond type '" + type + "'");
		return '#';
	}
	private static Pair<int, int> hexToPair(HexIndex hex) => new Pair<int, int>(4 * hex.Q + 2* hex.R, -2*hex.R);
	private static Pair<int, int> bondToPair(class_277 bond)
	{
		//assumes bonds are between adjacent atoms only
		var pair1 = hexToPair(bond.field_2187);
		var pair2 = hexToPair(bond.field_2188);
		return new Pair<int, int>((pair1.Left + pair2.Left) / 2, (pair1.Right + pair2.Right) / 2);
	}
	public static void printMolecule(Molecule molecule)
	{
		var moleculeDyn = new DynamicData(molecule);
		var atomDict = moleculeDyn.Get<Dictionary<HexIndex, Atom>>("field_2642");
		if (atomDict.Count == 0)
		{
			Logger.Log("<empty molecule>");
			return;
		}
		var bondList = moleculeDyn.Get<List<class_277>>("field_2643");
		int minX = int.MaxValue;
		int minY = int.MaxValue;
		int maxX = int.MinValue;
		int maxY = int.MinValue;
		Dictionary<Pair<int,int>, string> charDict = new();
		foreach (var pair in atomDict)
		{
			Pair<int, int> location = hexToPair(pair.Key);
			charDict.Add(location, atomtypeToChar(pair.Value.field_2275));
			minX = Math.Min(minX, location.Left);
			maxX = Math.Max(maxX, location.Left+4);
			minY = Math.Min(minY, location.Right-1);
			maxY = Math.Max(maxY, location.Right+2);
		}
		foreach (var bond in bondList)
		{
			Pair<int, int> location = bondToPair(bond);
			charDict.Add(location, "" + bondToChar(bond.field_2186,location));
		}
		string[,] vram = new string[maxX - minX, maxY - minY];
		for (int i = 0; i < maxX - minX; i++)
		{
			for (int j = 0; j < maxY - minY; j++)
			{
				vram[i, j] = (i % 4 == 3) ? "	" : " ";
			}
		}
		foreach (var pair in charDict)
		{
			vram[pair.Key.Left - minX, pair.Key.Right - minY] = pair.Value;
		}
		for (int j = 0; j < maxY - minY; j++)
		{
			string str = "";
			for (int i = 0; i < maxX - minX; i++)
			{
				str = str + vram[i, j];
			}
			if (j == 0)
			{
				Logger.Log("/	" + str + "\\");
			}
			else if (j == maxY - minY - 1)
			{
				Logger.Log("\\	" + str + "/");
			}
			else
			{
				Logger.Log("|	" + str + "|");
			}
		}
	}

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
	public static Vector2 textureDimensions(Texture texture) => texture.field_2056.ToVector2();
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

	public static List<HexIndex> getConduitList(Part part)
	{
		var conduitList = part.method_1173();
		var origin = common.getPartOrigin(part);
		var rot = common.getPartRotation(part);
		var list = new List<HexIndex>();
		foreach (HexIndex hex in conduitList)
		{
			list.Add(hex.Rotated(rot) + origin);
		}
		return list;
	}
	public static int getPartIndex(Part part)
	{
		return part.method_1167();
	}
	public static void setPartIndex(Part part, int index)
	{
		var dynPart = new DynamicData(part);
		dynPart.Set("field_2698", index);
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
		SES_self.method_2108();
	}
	//---------------------------------------------------//
	public static void ApplySettings(bool _drawThickHexes)
	{
		drawThickHexes = _drawThickHexes;
	}
}