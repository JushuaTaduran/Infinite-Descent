using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GEDRoomManager : MonoBehaviour
{
    [SerializeField] GameObject squareRoomPrefab;
    [SerializeField] GameObject crossRoomPrefab;
    [SerializeField] GameObject spawnRoomPrefab;
    [SerializeField] GameObject bossRoomPrefab;
    [SerializeField] private int maxRooms = 15;
    [SerializeField] private int minRooms = 10;
    [SerializeField] private int minDistanceBetweenSpecialRooms = 5;

    public int roomWidth = 11;
    public int roomHeight = 11;

    public int gridSizeX = 15;
    public int gridSizeY = 15;

    private List<GameObject> roomObjects = new List<GameObject>();
    private Queue<Vector2Int> roomQueue = new Queue<Vector2Int>();
    private int[,] roomGrid;
    private int roomCount;

    public bool generationComplete = false; // Public flag to indicate generation completion
    private List<GameObject> rooms = new List<GameObject>();

    // Dictionary to store room positions and connections
    public Dictionary<string, Vector2Int> roomPositions = new Dictionary<string, Vector2Int>();
    public HashSet<(string, string)> roomConnections = new HashSet<(string, string)>();

    private void Start()
    {
        roomGrid = new int[gridSizeX, gridSizeY];
        roomQueue = new Queue<Vector2Int>();

        Vector2Int initialRoomIndex = new Vector2Int(gridSizeX / 2, gridSizeY / 2);
        StartRoomGenerationFromRoom(initialRoomIndex);
    }

    private void Update()
    {
        if (roomQueue.Count > 0 && roomCount < maxRooms && !generationComplete)
        {
            Vector2Int roomIndex = roomQueue.Dequeue();
            int gridX = roomIndex.x;
            int gridY = roomIndex.y;

            TryGenerateRoom(new Vector2Int(gridX - 1, gridY));
            TryGenerateRoom(new Vector2Int(gridX + 1, gridY));
            TryGenerateRoom(new Vector2Int(gridX, gridY + 1));
            TryGenerateRoom(new Vector2Int(gridX, gridY - 1));
        }
        else if (roomCount < minRooms)
        {
            Debug.Log("roomCount was less than the minimum amount of rooms. trying again");
            RegenerateRooms();
        }
        else if (!generationComplete)
        {
            Debug.Log($"RoomManager: Generation complete, {roomCount} rooms created");
            generationComplete = true;

            // Additional logic for room generation completion...
            CheckAndReplaceRooms();
            AddBossRoom();
            EnsureSpecialRoomsExist();
            BuildGraph();
        }
    }

    public void StartRoomGenerationFromRoom(Vector2Int roomIndex)
    {
        roomQueue.Enqueue(roomIndex);
        int x = roomIndex.x;
        int y = roomIndex.y;
        roomGrid[x, y] = 1;
        roomCount++;
        var initialRoom = Instantiate(spawnRoomPrefab, GetPositionFromGridIndex(roomIndex), Quaternion.identity);
        initialRoom.name = $"SpawnRoom";
        initialRoom.GetComponent<Room>().RoomIndex = roomIndex;
        roomObjects.Add(initialRoom);
        Debug.Log($"SpawnRoom created at position: {GetPositionFromGridIndex(roomIndex)}");
    }

    private bool TryGenerateRoom(Vector2Int roomIndex)
    {
        int x = roomIndex.x;
        int y = roomIndex.y;

        if (roomCount >= maxRooms)
        {
            Debug.Log($"Max rooms reached: {roomCount}");
            return false;
        }

        // Random chance to avoid always placing rooms
        if (Random.value < 0.5f && roomIndex != Vector2Int.zero)
        {
            Debug.Log($"Random chance prevented room generation at {roomIndex}");
            return false;
        }

        int adjacentCount = CountAdjacentRooms(roomIndex);
        Debug.Log($"RoomIndex: {roomIndex}, AdjacentCount: {adjacentCount}");

        // Only place a room if there is 0 or 1 adjacent room.
        if (adjacentCount > 1)
        {
            Debug.Log($"Too many adjacent rooms ({adjacentCount}) at {roomIndex}, skipping room generation");
            return false;
        }

        roomQueue.Enqueue(roomIndex);
        roomGrid[x, y] = 1;
        roomCount++;

        // Determine which prefab to use based on adjacent count.
        GameObject prefabToInstantiate = (adjacentCount == 2) ? crossRoomPrefab : squareRoomPrefab;

        // Debug logs to check the adjacent count and selected prefab
        Debug.Log($"RoomIndex: {roomIndex}, AdjacentCount: {adjacentCount}, Prefab: {(adjacentCount == 2 ? "CrossRoom" : "SquareRoom")}");

        // Instantiate the selected room prefab.
        var newRoom = Instantiate(prefabToInstantiate, GetPositionFromGridIndex(roomIndex), Quaternion.identity);
        newRoom.GetComponent<Room>().RoomIndex = roomIndex;
        newRoom.name = $"Room-{roomCount}";
        roomObjects.Add(newRoom);

        // Open the doors for the new room.
        OpenDoors(newRoom, x, y);

        return true;
    }

    private void RegenerateRooms()
    {
        Debug.Log("Regenerating rooms...");
        roomObjects.ForEach(room => Debug.Log($"Destroying room: {room.name}"));
        roomObjects.ForEach(Destroy);
        roomObjects.Clear();
        roomGrid = new int[gridSizeX, gridSizeY];
        roomQueue.Clear();
        roomCount = 0;
        generationComplete = false;

        // Recreate the SpawnRoom
        Vector2Int initialRoomIndex = new Vector2Int(gridSizeX / 2, gridSizeY / 2);
        StartRoomGenerationFromRoom(initialRoomIndex);
    }

    private void EnsureSpecialRoomsExist()
    {
        bool spawnRoomExists = roomObjects.Exists(room => room.name == "SpawnRoom");
        bool bossRoomExists = roomObjects.Exists(room => room.name == "BossRoom");

        if (!spawnRoomExists || !bossRoomExists)
        {
            Debug.Log("Special rooms missing, regenerating...");
            RegenerateRooms();
        }
    }

    void OpenDoors(GameObject room, int x, int y)
    {
        Room newRoomScript = room.GetComponent<Room>();

        Room leftRoomScript = GetRoomScriptAt(new Vector2Int(x - 1, y));
        Room rightRoomScript = GetRoomScriptAt(new Vector2Int(x + 1, y));
        Room topRoomScript = GetRoomScriptAt(new Vector2Int(x, y + 1));
        Room bottomRoomScript = GetRoomScriptAt(new Vector2Int(x, y - 1));

        if (x > 0 && roomGrid[x - 1, y] != 0)
        {
            newRoomScript.OpenDoor(Vector2Int.left);
            leftRoomScript.OpenDoor(Vector2Int.right);
        }
        if (x < gridSizeX - 1 && roomGrid[x + 1, y] != 0)
        {
            newRoomScript.OpenDoor(Vector2Int.right);
            rightRoomScript.OpenDoor(Vector2Int.left);
        }
        if (y > 0 && roomGrid[x, y - 1] != 0)
        {
            newRoomScript.OpenDoor(Vector2Int.down);
            bottomRoomScript.OpenDoor(Vector2Int.up);
        }
        if (y < gridSizeY - 1 && roomGrid[x, y + 1] != 0)
        {
            newRoomScript.OpenDoor(Vector2Int.up);
            topRoomScript.OpenDoor(Vector2Int.down);
        }
    }

    Room GetRoomScriptAt(Vector2Int index)
    {
        GameObject roomObject = roomObjects.Find(r => r.GetComponent<Room>().RoomIndex == index);
        if (roomObject != null)
            return roomObject.GetComponent<Room>();
        return null;
    }

    private int CountAdjacentRooms(Vector2Int roomIndex)
    {
        int x = roomIndex.x;
        int y = roomIndex.y;
        int count = 0;

        if (x > 0 && roomGrid[x - 1, y] != 0) count++;
        if (x < gridSizeX - 1 && roomGrid[x + 1, y] != 0) count++;
        if (y > 0 && roomGrid[x, y - 1] != 0) count++;
        if (y < gridSizeY - 1 && roomGrid[x, y + 1] != 0) count++;

        return count;
    }

    private Vector3 GetPositionFromGridIndex(Vector2Int gridIndex)
    {
        int gridX = gridIndex.x;
        int gridY = gridIndex.y;

        // Offset the position by the position of the GEDRoomManager GameObject
        Vector3 offset = transform.position;

        return new Vector3(roomWidth * (gridX - gridSizeX / 2), roomHeight * (gridY - gridSizeY / 2), 0) + offset;
    }

    private void CheckAndReplaceRooms()
    {
        foreach (var roomObject in roomObjects.ToArray())
        {
            Room roomScript = roomObject.GetComponent<Room>();
            Vector2Int roomIndex = roomScript.RoomIndex;
            int adjacentCount = CountAdjacentRooms(roomIndex);

            if (adjacentCount == 2)
            {
                Debug.Log($"Replacing Room-{roomScript.RoomIndex} with appropriate CrossRoom due to adjacent count of 2");

                // Collect the directions of adjacent rooms
                bool top = roomIndex.y < gridSizeY - 1 && roomGrid[roomIndex.x, roomIndex.y + 1] != 0;
                bool bottom = roomIndex.y > 0 && roomGrid[roomIndex.x, roomIndex.y - 1] != 0;
                bool left = roomIndex.x > 0 && roomGrid[roomIndex.x - 1, roomIndex.y] != 0;
                bool right = roomIndex.x < gridSizeX - 1 && roomGrid[roomIndex.x + 1, roomIndex.y] != 0;

                // Remove the square room and replace it with the cross room
                Destroy(roomObject);
                roomObjects.Remove(roomObject);

                var crossRoom = Instantiate(crossRoomPrefab, GetPositionFromGridIndex(roomIndex), Quaternion.identity);
                crossRoom.GetComponent<Room>().RoomIndex = roomIndex;
                crossRoom.name = $"Room-{roomCount}";
                roomObjects.Add(crossRoom);

                // Open the doors for the cross room.
                OpenDoors(crossRoom, roomIndex.x, roomIndex.y);

                // Activate the appropriate room type within the crossRoomPrefab
                Room crossRoomScript = crossRoom.GetComponent<Room>();
                if (top && bottom)
                {
                    crossRoomScript.ActivateRoomType("VerticalRoom");
                }
                else if (left && right)
                {
                    crossRoomScript.ActivateRoomType("HorizontalRoom");
                }
                else if (top && left)
                {
                    crossRoomScript.ActivateRoomType("LTRoom");
                }
                else if (top && right)
                {
                    crossRoomScript.ActivateRoomType("RTRoom");
                }
                else if (bottom && left)
                {
                    crossRoomScript.ActivateRoomType("LBRoom");
                }
                else if (bottom && right)
                {
                    crossRoomScript.ActivateRoomType("RBRoom");
                }
            }
        }
    }

    private void AddBossRoom()
    {
        // Find a suitable location for the BossRoom
        Vector2Int bossRoomIndex = FindBossRoomLocation();
        if (bossRoomIndex != Vector2Int.zero)
        {
            var bossRoom = Instantiate(bossRoomPrefab, GetPositionFromGridIndex(bossRoomIndex), Quaternion.identity);
            bossRoom.GetComponent<Room>().RoomIndex = bossRoomIndex;
            bossRoom.name = "BossRoom";
            roomObjects.Add(bossRoom);
            Debug.Log($"BossRoom created at position: {GetPositionFromGridIndex(bossRoomIndex)}");

            // Open the doors for the boss room.
            OpenDoors(bossRoom, bossRoomIndex.x, bossRoomIndex.y);
        }
    }

    private Vector2Int FindBossRoomLocation()
    {
        // Find the SpawnRoom's location
        Vector2Int spawnRoomIndex = Vector2Int.zero;
        foreach (var roomObject in roomObjects)
        {
            if (roomObject.name == "SpawnRoom")
            {
                spawnRoomIndex = roomObject.GetComponent<Room>().RoomIndex;
                break;
            }
        }

        // Find a room with only one adjacent room to place the BossRoom
        foreach (var roomObject in roomObjects)
        {
            Room roomScript = roomObject.GetComponent<Room>();
            Vector2Int roomIndex = roomScript.RoomIndex;
            int adjacentCount = CountAdjacentRooms(roomIndex);

            // Ensure the BossRoom is placed at a minimum distance from the SpawnRoom
            if (adjacentCount == 1 && Vector2Int.Distance(roomIndex, spawnRoomIndex) >= minDistanceBetweenSpecialRooms)
            {
                return roomIndex;
            }
        }

        // If no suitable location is found, return zero vector
        return Vector2Int.zero;
    }

    // Method to build the graph from the generated rooms
    private void BuildGraph()
    {
        foreach (var roomObject in roomObjects)
        {
            Room roomScript = roomObject.GetComponent<Room>();
            Vector2Int roomIndex = roomScript.RoomIndex;
            string roomName = roomObject.name;

            // Add the room position to the dictionary
            roomPositions[roomName] = roomIndex;

            // Check for adjacent rooms and add connections
            if (roomIndex.x > 0 && roomGrid[roomIndex.x - 1, roomIndex.y] != 0)
            {
                string adjacentRoomName = GetRoomNameAt(new Vector2Int(roomIndex.x - 1, roomIndex.y));
                if (adjacentRoomName != null)
                {
                    roomConnections.Add((roomName, adjacentRoomName));
                }
            }
            if (roomIndex.x < gridSizeX - 1 && roomGrid[roomIndex.x + 1, roomIndex.y] != 0)
            {
                string adjacentRoomName = GetRoomNameAt(new Vector2Int(roomIndex.x + 1, roomIndex.y));
                if (adjacentRoomName != null)
                {
                    roomConnections.Add((roomName, adjacentRoomName));
                }
            }
            if (roomIndex.y > 0 && roomGrid[roomIndex.x, roomIndex.y - 1] != 0)
            {
                string adjacentRoomName = GetRoomNameAt(new Vector2Int(roomIndex.x, roomIndex.y - 1));
                if (adjacentRoomName != null)
                {
                    roomConnections.Add((roomName, adjacentRoomName));
                }
            }
            if (roomIndex.y < gridSizeY - 1 && roomGrid[roomIndex.x, roomIndex.y + 1] != 0)
            {
                string adjacentRoomName = GetRoomNameAt(new Vector2Int(roomIndex.x, roomIndex.y + 1));
                if (adjacentRoomName != null)
                {
                    roomConnections.Add((roomName, adjacentRoomName));
                }
            }
        }

        Debug.Log("[GEDRoomManager] Graph built successfully.");
    }

    // Helper method to get the name of a room at a specific grid index
    private string GetRoomNameAt(Vector2Int index)
    {
        GameObject roomObject = roomObjects.Find(r => r.GetComponent<Room>().RoomIndex == index);
        if (roomObject != null)
            return roomObject.name;
        return null;
    }
}