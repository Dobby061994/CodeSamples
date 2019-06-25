using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawningMk2 : MonoBehaviour
{
    // Public variables for the items to spawn, and Lists to track what has been spawned:
    public List<ObjectiveItem> objectiveItemList = new List<ObjectiveItem>();
    public List<Item> rareItemList = new List<Item>();
    public Keycard keycardPrefab;
    public List<ObjectiveItem> spawnedObjectives = new List<ObjectiveItem>();
    public List<Keycard> spawnedKeycards = new List<Keycard>();
    public List<ItemSpawnPoint> storeroomSpawnPoints = new List<ItemSpawnPoint>();
    LevelGeneratorMk2 lvlGen;

    private void Start()
    {
        lvlGen = GetComponent<LevelGeneratorMk2>();
    }

    public void PlaceObjectives()
    {
        // While there are still objective items to spawn:
        while (objectiveItemList.Count > 0)
        {
            // Select a random spawn point on a random floor:
            Floor randomFloor = SelectRandomFloor();
            ItemSpawnPoint randomSpawnPoint = SelectRandomSpawnPoint(randomFloor);

            if (randomSpawnPoint != null)
            {
                // Spawn the item:
                ObjectiveItem currentItem = objectiveItemList[Random.Range(0, objectiveItemList.Count)];

                Instantiate(currentItem, randomSpawnPoint.transform.position, randomSpawnPoint.transform.rotation, randomSpawnPoint.transform);

                randomFloor.itemSpawnPoints.Remove(randomSpawnPoint);
                objectiveItemList.Remove(currentItem);
                spawnedObjectives.Add(currentItem);
            }
            else if (randomSpawnPoint == null)
            {
                // Do nothing. The while loop will continue running until it finds an available spawn point.
                Debug.Log("Floor has no available spawn points. Trying another floor.");
            }
        }

        // Spawn a single keycard somewhere in the level for access to locked rooms:
        Floor randomFloor2 = SelectRandomFloor();
        ItemSpawnPoint randomSpawnPoint2 = SelectRandomSpawnPoint(randomFloor2);

        Keycard keycard = keycardPrefab;

        Instantiate(keycardPrefab, randomSpawnPoint2.transform.position, randomSpawnPoint2.transform.rotation, randomSpawnPoint2.transform);

        randomFloor2.itemSpawnPoints.Remove(randomSpawnPoint2);
        spawnedKeycards.Add(keycard);
    }

    // Spawn the items which only appear in the locked storerooms:
    public void SpawnStoreroomItems()
    {
        storeroomSpawnPoints = lvlGen.storeroomSpawnPoints;

        ItemSpawnPoint randomSpawnPoint = storeroomSpawnPoints[Random.Range(0, storeroomSpawnPoints.Count)];

        if (randomSpawnPoint != null)
        {
            Item powerCoil = rareItemList[Random.Range(0, rareItemList.Count)];

            Instantiate(powerCoil, randomSpawnPoint.transform.position, randomSpawnPoint.transform.rotation, randomSpawnPoint.transform);

            storeroomSpawnPoints.Remove(randomSpawnPoint);
        }
        else
        {
            Debug.Log("Couldn't find a storeroom spawn point :(");
        }
    }

    // Returns a random storeroom on a given floor:
    Room SelectRandomStoreroom(Floor floor)
    {
        int randomRoomAddress = Random.Range(0, floor.storerooms.Count);
        Room randomStoreroom = floor.storerooms[randomRoomAddress];

        return randomStoreroom;
    }

    Floor SelectRandomFloor()
    {
        // Select a random floor from the list of spawned floors:
        int randomFloorAddress = Random.Range(0, lvlGen.floorList.Count);
        Floor randomFloor = lvlGen.floorList[randomFloorAddress];

        // Return that random floor:
        return randomFloor;
    }

    ItemSpawnPoint SelectRandomSpawnPoint(Floor floor)
    {

        // If the room has an available spawn point:
        if (CheckForAvailableSpawnPoint(floor))
        {
            // Select a random spawn point from the list of available spawns:
            ItemSpawnPoint randomSpawnPoint = floor.itemSpawnPoints[Random.Range(0, floor.itemSpawnPoints.Count)];
            // Return that random spawn point:
            return randomSpawnPoint;
        }
        // Else if the room has no available spawn points:
        else
        {
            // Do nothing. The PlaceAllObjectives() while loop will continue running until it finds a room with an available spawn point.
            return null;
        }
    }

    bool CheckForAvailableSpawnPoint(Floor floor)
    {
        // If the floor DOES have an available spawn point:
        if (floor.itemSpawnPoints.Count > 0)
        {
            // Return true:
            return true;
        }
        // Else if the floor has no available spawn points:
        else
        {
            // Return false:
            return false;
        }
    }
}