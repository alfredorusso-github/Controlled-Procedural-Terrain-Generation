using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class HarborAgent : MonoBehaviour
{
    //Terrain data
    private Terrain _terrain;
    private TerrainData _td;
    private int _x;
    private int _y;
    
    // Agent data
    public int agentNr;
    public int token;
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
    
    void Start()
    {
        // Getting terrain information
        _terrain = GetComponent<Terrain>();
        _td = _terrain.terrainData;
        _x = _td.heightmapResolution;
        _y = _td.heightmapResolution;

        StartCoroutine(Action());
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

    private IEnumerator Action()
    {
        // Debug
        _start = true;
        
        // All coastline points
        _coastlinePoints = ValidPoints();
        
        // List used for keeping truck already visited point for a single agent
        _visitedLocation = new List<Vector2>();
        
        for (int i = 0; i < agentNr; i++)
        {
            // Check if the point is a valid one in order to place the harbor
            List<Vector2> candidates = new List<Vector2>();
            Vector2 location = RandomPoint();
            while (!CheckLocation(location, candidates))
            {
                location = RandomPoint();
            }
            PlaceHarbor(location, candidates);
            Physics.SyncTransforms();
            Debug.Log("Starting Point: " + location);
            
            // variable used for scoring point in order to have the agent moving in a certain direction 
            Vector2 attractor = GetAttractor();
            Vector2 repulsor = GetRepulsor(attractor, location);

            for (int j = 0; j < token - 1; j++)
            {
                // Move agent to the new location and place harbor
                location = FindNewLocation(location, attractor, repulsor);
            }
            
            _visitedLocation.Clear();
        }

        Debug.Log("Visited point: " + _visitedPoint.Count);

        yield return new WaitForEndOfFrame();
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
                location = RandomPoint();
                // _locations.Add(GetPoint(location));
                break;
            }
                    
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
        while (!CheckLocation(location, candidates))
        {
            location = NewLocation(location, attractor, repulsor);
            
            // Check if agent get stuck
            if (location == -Vector2.one)
            {
                // move the agent to random coastline point
                location = RandomPoint();
                continue;
            }
            
            _visitedPoint.Add(GetPoint(location));
            _visitedLocation.Add(location);
        }
        
        //Place harbor
        PlaceHarbor(location, candidates);
        Physics.SyncTransforms();

        return location;
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

        if (candidates.Count == 0)
        {
            return false;
        }

        return true;
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
    
    private float GetHeight(Vector2 location)
    {
        //Create origin for raycast that is above the terrain. I chose 200.
        Vector3 origin = new Vector3(location.x, 200, location.y);

        //Send the raycast.
        Physics.Raycast(origin, Vector3.down, out var hit, Mathf.Infinity, LayerMask.GetMask("Terrain"));

        return hit.point.y;
    }
    
    private Vector3 GetPoint(Vector2 location)
    {
        return new Vector3(location.x, GetHeight(location), location.y);
    }
    
    private bool CheckNearPoint(Vector2 location)
    {
        Vector2[] nearPoint =
        {
            location + Vector2.down,
            location + Vector2.up,
            location + Vector2.left,
            location + Vector2.right,
            location + Vector2.one,
            location - Vector2.one,
            location + new Vector2(1, -1),
            location + new Vector2(-1, 1)
        };

        foreach (Vector2 point in nearPoint)
        {
            if (IsInsideTerrain(point) && GetHeight(point) < .03f)
            {
                return true;
            }
        }

        return false;
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
        _locations.Add(GetPoint(location));
        
        Vector3 worldLocation = GetPoint(location);
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
