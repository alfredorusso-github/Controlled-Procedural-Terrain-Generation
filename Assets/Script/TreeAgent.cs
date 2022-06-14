using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class TreeAgent : MonoBehaviour
{
    //Terrain data
    private Terrain _terrain;
    private TerrainData _td;
    private int _x;
    private int _y;
    private float[,] _heightmap;

    // Agent data
    public int agentNr;
    public int token;
    public int returnValue;
    [Range(5, 10)] public int distance;

    // Tree prefab 
    public GameObject tree;

    // Valid Points
    private List<Vector2Int> _validPoints;

    // OnDrawGizmos
    bool _start;

    // nearby point
    private readonly Vector2Int[] _nearbyPoint = {
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
        _terrain = GetComponent<Terrain>();
        _td = _terrain.terrainData;
        _x = _td.heightmapResolution;
        _y = _td.heightmapResolution;

        //Initialize heightmap
        _heightmap = new float[_x, _y];
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (_start)
        {
            foreach (Vector2Int point in _validPoints)
            {
                Gizmos.DrawSphere(GetPoint(point), .2f);
            }
        }
    }

    private Vector3 GetPoint(Vector2 location)
    {
        //Create origin for raycast that is above the terrain
        Vector3 origin = new Vector3(location.x, _td.size.y + 10, location.y);

        //Send the raycast.
        Physics.Raycast(origin, Vector3.down, out var hit, Mathf.Infinity, LayerMask.GetMask("Terrain"));

        return hit.point;
    }

    // Make the agent come back to the starting position is done in order to add details to a specific zone of the map, avoiding placing tree in a completing randomly way
    public IEnumerator Action()
    {
        _heightmap = _td.GetHeights(0, 0, _x, _y);

        _validPoints = ValidPoints();

        Vector3 terrainPos = _terrain.GetPosition();

        for (int i = 0; i < agentNr; i++)
        {
            Vector2Int startingPoint = RandomStartingPoint();
            
            for (int j = 0; j < token; j++)
            {
                // Setting agent position to the starting point
                Vector2Int candidate = startingPoint;

                // Place trees until the return value is reached
                for (int k = 0; k < returnValue; k++)
                {
                    // Check if there is a candidate point where agent can move
                    Vector2Int checkCandidate = GetNearbyPoint(candidate);
                    if (candidate == checkCandidate)
                    {
                        // There are no more near point where it is possible to place tree
                        Debug.Log("Agent can't find a valid point where to place tree");
                        break;
                    }

                    // Move agent in random direction in the nearby point
                    candidate = checkCandidate;
                    
                    // Place tree
                    Instantiate(tree, GetPoint(new Vector2(terrainPos.x + candidate.x, terrainPos.z + candidate.y)), Quaternion.identity);
                }

                yield return new WaitForEndOfFrame();
            }
        }

        yield return new WaitForEndOfFrame();
    }

    private Vector2Int RandomStartingPoint()
    {
        return _validPoints[Random.Range(0, _validPoints.Count)];
    }

    private List<Vector2Int> ValidPoints()
    {
        float ah = AverageHeight();
        Debug.Log("Average height of the island: " + ah);

        List<Vector2Int> validPoints = new List<Vector2Int>();

        for (int i = 0; i < _x; i++)
        {
            for (int j = 0; j < _y; j++)
            {
                if (_heightmap[j, i] > ah + .05f && CheckSteepness(new Vector2(i, j)))
                {
                    validPoints.Add(new Vector2Int(i, j));
                }
            }
        }

        return validPoints;
    }

    private float AverageHeight()
    {

        float sum = 0.0f;
        int nPoints = 0;

        for (int i = 0; i < _x; i++)
        {
            for (int j = 0; j < _y; j++)
            {
                if (_heightmap[j, i] > 0.01f)
                {
                    sum += _heightmap[j, i];
                    nPoints++;
                }
            }
        }

        return sum / nPoints;
    }

    private bool CheckSteepness(Vector2 location)
    {
        //Create origin for raycast that is above the terrain.S
        Vector3 origin = new Vector3(location.x, _td.size.y + 10, location.y);

        //Send the raycast.
        Physics.Raycast(origin, Vector3.down, out var hit);

        return Vector3.Angle(hit.normal, Vector3.up) < 15f;
    }

    private Vector2Int GetNearbyPoint(Vector2Int location)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        int randomDistance = Random.Range(5, distance);

        foreach (Vector2Int point in _nearbyPoint)
        {
            if (IsValidPoint(location + point * randomDistance))
            {
                candidates.Add(location + point * randomDistance);
            }
        }

        return candidates.Count != 0 ? candidates[Random.Range(0, candidates.Count)] : location;
    }

    private bool IsValidPoint(Vector2Int location)
    {
        // Check if the location is inside the terrain
        if (!(location.x >= 0 && location.x <= (_x - 1)) || !(location.y >= 0 && location.y <= (_y - 1)))
        {
            return false;
        }

        if (!CheckSteepness(location))
        {
            return false;
        }

        Vector3 worldLocation = GetPoint(location);
        int collisions = Physics.OverlapBoxNonAlloc(worldLocation, tree.GetComponent<BoxCollider>().size * .5f,
            new Collider[1], Quaternion.identity, ~LayerMask.GetMask("Terrain"));
        
        return collisions == 0;
    }
}
