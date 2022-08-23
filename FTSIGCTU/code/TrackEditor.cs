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

public static class TrackEditor
{
	//data structs, enums, variables
	private static bool allowQuantumTracking = false;
	private static bool alsoReverseArms = true;

	private static editingModeType editingMode = editingModeType.none;
	private static HexIndex editHex1 = new HexIndex(0, 0);
	private static HexIndex editHex2 = new HexIndex(0, 0);
	private static readonly HexIndex[] adjacentOffsets = new HexIndex[6] {
		new HexIndex(1, 0),
		new HexIndex(0, 1),
		new HexIndex(-1, 1),
		new HexIndex(-1, 0),
		new HexIndex(0, -1),
		new HexIndex(-1, -1)
	};

	private static Texture[] textures;
	private static Sound[] sounds;

	private enum resource
	{
		merge,
		split,
		reverse,
		disjoin,
		pulse,
		COUNT,
	}

	public enum editingModeType : byte
	{
		none,
		shift_G,
		G,
	}

	//---------------------------------------------------//
	//internal methods
	private static bool PartIsTrack(Part part) => part.method_1159() == class_191.field_1770;

	private static List<HexIndex> getTrackList(Part part)
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
	private static void setTrackList(Part part, List<HexIndex> list)
	{
		var origin = common.getPartOrigin(part);
		var trackList = new List<HexIndex>();
		foreach (HexIndex hex in list)
		{
			trackList.Add(hex - origin);
		}
		new DynamicData(part).Set("field_2700", trackList);
	}
	private static bool isTrackAtHex(HexIndex HEX, Solution SOLUTION)
	{
		var partList = SOLUTION.field_3919;
		foreach (Part part in partList.Where(x => PartIsTrack(x)))
		{
			foreach (HexIndex hex in getTrackList(part)) if (HEX == hex) return true;
		}
		return false;
	}
	private static List<Part> filterTrackFromSolution(Solution SOLUTION, Func<Part, bool> filter)
	{
		//return a list of all tracks that satisfy the filter
		var partList = SOLUTION.field_3919;
		List<Part> list = new List<Part>();
		foreach (Part part in partList.Where(x => PartIsTrack(x) && filter(x)))
		{
			list.Add(part);
		}
		return list;
	}
	//-----//
	private static bool mergeTracks(HexIndex HEX1, HexIndex HEX2, Solution SOLUTION)
	{
		//returns true if we need to save partState to undo/redo history because the parts list was modified
		List<Part> trackParts = filterTrackFromSolution(SOLUTION, x => {
			var list = getTrackList(x);
			if (list.Contains(HEX1) && list.Contains(HEX2))
			{
				//don't include tracks that have both hexes
				return false;
			}
			return list[0] == HEX1 || list[0] == HEX2 || list[list.Count() - 1] == HEX1 || list[list.Count() - 1] == HEX2;
		});
		if (trackParts.Count() == 0) return false;

		//sort the track into groups based on whether they contain HEX1 or HEX2,
		//then whether that hex is the first, the last, or both (which can happen with self-intersecting track or length-1 track)
		//each track will belong to exactly ONE these groups, due to the filter criteria we used to get trackParts
		var pos1 = new List<Part>();
		var neg1 = new List<Part>();
		var pos2 = new List<Part>();
		var neg2 = new List<Part>();
		var both1 = new List<Part>();
		var both2 = new List<Part>();

		foreach (Part part in trackParts)
		{
			var list = getTrackList(part);
			var first = list[0];
			var last = list[list.Count() - 1];
			if (first == last)
			{
				if (first == HEX1) { both1.Add(part); } else { both2.Add(part); }
			}
			else
			{
				if (first == HEX1) { neg1.Add(part); }
				else if (first == HEX2) { neg2.Add(part); }
				else if (last == HEX1) { pos1.Add(part); }
				else { pos2.Add(part); }
			}
		}

		var partList = SOLUTION.field_3919;
		bool PartListWasChanged = false;

		//create temporary method to make the code cleaner
		void processTrackLists(List<Part> POS, List<Part> NEG)
		{
			while (POS.Count() > 0 && NEG.Count() > 0)
			{
				//extract a part from each list
				Part pos = POS[POS.Count() - 1];
				Part neg = NEG[POS.Count() - 1];
				POS.Remove(pos);
				NEG.Remove(neg);
				partList.Remove(pos);
				partList.Remove(neg);
				//merge them together
				var posTracks = getTrackList(pos);
				var negTracks = getTrackList(neg);
				posTracks.AddRange(negTracks);
				setTrackList(pos, posTracks);
				partList.Add(pos);
				PartListWasChanged = true;
			}
		}

		processTrackLists(pos1, neg2);
		processTrackLists(pos2, neg1);
		processTrackLists(pos1, both2);
		processTrackLists(pos2, both1);
		processTrackLists(both1, neg2);
		processTrackLists(both2, neg1);
		processTrackLists(both1, both2);

		if (PartListWasChanged)
		{
			common.playSound(sounds[(int)resource.merge], 0.2f);
		}
		return PartListWasChanged;
	}

