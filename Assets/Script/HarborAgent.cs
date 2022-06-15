using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class HarborAgent : MonoBehaviour
{
    //Terrain data
    private Terrain _terrain;
    private TerrainData _td;
    private int _x;
    private int _y;
    private Vector3 _terrainPos;
    
    // Agent data
    public int agentNr;
    public int token;
    public int timesSearchingPoint;
    [Range(5, 50)] public int distance;
    [Range(5, 15)] public int harborLenght;
    
    // Harbor prefab
    public GameObject harbor;
    
    // Coastline points
    private List<Vector2> _coastlinePoints;
    
    // Visited location
    private List<Vector2> _visitedLocation;

    // OnDrawGizmos
    bool _start;
    private readonly List<Vector3> _visitedPoint = new List<Vector3>();
    private readonly List<Vector3> _locations = new List<Vector3>();

    // nearby point
    private readonly Vector2[] _nearbyPoint =
    {
        Vector2.right,
        Vector2.left,
        Vector2.down,
        Vector2.up,
        Vector2.one,
        -Vector2.one,
        new Vector2(-1, 1),
        new Vector2(1, -1)
    };
    
    // Instance of this class
    public static HarborAgent Instance;

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
        // Getting terrain information
        _terrain = GetComponent<Terrain>();
        _td = _terrain.terrainData;
        _x = _td.heightmapResolution;
        _y = _td.heightmapResolution;
        _terrainPos = _terrain.GetPosition();

        // StartCoroutine(Action());
    }

    private void OnDrawGizmos()
    {
        if (_start)
        {
            foreach (var point in _visitedPoint)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(point, .2f);
            }
            
            foreach (var location in _locations)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(location, .2f);
                Gizmos.DrawWireCube(location, Vector3.one * harborLenght); 
            }
        }
    }

    public IEnumerator Action()
    {
        // Debug
        _start = true;
        
        // All coastline points
        _coastlinePoints = ValidPoints();
        
        // List used for keeping truck already visited point for a single agent
        _visitedLocation = new List<Vector2>();
        
        Debug.Log("Starting placing Harbors");
        
        for (int i = 0; i < agentNr; i++)
        {
            List<Vector2> candidates = new List<Vector2>();
            
            // Place agent on a coastline point
            Vector2 location = GetStartingPoint(candidates);
            
            // The agent can't find a point where it is possible to place the harbor
            if (location == -Vector2.one)
            {
                Debug.Log("No more point available where to place harbor...");
                _visitedLocation.Clear();
                break;
            }
            
            //Place the harbor
            PlaceHarbor(location, candidates);
            Physics.SyncTransforms();
        
            // variable used for scoring point in order to have the agent moving in a certain direction 
            Vector2 attractor = GetAttractor();
            Vector2 repulsor = GetRepulsor(attractor, location);
        
            for (int j = 0; j < token - 1; j++)
            {
                // Move agent to the new location and place harbor
                location = FindNewLocation(location, attractor, repulsor);
                
                // No location was found
                if (location == -Vector2.one)
                {
                    Debug.Log("No more point available where to place harbor...");
                    location = RandomPoint();
                }
            }
            
            _visitedLocation.Clear();
        
            yield return new WaitForEndOfFrame();
        }
        
        Debug.Log("Visited point: " + _visitedPoint.Count);

        yield return new WaitForEndOfFrame();

        GameObject[] harbors = GameObject.FindGameObjectsWithTag("Harbor");
        Debug.Log("Number of harbor placed: " + harbors.Length);
        
        Debug.Log("Finished placing harbors...");
    }
    
    private List<Vector2> ValidPoints()
    {
        List<Vector2> candidates = new List<Vector2>();
        
        for (int i = 0; i < _x; i++)
        {
            for (int j = 0; j < _y; j++)
            {
                Vector2 tmp = new Vector2(i, j);
                if (GetHeight(tmp) >= .03f && CheckNearPoint(tmp))
                {
                    candidates.Add(tmp);
                }
            }
        }

        return candidates;
    }
    
    private bool CheckNearPoint(Vector2 location)
    {
        foreach (Vector2 point in _nearbyPoint)
        {
            if (IsInsideTerrain(location + point) && GetHeight(location + point) < .03f)
            {
                return true;
            }
        }

        return false;
    }

    private Vector2 GetStartingPoint(List<Vector2> candidates)
    {
        Vector2 location = RandomPoint();

        for (int i = 0; i < timesSearchingPoint; i++)
        {
            if (CheckLocation(location, candidates))
            {
                return location;
            }

            location = RandomPoint();
        }
        
        return -Vector2.one;
    }

    private Vector2 FindNewLocation(Vector2 location, Vector2 attractor, Vector2 repulsor)
    {
        // Move agent to the new location
        for (int k = 0; k < distance; k++)
        {
            location = NewLocation(location, attractor, repulsor);
            
            // Agent get stuck
            if (location == -Vector2.one)
            {
                _visitedLocation.Clear();
                location = RandomPoint();
                break;
            }
            
            // Debug
            if (k == distance - 1)
            {
                _visitedLocation.Add(location);
                break;
            }
            _visitedPoint.Add(GetPoint(location));
            _visitedLocation.Add(location);
        }
        
        // Check if the location has a valid directions where to place harbor otherwise agent continue walking until it will find valid point or get stuck
        List<Vector2> candidates = new List<Vector2>();

        for (int i = 0; i < timesSearchingPoint; i++)
        {
            if (CheckLocation(location, candidates))
            {
                PlaceHarbor(location, candidates);
                Physics.SyncTransforms();
                return location;
            }
        
            location = NewLocation(location, attractor, repulsor);
        
            if (location == -Vector2.one)
            {
                location = RandomPoint();
            }
            
            _visitedPoint.Add(GetPoint(location));
            _visitedLocation.Add(location);
        }
        
        return -Vector2.one;
    }
    
    private Vector2 NewLocation(Vector2 location, Vector2 attractor, Vector2 repulsor)
    {
        List<Vector2> candidates = new List<Vector2>();

        foreach (var point in _nearbyPoint)
        {
            if (IsInsideTerrain(location + point) && GetHeight(location + point) >= .03f &&
                CheckNearPoint(location + point) && !_visitedLocation.Contains(location + point))
            {
                candidates.Add(location + point);
            }
        }

        if (candidates.Count == 0)
        {
            Debug.Log("Agent stuck, move it to a random coastline point");
            return -Vector2.one;
        }

        List<float> scores = new List<float>();

        foreach (var candidate in candidates)
        {
            scores.Add(ScorePoint(candidate, attractor, repulsor));
        }

        return candidates[scores.IndexOf(scores.Max())];
    }

    private bool CheckLocation(Vector2 location, List<Vector2> candidates)
    {
        int collisions = Physics.OverlapBoxNonAlloc(GetPoint(location), Vector3.one * (harborLenght * .5f), new Collider[1], Quaternion.identity, LayerMask.GetMask("Harbor"));
        
        if (collisions > 0)
        {
            return false;
        }
        
        foreach (Vector2 dir in _nearbyPoint)
        {
            if (CheckNearbyPoint(location, location + dir))
            {
                candidates.Add(location + dir);
            }
        }

        return candidates.Count != 0;
    }
    
    private bool CheckNearbyPoint(Vector2 location, Vector2 candidate)
    {
        // Check if the point is a sea point
        if (GetHeight(candidate) != 0)
        {
            return false;
        }

        Vector2 dir = (candidate - location).normalized;
        Vector2 perpendicularDir = Vector2.Perpendicular(dir);

        for (int i = 1; i < harborLenght; i++)
        {
            Vector2 pointToCheck = candidate + dir * i;

            if (!IsInsideTerrain(pointToCheck))
            {
                return false;
            }

            Vector2 right = pointToCheck + perpendicularDir;
            Vector2 left = pointToCheck - perpendicularDir;

            // Check if the point and its right and left points are sea point   
            if (GetHeight(pointToCheck) != 0 || GetHeight(right) != 0 || GetHeight(left) != 0)
            {
                return false;
            }
            
            // Check if the harbor collide with other one
            bool check = Physics.Raycast(new Vector3(pointToCheck.x, 100, pointToCheck.y), Vector3.down, Mathf.Infinity,
                LayerMask.GetMask("Harbor"));
            if (check)
            {
                return false;
            }
        }

        return true;
    }

    private float GetHeight(Vector2 location)
    {
        //Create origin for raycast that is above the terrain. I chose 200.
        Vector3 origin = new Vector3(location.x + _terrainPos.x, 200, location.y + _terrainPos.z);

        //Send the raycast.
        Physics.Raycast(origin, Vector3.down, out var hit, Mathf.Infinity, LayerMask.GetMask("Terrain"));

        return hit.point.y;
    }
    
    private Vector3 GetPoint(Vector2 location)
    {
        return new Vector3(location.x + _terrainPos.x, GetHeight(location), location.y + _terrainPos.z);
    }

    private bool IsInsideTerrain(Vector2 point)
    {
        return point.x >= 0 && point.x <= _x - 1 && point.y >= 0 && point.y <= _y - 1;
    }
    
    private Vector2 RandomPoint()
    {
        return _coastlinePoints[Random.Range(0, _coastlinePoints.Count)];
    }
    
    private Vector2 GetAttractor()
    {
        return new Vector2(Random.Range(0, _x), Random.Range(0, _y));
    }

    private Vector2 GetRepulsor(Vector2 attractor, Vector2 location)
    {
        Vector2 attractorDirection = (attractor - location).normalized;

        //Calculating repulsor
        Vector2 repulsor = new Vector2(Random.Range(0, _x), Random.Range(0, _y));
        Vector2 repulsorDirection = (repulsor - location).normalized;

        while (attractorDirection == repulsorDirection)
        {
            repulsor = new Vector2(Random.Range(0, _x), Random.Range(0, _y));
            repulsorDirection = (repulsor - location).normalized;
        }

        return repulsor;
    }
    
    private float ScorePoint(Vector2 point, Vector2 attractor, Vector2 repulsor)
    {
        return Mathf.Pow(Vector2.Distance(point, repulsor), 2.0f) - Mathf.Pow(Vector2.Distance(point, attractor), 2.0f);
    }

    private void PlaceHarbor(Vector2 location, List<Vector2> candidates)
    {
        //Debug
        DrawDir(location, candidates);
        _locations.Add(GetPoint(location));
        
        Vector3 worldLocation = GetPoint(new Vector2(location.x, location.y));
        Vector3 candidatesWorldLocation = GetPoint(candidates[Random.Range(0, candidates.Count)]);

        Vector3 dir = (candidatesWorldLocation - worldLocation).normalized;
        dir.y = 0;

        Quaternion rotHarbor = Quaternion.LookRotation(dir, Vector3.up);
        float randomLenght = Random.Range(5, this.harborLenght);
        Vector3 scaleHarbor = new Vector3(1, 1, randomLenght);

        var tmpHarbor = Instantiate(harbor, worldLocation, rotHarbor);
        tmpHarbor.transform.localScale = scaleHarbor;
        tmpHarbor.transform.Translate(Vector3.forward * (randomLenght * .5f));
    }
    
    private void DrawDir(Vector2 location, List<Vector2> candidates)
    {
        foreach (Vector2 candidate in candidates)
        {
            Vector3 worldLocation = GetPoint(location);
            Vector3 worldCandidate = GetPoint(candidate);

            Vector3 dir = (worldCandidate - worldLocation).normalized;
            dir.y = 0f;
            Debug.DrawRay(worldLocation, dir * harborLenght, Color.red, 50f);
        }
    }
}
