using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MixRoomManager : MonoBehaviour
{
    [SerializeField] List<GameObject> squareRoomPrefabs;
    [SerializeField] List<GameObject> crossRoomPrefabs;
    [SerializeField] List<GameObject> spawnRoomPrefabs;
    [SerializeField] List<GameObject> bossRoomPrefabs;
    [SerializeField] private int maxRooms = 15;
    [SerializeField] private int minRooms = 10;
    [SerializeField] private int minDistanceBetweenSpecialRooms = 5; // Minimum distance between SpawnRoom and BossRoom

    int roomWidth = 11;
    int roomHeight = 11;

    int gridSizeX = 20;
    int gridSizeY = 20;

    private List<GameObject> roomObjects = new List<GameObject>();

    private Queue<Vector2Int> roomQueue = new Queue<Vector2Int>();

    private int[,] roomGrid;

    private int roomCount;

    public bool generationComplete = false;

    private GameObject spawnRoom = null;
    private GameObject bossRoom = null;

    private void Start()
    {
    }

    private void Update()
    {
        if (roomQueue.Count > 0 && roomCount < maxRooms - 1 && !generationComplete)
        {
            Vector2Int roomIndex = roomQueue.Dequeue();
            int gridX = roomIndex.x;
            int gridY = roomIndex.y;

            TryGenerateRoom(new Vector2Int(gridX - 1, gridY));
            TryGenerateRoom(new Vector2Int(gridX + 1, gridY));
            TryGenerateRoom(new Vector2Int(gridX, gridY + 1));
            TryGenerateRoom(new Vector2Int(gridX, gridY - 1));
        }
        else if (roomCount < minRooms && !generationComplete)
        {
            Debug.Log("roomCount was less than the minimum amount of rooms. trying again");
            RegenerateRooms();
        }
        else if (!generationComplete)
        {
            Debug.Log($"MixRoomManager: Generation complete, {roomCount} rooms created");
            generationComplete = true;

            // Start the checker for rooms with exactly 2 adjacent rooms
            CheckAndReplaceRooms();

            // Add BossRoom at the end
            bool bossRoomAdded = AddBossRoom();
            
            // If we couldn't add a boss room, regenerate
            if (!bossRoomAdded)
            {
                Debug.Log("Failed to add boss room, regenerating level");
                generationComplete = false;
                RegenerateRooms();
            }
            else
            {
                // Verify that both special rooms exist
                EnsureSpecialRoomsExist();
            }
        }
    }

    private void StartRoomGenerationFromRoom(Vector2Int roomIndex)
    {
        roomQueue.Enqueue(roomIndex);
        int x = roomIndex.x;
        int y = roomIndex.y;
        roomGrid[x, y] = 1;
        roomCount++;
        
        // Create the spawn room
        var initialRoom = Instantiate(GetRandomPrefab(spawnRoomPrefabs), GetPositionFromGridIndex(roomIndex), Quaternion.identity);
        initialRoom.name = "SpawnRoom";
        initialRoom.GetComponent<Room>().RoomIndex = roomIndex;
        roomObjects.Add(initialRoom);
        spawnRoom = initialRoom; // Store reference to spawn room
        
        Debug.Log($"SpawnRoom created at position: {GetPositionFromGridIndex(roomIndex)}");
    }

    private bool TryGenerateRoom(Vector2Int roomIndex)
    {
        int x = roomIndex.x;
        int y = roomIndex.y;

        if (x < 0 || x >= gridSizeX || y < 0 || y >= gridSizeY)
        {
            Debug.Log($"Room index out of bounds: {roomIndex}");
            return false;
        }

        if (roomGrid[x, y] != 0)
        {
            Debug.Log($"Room already exists at {roomIndex}");
            return false;
        }

        if (roomCount >= maxRooms - 1)
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
        GameObject prefabToInstantiate = (adjacentCount == 2) ? GetRandomPrefab(crossRoomPrefabs) : GetRandomPrefab(squareRoomPrefabs);

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

    public void RegenerateRooms()
    {
        Debug.Log("Regenerating rooms...");

        // Destroy all existing rooms
        foreach (var room in roomObjects)
        {
            Destroy(room);
        }
        roomObjects.Clear();
        spawnRoom = null;
        bossRoom = null;

        // Reset the room grid and queue
        roomGrid = new int[gridSizeX, gridSizeY];
        roomQueue.Clear();
        roomCount = 0;
        generationComplete = false;

        // Recreate the SpawnRoom at the center of the grid
        Vector2Int initialRoomIndex = new Vector2Int(gridSizeX / 2, gridSizeY / 2);
        StartRoomGenerationFromRoom(initialRoomIndex);

        Debug.Log("SpawnRoom recreated at the start of regeneration.");
    }

    private void EnsureSpecialRoomsExist()
    {
        bool spawnRoomExists = false;
        bool bossRoomExists = false;
        
        foreach (var room in roomObjects)
        {
            if (room.name == "SpawnRoom") spawnRoomExists = true;
            if (room.name == "BossRoom") bossRoomExists = true;
        }

        if (!spawnRoomExists || !bossRoomExists)
        {
            Debug.LogWarning($"Special rooms missing: SpawnRoom exists: {spawnRoomExists}, BossRoom exists: {bossRoomExists}");
            
            // If the generation is complete but we're missing special rooms, regenerate
            if (generationComplete)
            {
                Debug.Log("Regenerating due to missing special rooms");
                generationComplete = false;
                RegenerateRooms();
            }
        }
        else
        {
            Debug.Log("Both special rooms exist in the generated level");
        }
    }

    private void OpenDoors(GameObject room, int x, int y)
    {
        Room newRoomScript = room.GetComponent<Room>();

        Room leftRoomScript = GetRoomScriptAt(new Vector2Int(x - 1, y));
        Room rightRoomScript = GetRoomScriptAt(new Vector2Int(x + 1, y));
        Room topRoomScript = GetRoomScriptAt(new Vector2Int(x, y + 1));
        Room bottomRoomScript = GetRoomScriptAt(new Vector2Int(x, y - 1));

        if (x > 0 && roomGrid[x - 1, y] != 0)
        {
            newRoomScript.OpenDoor(Vector2Int.left);
            if (leftRoomScript != null) leftRoomScript.OpenDoor(Vector2Int.right);
        }
        if (x < gridSizeX - 1 && roomGrid[x + 1, y] != 0)
        {
            newRoomScript.OpenDoor(Vector2Int.right);
            if (rightRoomScript != null) rightRoomScript.OpenDoor(Vector2Int.left);
        }
        if (y > 0 && roomGrid[x, y - 1] != 0)
        {
            newRoomScript.OpenDoor(Vector2Int.down);
            if (bottomRoomScript != null) bottomRoomScript.OpenDoor(Vector2Int.up);
        }
        if (y < gridSizeY - 1 && roomGrid[x, y + 1] != 0)
        {
            newRoomScript.OpenDoor(Vector2Int.up);
            if (topRoomScript != null) topRoomScript.OpenDoor(Vector2Int.down);
        }
    }

    private Room GetRoomScriptAt(Vector2Int index)
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
        return new Vector3(roomWidth * (gridX - gridSizeX / 2), roomHeight * (gridY - gridSizeY / 2));
    }

    private void OnDrawGizmos()
    {
        Color gizmoColor = new Color(0, 1, 1, 0.05f);
        Gizmos.color = gizmoColor;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 position = GetPositionFromGridIndex(new Vector2Int(x, y));
                Gizmos.DrawWireCube(position, new Vector3(roomWidth, roomHeight, 1));
            }
        }
    }

    private void CheckAndReplaceRooms()
    {
        foreach (var roomObject in roomObjects.ToArray())
        {
            // Skip replacing special rooms
            if (roomObject.name == "SpawnRoom" || roomObject.name == "BossRoom")
                continue;
                
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

                var crossRoom = Instantiate(GetRandomPrefab(crossRoomPrefabs), GetPositionFromGridIndex(roomIndex), Quaternion.identity);
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

private bool AddBossRoom()
{
    // Find a suitable location for the BossRoom
    Vector2Int bossRoomIndex = FindBossRoomLocation();
    if (bossRoomIndex != Vector2Int.zero)
    {
        // Remove the existing room at the BossRoom location
        GameObject existingRoom = roomObjects.Find(r => r.GetComponent<Room>().RoomIndex == bossRoomIndex);
        if (existingRoom != null)
        {
            Debug.Log($"Replacing room {existingRoom.name} with BossRoom");
            roomObjects.Remove(existingRoom);
            Destroy(existingRoom);
        }

        // Create the boss room
        var bossRoom = Instantiate(GetRandomPrefab(bossRoomPrefabs), GetPositionFromGridIndex(bossRoomIndex), Quaternion.identity);
        bossRoom.GetComponent<Room>().RoomIndex = bossRoomIndex;
        bossRoom.name = "BossRoom";
        roomObjects.Add(bossRoom);
        this.bossRoom = bossRoom; // Store reference to boss room
        
        Debug.Log($"BossRoom created at position: {GetPositionFromGridIndex(bossRoomIndex)}");

        // Open the doors for the boss room.
        OpenDoors(bossRoom, bossRoomIndex.x, bossRoomIndex.y);
        
        return true;
    }
    else
    {
        Debug.LogWarning("Failed to place BossRoom - no suitable location found");
        return false;
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

    Debug.Log($"SpawnRoom location: {spawnRoomIndex}");

    // If we couldn't find the spawn room, return zero
    if (spawnRoomIndex == Vector2Int.zero)
    {
        Debug.LogError("Couldn't find SpawnRoom when trying to place BossRoom");
        return Vector2Int.zero;
    }

    Vector2Int bestRoomIndex = Vector2Int.zero;
    float maxDistance = 0;

    // First, try to find a room with exactly one adjacent room
    foreach (var roomObject in roomObjects)
    {
        // Skip if this is already the SpawnRoom or BossRoom
        if (roomObject.name == "SpawnRoom" || roomObject.name == "BossRoom")
            continue;

        Room roomScript = roomObject.GetComponent<Room>();
        Vector2Int roomIndex = roomScript.RoomIndex;

        // Count adjacent rooms - we want rooms with only one connection
        int adjacentCount = CountAdjacentRooms(roomIndex);

        // Calculate distance from spawn room
        float distance = Vector2Int.Distance(roomIndex, spawnRoomIndex);

        Debug.Log($"Checking room at {roomIndex}: Adjacent={adjacentCount}, Distance={distance}");

        // Find the farthest dead-end room that meets our distance requirement
        if (adjacentCount == 1 && distance > maxDistance && distance >= minDistanceBetweenSpecialRooms)
        {
            maxDistance = distance;
            bestRoomIndex = roomIndex;
        }
    }

    // If we found a suitable room with exactly one adjacent room
    if (bestRoomIndex != Vector2Int.zero)
    {
        Debug.Log($"Found BossRoom location at {bestRoomIndex}, Distance: {maxDistance}");
        return bestRoomIndex;
    }

    Debug.Log("No room with exactly one adjacent room found. Falling back to any valid room.");

    // Fallback: Find any room that meets the minimum distance requirement
    maxDistance = 0;
    foreach (var roomObject in roomObjects)
    {
        // Skip if this is already the SpawnRoom or BossRoom
        if (roomObject.name == "SpawnRoom" || roomObject.name == "BossRoom")
            continue;

        Room roomScript = roomObject.GetComponent<Room>();
        Vector2Int roomIndex = roomScript.RoomIndex;

        // Calculate distance from spawn room
        float distance = Vector2Int.Distance(roomIndex, spawnRoomIndex);

        Debug.Log($"Checking fallback room at {roomIndex}: Distance={distance}");

        // Find the farthest room that meets our distance requirement
        if (distance > maxDistance)
        {
            maxDistance = distance;
            bestRoomIndex = roomIndex;
        }
    }

    // If we found any room that's far enough, use it
    if (bestRoomIndex != Vector2Int.zero && maxDistance >= minDistanceBetweenSpecialRooms)
    {
        Debug.Log($"Placing BossRoom at fallback room: {bestRoomIndex}, Distance: {maxDistance}");
        return bestRoomIndex;
    }

    // Last resort: If we can't find a room that's far enough, use the farthest room we found
    if (bestRoomIndex != Vector2Int.zero)
    {
        Debug.LogWarning($"Using closest available room for BossRoom: {bestRoomIndex}, Distance: {maxDistance}");
        return bestRoomIndex;
    }

    Debug.Log("No valid BossRoom location found");
    return Vector2Int.zero;
}

private GameObject GetRandomPrefab(List<GameObject> prefabs)
{
    if (prefabs == null || prefabs.Count == 0)
    {
        Debug.LogError("No prefabs provided for selection");
        return null;
    }
    return prefabs[Random.Range(0, prefabs.Count)];
}

// Method to get the list of room objects
public List<GameObject> GetRoomObjects()
{
    return roomObjects;
}
}