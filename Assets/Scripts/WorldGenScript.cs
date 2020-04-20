using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldGenScript : MonoBehaviour
{
    static float NOTHINGNESS_VACANCY_CHANCE = .5f;

    public GameObject[] roomPrefabs;
    public TextAsset roomInfosTextAsset;

    public GameObject player;

    List<RoomInfo> roomTypes;
    Dictionary<Tuple<int, int, int>, RoomInfo> roomInfos;
    Dictionary<Tuple<int, int, int>, GameObject> roomObjects;
    Tuple<int, int, int> lastPlayerCoor;

    void Start()
    {
        // Parse room infos.
        roomTypes = new List<RoomInfo>();
        int prefabIndex = 0;
        foreach (string line in Util.SplitNewLines(roomInfosTextAsset.text).Skip(1)) {
            roomTypes.Add(new RoomInfo(prefabIndex++, line));
        }
        // Create the first room.
        roomInfos = new Dictionary<Tuple<int, int, int>, RoomInfo>();
        roomObjects = new Dictionary<Tuple<int, int, int>, GameObject>();
        Tuple<int, int, int> center = new Tuple<int, int, int>(0, 0, 0);
        RoomInfo startRoom = new RoomInfo(roomTypes[6]);
        InstantiateRoom(center, startRoom);
        lastPlayerCoor = new Tuple<int, int, int>(int.MinValue, int.MinValue, int.MinValue);
    }

    void Update()
    {
        Tuple<int, int, int> playerCoor = new Tuple<int, int, int>(Mathf.FloorToInt((player.transform.localPosition.x + 8) / 16),
                                                                   Mathf.FloorToInt(player.transform.localPosition.y / 10),
                                                                   Mathf.FloorToInt((player.transform.localPosition.z + 8) / 16));
        if (playerCoor.Equals(lastPlayerCoor)) {
            return;
        }
        if (playerCoor.Item2 != 0) {
            // TODO: Add verticality.
            return;
        }
        lastPlayerCoor = playerCoor;
        bool generated = true;
        while (generated) {
            generated = GenerateWithinN(playerCoor, 2);
        }
    }
    bool GenerateWithinN(Tuple<int, int, int> playerCoor, int n) {
        bool generated = false;
        // Find all missing rooms within N that can be reached.
        for (int dx = -n; dx <= n; dx++) {
            // TODO: Add verticality back in.
            int dy = 0; //for (int dy = -n; dy <= n; dy++) {
                for (int dz = -n; dz <= n; dz++) {
                    Tuple<int, int, int> coor = new Tuple<int, int, int>(playerCoor.Item1 + dx, playerCoor.Item2 + dy, playerCoor.Item3 + dz);
                    if (roomInfos.ContainsKey(coor)) {
                        // This room has already been generated.
                        continue;
                    }
                    // We need to generate everything we can see, not just where we can go.
                    //if (!CanBeReached(playerCoor, coor)) {
                        // There would be no path to this room.
                        //continue;
                    //}
                    RoomInfo compatibleRoomInfo = GetCompatibleRoomInfo(coor);
                    if (compatibleRoomInfo == null) {
                        // We've decided not to put something here, maybe because nothing will fit.
                        roomInfos[coor] = null;
                    } else {
                        // Instantiate the room.
                        InstantiateRoom(coor, compatibleRoomInfo);
                    }
                    generated = true;
                }
            //}
        }
        return generated;
    }
    bool CanBeReached(Tuple<int, int, int> start, Tuple<int, int, int> goal) {
        Queue<Tuple<int, int, int>> queue = new Queue<Tuple<int, int, int>>();
        HashSet<Tuple<int, int, int>> seen = new HashSet<Tuple<int, int, int>>();
        queue.Enqueue(start);
        seen.Add(start);
        while (queue.Count > 0) {
            Tuple<int, int, int> current = queue.Dequeue();
            if (RoomIsAbsent(current)) {
                continue;
            }
            RoomInfo currentInfo = roomInfos[current];
            // Check if the current room has an exit in each direction.
            for (int i = 0; i < 6; i++) {
                if (currentInfo.exits[i] != ExitType.Door) {
                    continue;
                }
                Tuple<int, int, int> neighborCoor = GetNeighborCoor(current, i);
                if (neighborCoor.Equals(goal)) {
                    return true;
                }
                if (seen.Contains(neighborCoor)) {
                    continue;
                }
                queue.Enqueue(neighborCoor);
                seen.Add(neighborCoor);
            }
        }
        return false;
    }
    RoomInfo GetCompatibleRoomInfo(Tuple<int, int, int> coor) {
        ExitType[] adjacentExits = new ExitType[6];
        for (int direction = 0; direction < 6; direction++) {
            Tuple<int, int, int> neighborCoor = GetNeighborCoor(coor, direction);
            if (!roomInfos.ContainsKey(neighborCoor)) {
                adjacentExits[direction] = ExitType.Undecided;
            } else if (roomInfos[neighborCoor] == null) {
                adjacentExits[direction] = ExitType.Nothingness;
            } else {
                adjacentExits[direction] = roomInfos[neighborCoor].exits[ReverseDirection(direction)];
            }
        }
        if (UnityEngine.Random.value < NOTHINGNESS_VACANCY_CHANCE) {
            // We're only connected to nothingness, so we should sometimes spawn nothing.
            bool foundNothingness = false;
            bool eligibleForNull = true;
            foreach (ExitType exit in adjacentExits) {
                if (exit == ExitType.Nothingness) {
                    foundNothingness = true;
                } else if (exit != ExitType.Undecided) {
                    eligibleForNull = false;
                    break;
                }
            }
            if (foundNothingness && eligibleForNull) {
                return null;
            }
        }

        int[] roomTypeOrder = Enumerable.Range(0, roomTypes.Count).ToArray().Shuffle();
        foreach (RoomInfo roomType in roomTypeOrder.Select(i => roomTypes[i])) {
            bool[] flipOrder = new bool[] { false, true }.Shuffle();
            int[] rotationOrder = Enumerable.Range(0, 4).ToArray().Shuffle();
            foreach (bool flipped in flipOrder) {
                if (flipped && !roomType.flippable) {
                    continue;
                }
                foreach (int rotation in rotationOrder) {
                    // Given this configuration of this room type, do its entrances match up with the adjacent exits already present?
                    bool compatible = true;
                    for (int direction = 0; direction < 6; direction++) {
                        if (!adjacentExits[direction].IsCompatibleWith(roomType.exits[RoomInfo.TranslateDirection(direction, flipped, rotation)], direction)) {
                            compatible = false;
                            break;
                        }
                    }
                    if (compatible) {
                        return new RoomInfo(roomType, flipped, rotation); 
                    }
                }
            }
        }

        Debug.LogErrorFormat("Failed to generate a room at {0} to fit adjacent exits: {1}.", coor, string.Join(", ", adjacentExits));
        return null;
    }
    Tuple<int, int, int> GetNeighborCoor(Tuple<int, int, int> coor, int direction) {
        if (direction == 0) {
            return new Tuple<int, int, int>(coor.Item1, coor.Item2, coor.Item3 + 1);
        } else if (direction == 1) {
            return new Tuple<int, int, int>(coor.Item1 + 1, coor.Item2, coor.Item3);
        } else if (direction == 2) {
            return new Tuple<int, int, int>(coor.Item1, coor.Item2, coor.Item3 - 1);
        } else if (direction == 3) {
            return new Tuple<int, int, int>(coor.Item1 - 1, coor.Item2, coor.Item3);
        } else if (direction == 4) {
            return new Tuple<int, int, int>(coor.Item1, coor.Item2 + 1, coor.Item3);
        } else if (direction == 5) {
            return new Tuple<int, int, int>(coor.Item1, coor.Item2 - 1, coor.Item3);
        }
        throw new Exception("Unknown direction.");
    }
    int ReverseDirection (int direction) {
        if (direction == 0) {
            return 2;
        } else if (direction == 1) {
            return 3;
        } else if (direction == 2) {
            return 0;
        } else if (direction == 3) {
            return 1;
        } else if (direction == 4) {
            return 5;
        }
        return 4;
    }
    bool RoomIsAbsent(Tuple<int, int, int> coor) {
        return !roomInfos.ContainsKey(coor) || roomInfos[coor] == null;
    }

    void InstantiateRoom(Tuple<int, int, int> coor, RoomInfo info) {
        UnityEngine.Random.State oldRandomState = UnityEngine.Random.state;
        UnityEngine.Random.state = info.randomState;

        GameObject roomObject = Instantiate(roomPrefabs[info.prefabIndex]);
        roomObject.transform.localPosition = new Vector3(coor.Item1 * 16, coor.Item2 * 10, coor.Item3 * 16);
        if (info.flipped) {
            roomObject.transform.localScale = new Vector3(-1, 1, 1);
        }
        if (info.rotation > 0) {
            roomObject.transform.localRotation = Quaternion.Euler(0, info.rotation * 90, 0);
        }
        roomObject.name = info.ToString();

        roomInfos.Add(coor, info);
        roomObjects.Add(coor, roomObject);
        UnityEngine.Random.state = oldRandomState;
    }
}

