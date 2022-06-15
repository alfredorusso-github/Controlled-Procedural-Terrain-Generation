using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class BeachAgent : MonoBehaviour
{
    //Terrain data
    private Terrain _terrain;
    private TerrainData _td;
    private int _x;
    private int _y;
    private float[,] _heightmap;

    //Beach agent
    public int beachAgentsNr;
    public int beachTokens;
    public float heightLimit;
    public int randomWalkSize;
    public int awayLimit;
    [Range(.003f, .01f)] public float beachHeight;

    private readonly Vector2Int[] _neighboringPoints =
    {
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
    public static BeachAgent Instance;

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

    public IEnumerator Action()
    {
        _heightmap = _td.GetHeights(0, 0, _x, _y);

        FlatPoints();

        List<Vector2Int> shorelinePoints = GetShorelinePoints();

        Debug.Log("Starting generating beach...");

        for (int i = 0; i < beachAgentsNr; i++)
        {
            Vector2Int location = shorelinePoints[Random.Range(0, shorelinePoints.Count)];

            for (int j = 0; j < beachTokens; j++)
            {
                if (_heightmap[location.y, location.x] >= heightLimit)
                {
                    //location = random shoreline point
                    location = shorelinePoints[Random.Range(0, shorelinePoints.Count)];
                }

                //flatten area.
                _heightmap[location.y, location.x] = Random.Range(.003f, beachHeight);
                foreach (Vector2Int point in GetNeighboringPoints(location))
                {
                    _heightmap[point.y, point.x] = Random.Range(.003f, beachHeight);
                }

                //smooth area. After i flatten the point and the nearby points i can smooth the area.
                float newHigh = VonNeumannNeighborhood(location);
                _heightmap[location.y, location.x] = newHigh;
                foreach (Vector2Int point in GetNeighboringPoints(location))
                {
                    _heightmap[point.y, point.x] = VonNeumannNeighborhood(point);
                }

                //setting away with a random point in a short roadLenght away from location
                Vector2Int away = AwayRandomPoint(location);

                for (int k = 0; k < randomWalkSize; k++)
                {
                    if (_heightmap[away.y, away.x] <= .01f)
                    {
                        //flatten area around away
                        _heightmap[away.y, away.x] = Random.Range(.003f, beachHeight);
                        foreach (Vector2Int point in GetNeighboringPoints(away))
                        {
                            _heightmap[point.y, point.x] = Random.Range(.003f, beachHeight);
                        }
                    
                        //smooth area around away
                        _heightmap[away.y, away.x] = VonNeumannNeighborhood(away);
                        foreach (Vector2Int point in GetNeighboringPoints(away))
                        {
                            _heightmap[point.y, point.x] = VonNeumannNeighborhood(point);
                        }
                    }

                    away = GetNeighboringPoint(away);
                    if (away == -Vector2.one)
                    {
                        break;
                    }
                }

                location = GetNeighboringPoint(location);
            }

            _td.SetHeights(0, 0, _heightmap);
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();

        Debug.Log("Finish generating beach...");

        SplatMap.Instance.MakeSplatMap();

        yield return CityAgent.Instance.Action();
    }
    
    private List<Vector2Int> GetShorelinePoints()
    {
        List<Vector2Int> shorelinePoints = new List<Vector2Int>();

        for (int i = 0; i < _x; i++)
        {
            for (int j = 0; j < _y; j++)
            {
                if (CheckShorelinePoint(_heightmap[j, i], i, j))
                {
                    shorelinePoints.Add(new Vector2Int(i, j));
                }
            }
        }

        return shorelinePoints;
    }

    private bool CheckShorelinePoint(float heightMapValue, int i, int j)
    {
        return heightMapValue >= .003f && heightMapValue < .01f && i + awayLimit < _x && j + awayLimit < _y;
    }

    private List<Vector2Int> GetNeighboringPoints(Vector2Int location)
    {
        List<Vector2Int> validPoint = new List<Vector2Int>();
        foreach (Vector2Int point in _neighboringPoints)
        {
            if (CheckNeighboringPoint(location + point))
            {
                validPoint.Add(location + point);
            }
        }

        return validPoint;
    }

    private Vector2Int GetNeighboringPoint(Vector2Int location)
    {
        List<Vector2Int> candidates = GetNeighboringPoints(location);

        if (candidates.Count != 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }
        
        Debug.Log("Impossible to find a valid point");
        return -Vector2Int.one;
    }

    private bool IsInsideTerrain(Vector2Int location)
    {
        return location.x >= 0 && location.x <= _x - 1 && location.y >= 0 && location.y <= _y - 1;
    }

    private bool CheckNeighboringPoint(Vector2Int point)
    {
        return IsInsideTerrain(point) && _heightmap[point.y, point.x] <= 0.01;
    }

    private Vector2Int AwayRandomPoint(Vector2Int position)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        foreach (Vector2Int point in _neighboringPoints)
        {
            Vector2Int randomPoint = position + point * awayLimit;

            if (IsInsideTerrain(randomPoint) && _heightmap[randomPoint.y, randomPoint.x] < .01f)
            {
                candidates.Add(randomPoint);
            }
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void FlatPoints()
    {
        for (int i = 0; i < _x; i++)
        {
            for (int j = 0; j < _y; j++)
            {
                if (_heightmap[j, i] < .003f)
                {
                    _heightmap[j, i] = 0.0f;
                }
            }
        }

        _td.SetHeights(0, 0, _heightmap);
    }

    private float VonNeumannNeighborhood(Vector2Int position){

        float centralPointWeight = 11.0f/4.0f;
        float surroundingWeight = 33.0f/4.0f;

        float centralPointHeight = _heightmap[position.y, position.x]; 

        Vector2Int [] surroundingPoints = {
            position + Vector2Int.right,
            position + Vector2Int.left, 
            position + Vector2Int.up, 
            position + Vector2Int.down, 
            position + Vector2Int.right * 2, 
            position + Vector2Int.left * 2, 
            position + Vector2Int.up * 2, 
            position + Vector2Int.down * 2 
            
        };

        float [] heights = new float[surroundingPoints.Length];

        for(int i=0; i < surroundingPoints.Length; i++){
            
            if( IsInsideTerrain(surroundingPoints[i]) ){
                heights[i] = _heightmap[surroundingPoints[i].y, surroundingPoints[i].x];
            }
            else
            {
                heights[i] = _heightmap[position.y, position.x];
            }            
        }

        float num = centralPointHeight * centralPointWeight;
        foreach (float height in heights)
        {
            num += height * surroundingWeight;
        }

        float denom = centralPointWeight + 8 * surroundingWeight; 

        return num/denom;
    }
}