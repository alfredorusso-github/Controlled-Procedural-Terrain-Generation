using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class HarborAgent : MonoBehaviour
{
    //Terrain data
    private Terrain _terrain;
    private TerrainData _td;
    // private float[,] _heightmap;
    private int _x;
    private int _y;

    // Agent Data
    [FormerlySerializedAs("AgentNr")] public int agentNr;
    [FormerlySerializedAs("Token")] public int token;
    [Range(5, 50)] public int distance;
    [FormerlySerializedAs("HarborLenght")] [Range(5, 20)] public int harborLenght;

    // Harbor prefab
    public GameObject harbor;
    
    // In order to don't recalculate candidates for checking point and place harbor
    private List<Vector2> _candidates;
    
    // Valid Points
    List<Vector2> _validPoints;

    // OnDrawGizmos
    bool _start;
    private List<Vector3> _coastlinePoint = new List<Vector3>();
    private readonly List<Vector3> _locations = new List<Vector3>();

    // nearby point
    private readonly Vector2[] _nearbyPoint = {
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

        // Initialize heightmap
         // _heightmap = new float[_x, _y];

         StartCoroutine(Action());
    }

    private void OnDrawGizmos()
    {
        if (_start)
        {
            foreach (Vector3 location in _locations)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(location, .2f);
                
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(location, Vector3.one * harborLenght);
            }

            // foreach (var point in _validPoints)
            // {
            //     Gizmos.color = Color.red;
            //     Gizmos.DrawSphere(GetPoint(point), .2f);
            // }
        }
    }
    
    private Vector3 GetPoint(Vector2 location)
    {
        return new Vector3(location.x, GetHeight(location), location.y);
    }
    
    private IEnumerator Action()
    {
        // _heightmap = _td.GetHeights(0, 0, _x, _y);
        _validPoints = ValidPoints();

        _start = true;

        for (int i = 0; i < agentNr; i++)
        {
            for (int j = 0; j < token; j++)
            {
                // Move the agent to a random point on the coastline
                Vector2 location = GetNewPosition();

                if (location == -Vector2.one)
                {
                    Debug.Log("There are no more valid point to place harbor");
                    break;
                }
        
                _locations.Add(GetPoint(location));
                
                // Place Harbor
                PlaceHarbor(location);
        
                yield return new WaitForEndOfFrame();
            }
        
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();
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
    
    private bool CheckNearPoint(Vector2 location)
    {
        Vector2[] nearPoint =
        {
            location + Vector2.down,
            location + Vector2.up,
            location + Vector2.left,
            location + Vector2.right,
            // location + Vector2.one,
            // location - Vector2.one,
            // location + new Vector2(1, -1),
            // location + new Vector2(-1, 1)
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
    
    private Vector2 GetNewPosition()
    {
        Vector2 newPosition = RandomPoint();

        // Try for five times to find a valid point
        for (int i = 0; i < 100; i++)
        {
            if (CheckLocation(newPosition))
            {
                return newPosition;
            }

            newPosition = RandomPoint();
        }
        
        // No valid point has been found
        return -Vector2.one;
    }

    private bool CheckLocation(Vector2 location)
    {
        if (!IsInsideTerrain(location))
        {
            return false;
        }
        
        var c = Physics.OverlapBoxNonAlloc(GetPoint(location), Vector3.one * (harborLenght * .5f), new Collider[1], Quaternion.identity, LayerMask.GetMask(("Harbor")));

        if (c != 0)
        {
            return false;
        }

        _candidates = new List<Vector2>();
        
        foreach (Vector2 point in _nearbyPoint)
        {
            if (CheckNearbyPoint(location, location + point))
            {
                _candidates.Add(location + point);
            }
        }

        if (_candidates.Count == 0)
        {
            return false;
        }
        
        DrawDir(location, _candidates);

        return true;
    }
    
    private bool CheckNearbyPoint(Vector2 location, Vector2 candidate)
    {
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
            
            bool check = Physics.Raycast(new Vector3(pointToCheck.x, 100, pointToCheck.y), Vector3.down, Mathf.Infinity, LayerMask.GetMask("Harbor"));
            if (check)
            {
                return false;
            }
        }

        return true;
    }

    private Vector2 RandomPoint()
    {
        return _validPoints[Random.Range(0, _validPoints.Count)];
    }
    
    private void PlaceHarbor(Vector2 location)
    {
        Vector3 worldLocation = GetPoint(location);
        Vector3 candidatesWorldLocation = GetPoint(_candidates[Random.Range(0, _candidates.Count)]);

        Vector3 dir = (candidatesWorldLocation - worldLocation).normalized;
        dir.y = 0;

        Quaternion rotHarbor = Quaternion.LookRotation(dir, Vector3.up);
        float randomLenght = Random.Range(5, this.harborLenght);
        Vector3 scaleHarbor = new Vector3(1, 1, randomLenght);

        var tmpHarbor = Instantiate(harbor, worldLocation, rotHarbor);
        tmpHarbor.transform.localScale = scaleHarbor;
        tmpHarbor.transform.Translate(Vector3.forward * (randomLenght  * .5f));
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