class RoomInfo {
    static int[][][] TRANSFORM_LOOKUP = new int[][][] {
        new int[][]{ new int[] { 0, 1, 2, 3, 4, 5 }, new int[] { 3, 0, 1, 2, 4, 5 }, new int[] { 2, 3, 0, 1, 4, 5 }, new int[] { 1, 2, 3, 0, 4, 5 } },
        new int[][]{ new int[] { 0, 3, 2, 1, 4, 5 }, new int[] { 1, 0, 3, 2, 4, 5 }, new int[] { 2, 1, 0, 3, 4, 5 }, new int[] { 3, 2, 1, 0, 4, 5 } }
    };

    public int prefabIndex;
    public string name;
    public ExitType[] exits;
    public UnityEngine.Random.State randomState;
    public bool flipped; // across the X axis. performed before rotation.
    public int rotation; // [0, 3], number of 90 degree turns clockwise
    public bool flippable;

    // Prototype constructor.
    public RoomInfo(int prefabIndex, string line) {
        this.prefabIndex = prefabIndex;
        string[] tokens = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
        name = tokens[0];
        exits = tokens.Skip(1).Take(6).Select(token => token.ToExitType()).ToArray();
        flippable = tokens[7] == "Y";
    }
    // Starting room constructor.
    public RoomInfo(RoomInfo other) {
        prefabIndex = other.prefabIndex;
        name = other.name;
        exits = (ExitType[])other.exits.Clone();
        flippable = other.flippable;
        randomState = UnityEngine.Random.state;
    }
    // Room variant constructor.
    public RoomInfo(RoomInfo other, bool flipped, int rotation) {
        prefabIndex = other.prefabIndex;
        name = other.name;
        exits = new ExitType[6];
        int[] transform = TRANSFORM_LOOKUP[flipped ? 1 : 0][rotation];
        for (int direction = 0; direction < 6; direction++) {
            exits[direction] = other.exits[transform[direction]];
        }
        randomState = UnityEngine.Random.state;
        this.flipped = flipped;
        this.rotation = rotation;
        this.flippable = other.flippable;
    }

