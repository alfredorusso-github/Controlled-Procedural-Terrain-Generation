using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TreeAgent : MonoBehaviour
{
    //Terrain data
    private Terrain terrain;
    private TerrainData td;
    private int x;
    private int y;
    private float[,] heightmap;

    // Agent data
    public int AgentNr;
    public int Token;
    public int ReturnValue;
    [Range(2, 8)] public int distance;

    // Tree prefab 
    public GameObject tree;

    // Valid Points
    List<Vector2Int> validPoints;

    // OnDrawGizmos
    bool start;

    // nearby point
    Vector2Int[] nearbyPoint = new Vector2Int[]{
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.down,
        Vector2Int.up,
        Vector2Int.one,
        -Vector2Int.one,
        new Vector2Int(-1, 1),
        new Vector2Int(1, -1)
    };

    // Instance of this class
    public static TreeAgent Instance;

    private void Awake()
    {

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

    }

    void Start()
    {
        //Getting terrain information
        terrain = GetComponent<Terrain>();
        td = terrain.terrainData;
        x = td.heightmapResolution;
        y = td.heightmapResolution;

        //Initialize heighmap
        heightmap = new float[x, y];
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (start)
        {
            foreach (Vector2Int point in validPoints)
            {
                Gizmos.DrawSphere(GetHeight(point), .2f);
            }
        }
    }

    private Vector3 GetHeight(Vector2 location)
    {
        //Create object to store raycast data
        RaycastHit hit;

        //Create origin for raycast that is above the terrain. I chose 100.
        Vector3 origin = new Vector3(location.x, 100, location.y);

        //Send the raycast.
        Physics.Raycast(origin, Vector3.down, out hit, Mathf.Infinity, LayerMask.GetMask("Terrain"));

        return hit.point;
    }

    // Make the agent come back to the starting position is done in order to add details to a specific zone of the map, avoiding placing tree in a completing randomly manner
    public IEnumerator Action()
    {
        heightmap = td.GetHeights(0, 0, x, y);

        validPoints = new List<Vector2Int>();
        ValidPoints();

        for (int i = 0; i < AgentNr; i++)
        {
            Vector2Int startingPoint = RandomStartingPoint();

            for (int j = 0; j < Token; j++)
            {
                // Setting agent position to the starting point
                Vector2Int candidate = startingPoint;

                // Place trees until the return value is reached
                for (int k = 0; k < ReturnValue; k++)
                {
                    // Place tree
                    tree = Instantiate(tree, GetHeight(candidate), Quaternion.identity);

                    // Check if there is a candidate point where agent can move
                    Vector2Int checkCandidate = GetNearbyPoint(candidate);
                    if (candidate == checkCandidate)
                    {
                        // There are no more near point where it is possible to place tree
                        break;
                    }

                    // Move agent in random direction in the nearby point
                    candidate = checkCandidate;
                }

                yield return new WaitForEndOfFrame();
            }
        }

        yield return new WaitForEndOfFrame();
    }

    private Vector2Int RandomStartingPoint()
    {
        return validPoints[Random.Range(0, validPoints.Count)];
    }

    private void ValidPoints()
    {
        float ah = averageHeight();
        Debug.Log("Average height of the island: " + ah);

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                if (heightmap[j, i] > ah + .05f && checkSteepness(new Vector2(i, j)))
                {
                    validPoints.Add(new Vector2Int(i, j));
                }
            }
        }
    }

    private float averageHeight()
    {

        float sum = 0.0f;
        int nPoints = 0;

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                if (heightmap[j, i] > 0.01f)
                {
                    sum += heightmap[j, i];
                    nPoints++;
                }
            }
        }

        return sum / nPoints;
    }

    private bool checkSteepness(Vector2 location)
    {

        //Create object to store raycast data
        RaycastHit hit;

        //Create origin for raycast that is above the terrain. I chose 100.
        Vector3 origin = new Vector3(location.x, 100, location.y);

        //Send the raycast.
        Physics.Raycast(origin, Vector3.down, out hit);

        if (Vector3.Angle(hit.normal, Vector3.up) < 15f)
        {
            return true;
        }

        return false;
    }

    private Vector2Int GetNearbyPoint(Vector2Int location)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        int RandomDistance = Random.Range(2, distance);

        foreach (Vector2Int point in nearbyPoint)
        {
            if (checkNearbyPoint(location + point * RandomDistance))
            {
                candidates.Add(location + point * RandomDistance);
            }
        }

        if (candidates.Count != 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        return location;
    }

    private bool checkNearbyPoint(Vector2Int location)
    {
        if (!checkSteepness(location))
        {
            return false;
        }

        Vector3 WorldLocation = GetHeight(location);
        Collider[] collider = Physics.OverlapBox(WorldLocation, tree.GetComponent<BoxCollider>().size * .5f, Quaternion.identity, ~LayerMask.GetMask("Terrain"));
        if (collider.Length > 0)
        {
            return false;
        }

        return true;
    }
}
