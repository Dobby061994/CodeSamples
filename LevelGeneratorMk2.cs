using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGeneratorMk2 : MonoBehaviour
{
    // *ATTENTION*
    // This is an in-progress version of my level generating script as I continue to work on the project.

    // Public variables required for level generation:
    public Room roomStartPrefab, roomExitPrefab;
    public Room roomStairwellPrefab;
    public Room roomStoreroomPrefab;
    public Floor floorPrefab;
    public List<Floor> floorList = new List<Floor>();
    public List<Room> connectingRoomPrefabList = new List<Room>();
    public List<RoomUnique> uniqueRoomPrefabList = new List<RoomUnique>();
    public List<ItemSpawnPoint> storeroomSpawnPoints = new List<ItemSpawnPoint>();
    public int floorLength, totalFloors;
    public int currentFloorNum;
    public int totalStorerooms;
    public int uniqueRoomChance;
    public bool randomScenery;

    // A list of the rooms that are guaranteed to spawn but only once each:
    public List<Room> uniqueRoomsToSpawn = new List<Room>();

    // References to other scripts:
    ItemSpawningMk2 objSpawn;
    MenuHandling menuHandler;

    // References to the start and exit rooms:
    RoomStart startRoom;
    RoomExit exitRoom;

    // The LayerMasks used to check for overlapping geometry:
    LayerMask roomLayerMask, doorwayLayerMask;

    // Actor prefabs:
    public GameObject playerPrefab, BGMprefab, enemyPrefab, player;

    private void Start()
    {
        roomLayerMask = LayerMask.GetMask("ProceduralRoom");
        doorwayLayerMask = LayerMask.GetMask("Doorway");
        currentFloorNum = 0;
        objSpawn = GetComponent<ItemSpawningMk2>();
        menuHandler = GetComponent<MenuHandling>();
    }

    public IEnumerator GenerateLevel()
    {
        WaitForSeconds startup = new WaitForSeconds(1);
        WaitForFixedUpdate interval = new WaitForFixedUpdate();
        yield return startup;

        // Clear the list of unique rooms to be spawned at the start of level generation, in case there is an error and we need
        // to restart generation:
        uniqueRoomsToSpawn.Clear();
        foreach (Room room in uniqueRoomPrefabList)
        {
            // We compile a list of the unique rooms that are yet to be spawned. This way we can later iterate through the list
            // and spawn each one once:
            uniqueRoomsToSpawn.Add(room);
        }

        // For each desired floor:
        for (int a = 0; a < totalFloors; a++)
        {
            // Create a new instance of Floor.cs:
            Floor currentFloor = CreateNewFloor(currentFloorNum);
            floorList.Add(currentFloor);

            // If this floor is the 1st floor of the level, spawn the start room:
            if (currentFloorNum == 0)
            {
                SpawnStartRoom(currentFloor);
                yield return interval;
            }
            // Else if this floor is not the 1st floor, then the 1st room in it will always be a stairwell, so the uppermost
            // door of the stairwell must be added to the list of available doors in the new floor:
            else
            {
                // Add upper stairwell door to new floor's list:
                AddDoorsToList(floorList[currentFloorNum - 1].rooms[floorList[currentFloorNum - 1].rooms.Count - 1], ref currentFloor.doorways);
            }

            // Spawn the specified number of connecting rooms:
            for (int b = 0; b < floorLength; b++)
            {
                SpawnConnectingRoom(currentFloor);
                yield return interval;
            }

            // If there are still further floors to spawn, spawn a stairwell leading up:
            if (currentFloorNum < totalFloors - 1)
            {
                SpawnStairwell(currentFloor);
                yield return interval;
            }
            // Else if there are no further floors to spawn, spawn the exit room:
            else
            {
                SpawnExitRoom(currentFloor);
                yield return interval;
            }
        }

        // If there are unique rooms to spawn:
        if (uniqueRoomsToSpawn.Count > 0)
        {
            // Spawn one of each unique room:
            for (int i = uniqueRoomsToSpawn.Count; i > 0; i--)
            {
                SpawnUniqueRoom(floorList[Random.Range(0, floorList.Count)]);
                yield return interval;
            }
        }

        // Spawn the specified number of storerooms:
        for (int i = 0; i < totalStorerooms; i++)
        {
            SpawnStoreroom(floorList[Random.Range(0, floorList.Count)]);
            yield return interval;
        }

        // For each floor in the scene:
        foreach (Floor floor in floorList)
        {
            // For each room in the floor:
            foreach(Room room in floor.rooms)
            {
                // Enable the randomised scenery for that room:
                EnableScenery(room);
            }

            // Do the same for the storerooms, which are kept in a separate list:
            foreach(Room room in floor.storerooms)
            {
                EnableScenery(room);
            }
            // Populate the List of item spawn points with the relevant transforms:
            PopulateItemSpawnLists(floor);
        }

        // For each floor in the scene:
        foreach(Floor floor in floorList)
        {
            // For each door in the floor:
            foreach(Doorway door in floor.doorways)
            {
                // Check to see if any duplicate doors have spawned (Currently non-functional):
                CheckDoorOverlap(door);
            }
        }

        // Spawn items into the generated level:
        objSpawn.PlaceObjectives();
        objSpawn.SpawnStoreroomItems();

        // Hide the main menu:
        menuHandler.HideMainMenu();

        //SpawnEnemy(exitRoom);

        // Finally, instantiate the player:
        player = Instantiate(playerPrefab);
        player.transform.position = startRoom.playerSpawnPoint.position;
        player.transform.rotation = startRoom.playerSpawnPoint.rotation;
        // Activate the player's heads-up-display:
        player.GetComponent<PlayerHUD>().InitialiseHUD();
        // Instantiate the background music object, an empty object which follows the player and plays the ambient soundtrack:
        GameObject bgm = Instantiate(BGMprefab);
    }

    // This function creates and returns a new instance of a floor in the scene:
    Floor CreateNewFloor(int floorNum)
    {
        Floor newFloor = Instantiate(floorPrefab);
        newFloor.transform.parent = this.transform;
        newFloor.floorNumber = floorNum;

        return newFloor;
    }

    // Spawn the start room:
    void SpawnStartRoom(Floor floor)
    {
        startRoom = Instantiate(roomStartPrefab) as RoomStart;
        startRoom.transform.parent = floor.transform;

        // Add the available doors in the room to the floor's master list of doors:
        AddDoorsToList(startRoom, ref floor.doorways);

        startRoom.transform.position = Vector3.zero;
        startRoom.transform.rotation = Quaternion.identity;

        startRoom.GetComponentInChildren<MeshCombiner>().AdvancedMerge();
        startRoom.GetComponent<RoomStart>().meshCol = startRoom.transform.GetChild(0).GetComponent<MeshCollider>();

        floor.rooms.Add(startRoom);
        startRoom.floorNumber = floor.floorNumber;
    }

    void SpawnConnectingRoom(Floor floor)
    {
        Room currentRoom = Instantiate(connectingRoomPrefabList[Random.Range(0, connectingRoomPrefabList.Count)]) as Room;
        currentRoom.transform.parent = floor.transform;

        // Create a list of the available doors on the current floor that a new room can be placed at:
        List<Doorway> availableDoorsInFloor = new List<Doorway>(floor.doorways);
        // Create a list of the doorways inside the room we want to place:
        List<Doorway> currentRoomDoorways = new List<Doorway>();
        // Add the doors in the current room to the newly-created list of available doors on the current floor:
        AddDoorsToList(currentRoom, ref floor.doorways);
        // Add the doors in the current room to their newly-created list:
        AddDoorsToList(currentRoom, ref currentRoomDoorways);

        bool roomIsSafeToSpawn = false;

        currentRoom.GetComponentInChildren<MeshCombiner>().AdvancedMerge();
        currentRoom.GetComponent<Room>().meshCol = currentRoom.transform.GetChild(0).GetComponent<MeshCollider>();

        // For each available door on the current floor:
        foreach (Doorway availableDoor in availableDoorsInFloor)
        {
            // We test to see if the current room can be safely placed at that available door:
            foreach (Doorway currentDoor in currentRoomDoorways)
            {
                OrientRoomToDoorway(ref currentRoom, currentDoor, availableDoor);

                if (CheckRoomOverlap(currentRoom))
                {
                    continue;
                }

                // If there is no overlap, the room is safe to spawn:
                roomIsSafeToSpawn = true;

                // Add the spawned room to the list of rooms on the current floor:
                floor.rooms.Add(currentRoom);
                currentRoom.floorNumber = floor.floorNumber;

                // Remove both the door we are testing, and the target door to place it at, from the list of available doors:
                floor.doorways.Remove(currentDoor);
                floor.doorways.Remove(availableDoor);
                // Deactivate the door gameobjects to allow the player passage:
                currentDoor.openable = true;
                availableDoor.gameObject.SetActive(false);

                break;
            }

            if (roomIsSafeToSpawn)
            {
                break;
            }
        }

        // If the room is not safe to spawn, destroy it and start generation again:
        if (!roomIsSafeToSpawn)
        {
            Destroy(currentRoom.gameObject);
            ResetLevelGeneration();
        }
    }

    void SpawnUniqueRoom(Floor floor)
    {
        int uniqueRoomAddress = Random.Range(0, uniqueRoomsToSpawn.Count);
        Room uniqueRoom = Instantiate(uniqueRoomsToSpawn[uniqueRoomAddress]) as RoomUnique;
        uniqueRoom.transform.parent = floor.transform;

        // Create a list of the available doors on the current floor that a new room can be placed at:
        List<Doorway> availableDoorsInFloor = new List<Doorway>(floor.doorways);
        // Create a list of the doorways inside the room we want to place:
        List<Doorway> currentRoomDoorways = new List<Doorway>();
        // Add the doors in the current room to the newly-created list of available doors on the current floor:
        AddDoorsToList(uniqueRoom, ref floor.doorways);
        // Add the doors in the current room to their newly-created list:
        AddDoorsToList(uniqueRoom, ref currentRoomDoorways);

        bool roomIsSafeToSpawn = false;

        uniqueRoom.GetComponentInChildren<MeshCombiner>().AdvancedMerge();
        uniqueRoom.GetComponent<Room>().meshCol = uniqueRoom.transform.GetChild(0).GetComponent<MeshCollider>();

        // For each available door on the current floor:
        foreach (Doorway availableDoor in availableDoorsInFloor)
        {
            // We test to see if the current room can be safely placed at that available door:
            foreach (Doorway currentDoor in currentRoomDoorways)
            {
                OrientRoomToDoorway(ref uniqueRoom, currentDoor, availableDoor);

                if (CheckRoomOverlap(uniqueRoom))
                {
                    continue;
                }

                // If there is no overlap, the room is safe to spawn:
                roomIsSafeToSpawn = true;

                // Add the spawned room to the list of rooms on the current floor:
                floor.rooms.Add(uniqueRoom);
                uniqueRoom.floorNumber = floor.floorNumber;

                // Remove both the door we are testing, and the target door to place it at, from the list of available doors:
                floor.doorways.Remove(currentDoor);
                floor.doorways.Remove(availableDoor);
                // Deactivate the door gameobjects to allow the player passage:
                currentDoor.openable = true;
                availableDoor.gameObject.SetActive(false);

                uniqueRoomsToSpawn.Remove(uniqueRoomsToSpawn[uniqueRoomAddress]);

                break;
            }

            if (roomIsSafeToSpawn)
            {
                break;
            }
        }

        if (!roomIsSafeToSpawn)
        {
            Destroy(uniqueRoom.gameObject);
            ResetLevelGeneration();
        }
    }

    void SpawnStairwell(Floor floor)
    {
        Room stairwellRoom = Instantiate(roomStairwellPrefab) as RoomStairwell;
        stairwellRoom.transform.parent = floor.transform;

        List<Doorway> availableDoorsInFloor = new List<Doorway>(floor.doorways);
        // By always using the 0th index of the stairwell's doorway list, we ensure that all stairwells spawn heading
        // in the same direction (By default, going up):
        Doorway goingUp = stairwellRoom.doorways[0];

        bool roomIsSafeToSpawn = false;

        stairwellRoom.GetComponentInChildren<MeshCombiner>().AdvancedMerge();
        stairwellRoom.GetComponent<RoomStairwell>().meshCol = stairwellRoom.transform.GetChild(0).GetComponent<MeshCollider>();

        // For each available door on the current floor:
        foreach (Doorway availableDoorway in availableDoorsInFloor)
        {
            Room room = stairwellRoom;

            // Test to see if the stairwell can be safely placed:
            OrientRoomToDoorway(ref room, goingUp, availableDoorway);

            if (CheckRoomOverlap(stairwellRoom))
            {
                continue;
            }

            roomIsSafeToSpawn = true;

            floor.rooms.Add(stairwellRoom);
            stairwellRoom.floorNumber = floor.floorNumber;

            floor.doorways.Remove(goingUp);
            floor.doorways.Remove(availableDoorway);
            goingUp.openable = true;
            availableDoorway.gameObject.SetActive(false);

            currentFloorNum++;

            break;
        }

        if (!roomIsSafeToSpawn)
        {
            Destroy(stairwellRoom.gameObject);
            ResetLevelGeneration();
        }
    }

    void SpawnStoreroom(Floor floor)
    {
        Room storeroom = Instantiate(roomStoreroomPrefab) as RoomStoreroom;
        storeroom.transform.parent = floor.transform;

        List<Doorway> availableDoorsInFloor = new List<Doorway>(floor.doorways);
        Doorway storeroomDoor = storeroom.doorways[0];

        bool roomIsSafeToSpawn = false;

        storeroom.GetComponentInChildren<MeshCombiner>().AdvancedMerge();
        storeroom.GetComponent<RoomStoreroom>().meshCol = storeroom.transform.GetChild(0).GetComponent<MeshCollider>();

        foreach (Doorway availableDoorway in availableDoorsInFloor)
        {
            Room room = storeroom;

            OrientRoomToDoorway(ref room, storeroomDoor, availableDoorway);

            if (CheckRoomOverlap(storeroom))
            {
                continue;
            }

            roomIsSafeToSpawn = true;

            floor.storerooms.Add(storeroom);
            storeroom.floorNumber = floor.floorNumber;

            floor.doorways.Remove(storeroomDoor);
            floor.doorways.Remove(availableDoorway);
            availableDoorway.gameObject.SetActive(false);
            storeroomDoor.openable = true;

            break;
        }

        if (!roomIsSafeToSpawn)
        {
            Destroy(storeroom.gameObject);
            ResetLevelGeneration();
        }
    }

    void SpawnExitRoom(Floor floor)
    {
        exitRoom = Instantiate(roomExitPrefab) as RoomExit;
        exitRoom.transform.parent = floor.transform;

        List<Doorway> availableDoorsInFloor = new List<Doorway>(floor.doorways);
        Doorway exitDoor = exitRoom.doorways[0];

        bool roomIsSafeToSpawn = false;

        exitRoom.GetComponentInChildren<MeshCombiner>().AdvancedMerge();
        exitRoom.GetComponent<RoomExit>().meshCol = exitRoom.transform.GetChild(0).GetComponent<MeshCollider>();

        foreach (Doorway availableDoorway in availableDoorsInFloor)
        {
            Room room = exitRoom;

            OrientRoomToDoorway(ref room, exitDoor, availableDoorway);

            if (CheckRoomOverlap(exitRoom))
            {
                continue;
            }

            roomIsSafeToSpawn = true;

            floor.rooms.Add(exitRoom);
            exitRoom.floorNumber = floor.floorNumber;

            floor.doorways.Remove(exitDoor);
            floor.doorways.Remove(availableDoorway);
            availableDoorway.gameObject.SetActive(false);
            exitDoor.openable = true;

            break;
        }

        if (!roomIsSafeToSpawn)
        {
            Destroy(exitRoom.gameObject);
            ResetLevelGeneration();
        }
    }

    // If random scenery is enabled, this function will check the list of possible scenery configurations for a room and
    //randomly activate one of them:
    void EnableScenery(Room room)
    {
        if (room.ScenerySets.Count > 0)
        {
            foreach (GameObject set in room.ScenerySets)
            {
                set.SetActive(false);
            }

            if (randomScenery)
            {
                int randomSet = Random.Range(0, room.ScenerySets.Count);
                room.ScenerySets[randomSet].SetActive(true);
            }
            else
            {
                room.ScenerySets[0].SetActive(true);
            }
        }
    }

    // This function checks each room for the spawn points that have been enabled alongside its scenery and adds them to a list:
    void PopulateItemSpawnLists(Floor floor)
    {
        foreach(Room room in floor.rooms)
        {
            if (room.GetComponentInChildren<ItemSpawnPoint>())
            {
                floor.itemSpawnPoints.Add(room.GetComponentInChildren<ItemSpawnPoint>());
            }
        }
        // We do the same for each storeroom:
        foreach(Room storeroom in floor.storerooms)
        {
            if (storeroom.GetComponentInChildren<ItemSpawnPoint>())
            {
                storeroomSpawnPoints.Add(storeroom.GetComponentInChildren<ItemSpawnPoint>());
            }
        }
    }

    // This function spawns an NPC enemy in a room provided the room has a spawn point:
    void SpawnEnemy(Room room)
    {
        int randomSpawnAddress = Random.Range(0, room.actorSpawns.Count);
        ActorSpawnPoint randomSpawn = room.actorSpawns[randomSpawnAddress];

        Instantiate(enemyPrefab.gameObject, randomSpawn.transform.position, randomSpawn.transform.rotation);
    }

    // This function adds the doorways in a room to the provided list:
    void AddDoorsToList(Room room, ref List<Doorway> list)
    {
        foreach (Doorway door in room.doorways)
        {
            int r = Random.Range(0, list.Count);
            list.Insert(r, door);
        }
    }

    // This function orients a newly-placed room to a target doorway so that they connect:
    void OrientRoomToDoorway(ref Room room, Doorway roomDoorway, Doorway targetDoorway)
    {
        room.transform.position = Vector3.zero;
        room.transform.rotation = Quaternion.identity;

        Vector3 targetDoorwayEuler = targetDoorway.transform.eulerAngles;
        Vector3 roomDoorwayEuler = roomDoorway.transform.eulerAngles;
        float deltaAngle = Mathf.DeltaAngle(roomDoorwayEuler.y, targetDoorwayEuler.y);
        Quaternion currentRoomTargetRotation = Quaternion.AngleAxis(deltaAngle, Vector3.up);
        room.transform.rotation = currentRoomTargetRotation * Quaternion.Euler(0f, 180f, 0f);

        Vector3 roomPositionOffset = roomDoorway.transform.position - room.transform.position;
        room.transform.position = targetDoorway.transform.position - roomPositionOffset;
    }

    // Returns true if there is any overlap between rooms:
    bool CheckRoomOverlap(Room room)
    {
        // Get a reference to the bounds of the current room:
        Bounds bounds = room.GetRoomBounds;
        // By shrinking the bounds just slightly, we allow rooms to be placed touching each other but NOT overlapping:
        bounds.Expand(-0.1f);

        // Store an array of colliders, containing all colliders within the room's bounds that occupy the 'ProceduralRoom' layer:
        Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.size / 2, room.transform.rotation, roomLayerMask);
        // If colliders using the 'ProceduralRoom' layer are detected:
        if (colliders.Length > 0)
        {
            // For each collider in the array:
            foreach (Collider col in colliders)
            {
                // If the collider is part of the room currently being placed, ignore it:
                if (col.transform.parent.gameObject.Equals(room.gameObject))
                {
                    continue;
                }
                // Else if the collider is part of another room, then there will be an overlap and we will not place the room here:
                else
                {
                    return true;
                }
            }
        }

        return false;
    }


    // This function checks to see if any duplicate doors have spawned (Currently non-functional):
    bool CheckDoorOverlap(Doorway door)
    {
        Bounds bounds = door.GetDoorwayBounds;

        Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.size / 2, door.transform.rotation, doorwayLayerMask);

        if (colliders.Length > 0)
        {
            foreach(Collider col in colliders)
            {
                if (col.transform.parent.gameObject.Equals(door.gameObject))
                {
                    continue;
                }
                else
                {
                    return true;
                }
            }
        }

        Debug.Log("No door overlap detected");
        return false;
    }

    // If we encounter an error, this function restarts level generation from scratch:
    void ResetLevelGeneration()
    {
        StopCoroutine("GenerateLevel");

        // Delete all rooms currently in the scene:
        if (startRoom)
        {
            Destroy(startRoom.gameObject);
        }

        if (exitRoom)
        {
            Destroy(exitRoom.gameObject);
        }

        foreach (Floor floor in floorList)
        {
            foreach (Room room in floor.rooms)
            {
                Destroy(room.gameObject);
            }

            floor.rooms.Clear();
            floor.doorways.Clear();
            Destroy(floor.gameObject);
        }

        // Clear the list of currently-spawned floors:
        floorList.Clear();
        currentFloorNum = 0;

        // Begin the level generation again:
        StartCoroutine("GenerateLevel");
    }
}