    public override string ToString() {
        return string.Format("{0} rotation {1}{2}", name, rotation, flipped ? " (flipped)" : "");
    }

    public static int TranslateDirection(int direction, bool flipped, int rotation) {
        return TRANSFORM_LOOKUP[flipped ? 1 : 0][rotation][direction];
    }
}

public enum ExitType : int {
    Undecided, None, Nothingness, Door, Wall, Outcropping
}
public static class ExitTypeExtensions {
    static bool[,] HORIZONTAL_COMPATIBILITY_MATRIX = new bool[,] {
        { true,  true,  true,  true,  true,  true },
        { true,  true,  true,  false, false, false },
        { true,  true,  true,  false, false, false },
        { true,  false, false, true,  false, false },
        { true,  false, false, false, true,  true  },
        { true,  false, false, false, true,  false },
    };
    static bool[,] VERTICAL_COMPATIBILITY_MATRIX = new bool[,] {
        { true,  true,  true,  true,  true,  true },
        { true,  true,  false, false, false, false },
        { true,  false, true,  false, false, false },
        { true,  false, false, true,  false, false },
        { true,  false, false, false, true,  false },
        { true,  false, false, false, false, false },
    };

    public static ExitType ToExitType(this string c) {
        if (c == "N") {
            return ExitType.Nothingness;
        } else if (c == "D") {
            return ExitType.Door;
        } else if (c == "W") {
            return ExitType.Wall;
        } else if (c == "O") {
            return ExitType.Outcropping;
        }
        return ExitType.None;
    }
    public static bool IsCompatibleWith(this ExitType one, ExitType two, int direction) {
        return direction <= 3 ? HORIZONTAL_COMPATIBILITY_MATRIX[(int)one, (int)two] : VERTICAL_COMPATIBILITY_MATRIX[(int)one, (int)two];
    }
}