	private static bool splitTrack(HexIndex HEX1, HexIndex HEX2, Solution SOLUTION)
	{
		//returns false, even if the parts list was modified
		//this is because we don't need to manually save partState to the undo/redo history,
		//since method_1175 seems to do this for us
		List<Part> trackParts = filterTrackFromSolution(SOLUTION, x => {
			var list = getTrackList(x);
			if (!list.Contains(HEX1)) return false;
			if (!list.Contains(HEX2)) return false;
			//list contains both hexes, so list.Count() >= 2
			for (int i = 1; i < list.Count(); i++)
			{
				if (list[i - 1] == HEX1 && list[i] == HEX2) return true;
				if (list[i - 1] == HEX2 && list[i] == HEX1) return true;
			}
			return false;
		});
		if (trackParts.Count() == 0) return false;

		var partList = SOLUTION.field_3919;
		foreach (Part part in trackParts)
		{
			partList.Remove(part);
			//find start and end of each new track
			List<int> heads = new List<int>() { 0 };
			List<int> tails = new List<int>();
			int count = 1;
			var list = getTrackList(part);
			//list contains both HEX1 and HEX2, so list.Count() >= 2
			for (int i = 1; i < list.Count(); i++)
			{
				if ((list[i - 1] == HEX1 && list[i] == HEX2) || (list[i - 1] == HEX2 && list[i] == HEX1))
				{
					tails.Add(i - 1);
					heads.Add(i);
					count++;
				}
			}
			tails.Add(list.Count() - 1);
			//for each segment of track, clone a new part
			for (int i = 0; i < count; i++)
			{
				Part clone = part.method_1175(SOLUTION, (Maybe<Part>)struct_18.field_1431);
				setTrackList(clone, list.GetRange(heads[i], tails[i] - heads[i] + 1));
				partList.Add(clone);
			}
		}
		common.playSound(sounds[(int)resource.split], 0.2f);
		return false;
	}

	private static bool reverseTrack(HexIndex HEX, Solution SOLUTION)
	{
		//returns true if we need to save partState to undo/redo history because the parts list was modified
		List<Part> trackParts = filterTrackFromSolution(SOLUTION, x => getTrackList(x).Contains(HEX));
		if (trackParts.Count() == 0) return false;

		//reverse tracks
		var partList = SOLUTION.field_3919;
		var hexUnion = new HashSet<HexIndex>();
		foreach (Part part in trackParts)
		{
			partList.Remove(part);
			var trackList = getTrackList(part);
			hexUnion.UnionWith(trackList);
			trackList.Reverse();
			setTrackList(part, trackList);
			partList.Add(part);
		}
		common.playSound(sounds[(int)resource.reverse], 0.5f);
		if (!alsoReverseArms) return true;

		//reverse all arms that sit on a hex in hexUnion
		InstructionType InstructionMovePlus = class_169.field_1655;
		InstructionType InstructionMoveMinus = class_169.field_1656;

		//note: the Where predicate below needs to be more specific if a future mod contains parts that:
		//	1. can share a hex with track
		//	2. uses the +/- instruction for something else
		//i doubt this will happen, though, so leaving it as-is
		foreach (Part part in partList.Where(part => hexUnion.Contains(common.getPartOrigin(part))))
		{
			//recreate the program tape, but with move instructions reversed
			var tape = part.field_2697;
			var tape_dyn = new DynamicData(tape);
			var sortedDict = tape_dyn.Get<SortedDictionary<int, InstructionType>>("field_2415");
			var newDict = new SortedDictionary<int, InstructionType>();
			foreach (var kvp in sortedDict)
			{
				var instruction = kvp.Value;
				if (instruction == InstructionMovePlus)
				{
					instruction = InstructionMoveMinus;
				}
				else if (instruction == InstructionMoveMinus)
				{
					instruction = InstructionMovePlus;
				}
				newDict.Add(kvp.Key, instruction);
			}
			tape_dyn.Set("field_2415", newDict);
		}
		return true;
	}

	private static bool disjoinTrack(HexIndex HEX, Solution SOLUTION)
	{
		//returns true if we need to save partState to undo/redo history because the parts list was modified
		if (!allowQuantumTracking) return false;
		List<Part> trackParts = filterTrackFromSolution(SOLUTION, x => getTrackList(x).Contains(HEX));
		if (trackParts.Count() == 0) return false;
		
		var partList = SOLUTION.field_3919;
		foreach (Part part in trackParts)
		{
			partList.Remove(part);
			var trackList = getTrackList(part);
			trackList.RemoveAll(x => x == HEX);
			if (trackList.Count() != 0)
			{
				setTrackList(part, trackList);
				partList.Add(part);
			}
		}
		common.playSound(sounds[(int)resource.disjoin], 0.5f);
		return true;
	}

	//---------------------------------------------------//
	public static void SolutionEditorScreen_method_50(SolutionEditorScreen SES_self)
	{
		var current_interface = SES_self.field_4010;
		bool simStopped = SES_self.method_503() == enum_128.Stopped;
		bool inNormalInputMode = current_interface.GetType() == (new NormalInputMode()).GetType();
		var SOLUTION = SES_self.method_502();
		Vector2 mouseCoord = class_115.method_202();
		bool withinBoardEditingBounds = common.withinBoardEditingBounds(mouseCoord, SES_self);

		if (withinBoardEditingBounds)
		{
			if (editingMode == editingModeType.none)
			{
				editHex1 = common.getHexFromPoint(mouseCoord, SES_self);
			}
			editHex2 = common.getHexFromPoint(mouseCoord, SES_self);
			if (editHex2 != editHex1)
			{
				//find correct adjacent offset
				Vector2 hex1coord = common.getPointFromHex(editHex1, SES_self);
				Vector2 mouseOffset = (mouseCoord - hex1coord).Normalized();
				float unitFactor = (common.getPointFromHex(new HexIndex(1, 0), SES_self) - common.getPointFromHex(new HexIndex(0, 0), SES_self)).X;
				editHex2 = common.getHexFromPoint(hex1coord + unitFactor * mouseOffset, SES_self);
			}
		}

		if (simStopped && inNormalInputMode)
		{
			//enter another editingMode if needed, process stuff as needed
			bool saveChangesManually = false;
			var editingKey = SDL.enum_160.SDLK_g;

			switch (editingMode)
			{
				case editingModeType.none:
					if (withinBoardEditingBounds && Input.IsSdlKeyPressed(editingKey) && isTrackAtHex(editHex1, SOLUTION))
					{
						if (!Input.IsControlHeld() && !Input.IsAltHeld())
						{
							editingMode = Input.IsShiftHeld() ? editingModeType.shift_G : editingModeType.G;
						}
					}
					break;
				case editingModeType.G:
				case editingModeType.shift_G:
					if (editingMode == editingModeType.shift_G ^ Input.IsShiftHeld())
					{
						editingMode = editingMode == editingModeType.G ? editingModeType.shift_G : editingModeType.G;
					}
					if (Input.IsControlHeld() || Input.IsAltHeld())
					{
						editingMode = editingModeType.none;
					}
					else if (Input.IsSdlKeyReleased(editingKey))
					{
						if (withinBoardEditingBounds && isTrackAtHex(editHex2, SOLUTION))
						{
							if (editingMode == editingModeType.shift_G)
							{
								saveChangesManually = (editHex1 == editHex2) ? disjoinTrack(editHex1, SOLUTION) : splitTrack(editHex1, editHex2, SOLUTION);
							}
							else // editingMode == editingModeType.G
							{
								saveChangesManually = (editHex1 == editHex2) ? reverseTrack(editHex1, SOLUTION) : mergeTracks(editHex1, editHex2, SOLUTION);
							}
						}
						editingMode = editingModeType.none;
					}
					break;
				default:
					throw new class_266("FTSIGCTU: TrackEditor encounted unknown editingModeType (" + (int)editingMode + ") .");
			}

			if (saveChangesManually)
			{
				//save partState to undo/redo history
				SES_self.method_502().method_1961();
			}
		}

		if (!simStopped || !inNormalInputMode || !withinBoardEditingBounds || Input.IsLeftClickHeld() || Input.IsRightClickHeld())
		{
			editingMode = editingModeType.none;
		}
	}
	public static void class153_method_221(class_153 c153_self)
	{
		bool hexesMatch = editHex1 == editHex2;
		//early exits
		if (editingMode == editingModeType.none) return;
		if (editingMode == editingModeType.shift_G && hexesMatch && !allowQuantumTracking) return;
		//pick the texture
		Texture tex;
		if (hexesMatch)
		{
			tex = (editingMode == editingModeType.shift_G) ? textures[(int)resource.disjoin] : textures[(int)resource.reverse];
		}
		else
		{
			tex = (editingMode == editingModeType.shift_G) ? textures[(int)resource.split] : textures[(int)resource.merge];
		}
		//find the draw locations
		Vector2 view = c153_self.method_359();
		Vector2 vec1 = class_187.field_1742.method_491(editHex1, view) + new Vector2(-40f, -47f);
		Vector2 vec2 = class_187.field_1742.method_491(editHex2, view) + new Vector2(-40f, -47f);
		//draw
		float a = class_162.method_415((float)Math.Cos((double)Time.NowInSeconds() * 3.0), -1f, 1f, 0.3f, 1f);
		int reps = common.drawThickHexes ? 2 : 1;
		for (int i = 0; i < reps; i++)
		{
			class_135.method_272(tex, vec1);
			class_135.method_271(textures[(int)resource.pulse], Color.White.WithAlpha(a), vec1);
			if (!hexesMatch) class_135.method_272(tex, vec2);
		}
	}
	public static void LoadPuzzleContent()
	{
		//load textures and sounds
		textures = new Texture[(int)resource.COUNT];
		sounds = new Sound[(int)resource.COUNT];

		string path = "ftsigctu/textures/board/trackEditorHex/";
		textures[(int)resource.merge] = class_235.method_615(path + "merge");
		textures[(int)resource.split] = class_235.method_615(path + "split");
		textures[(int)resource.reverse] = class_235.method_615(path + "reverse");
		textures[(int)resource.disjoin] = class_235.method_615(path + "disjoin");
		textures[(int)resource.pulse] = class_235.method_615(path + "pulse");

		sounds[(int)resource.merge] = class_238.field_1991.field_1839;  // 'sounds/glyph_bonding'
		sounds[(int)resource.split] = class_238.field_1991.field_1849;  // 'sounds/glyph_unbonding'
		sounds[(int)resource.reverse] = class_238.field_1991.field_1854;// 'sounds/piece_modify'
		sounds[(int)resource.disjoin] = class_238.field_1991.field_1857;// 'sounds/piece_remove'
		sounds[(int)resource.pulse] = class_238.field_1991.field_1828;  //not used, 'sounds/code_failure'
	}
	public static void ApplySettings(bool _alsoReverseArms, bool _allowQuantumTracking)
	{
		alsoReverseArms = _alsoReverseArms;
		allowQuantumTracking = _allowQuantumTracking;
	}
	//---------------------------------------------------//
}