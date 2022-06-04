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

                //setting away with a random point in a short distance away from location
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
        if (heightMapValue >= .003f && heightMapValue <= .006f && i + awayLimit < _x && j + awayLimit < _y)
        {
            return true;
        }

        return false;
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

        if (candidates.Count == 0)
        {
            Debug.Log("Impossible to find an away point");
            return -Vector2Int.one;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool CheckLimit(Vector2Int location)
    {
        if ((location.x >= 0 && location.x <= (_x - 1)) && (location.y >= 0 && location.y <= (_y - 1)))
        {
            return true;
        }

        return false;
    }

    private bool CheckNeighboringPoint(Vector2Int point)
    {
        if ((point.x >= 0 && point.x <= (_x - 1)) && (point.y >= 0 && point.y <= (_y - 1)) &&
            _heightmap[point.y, point.x] <= 0.01)
        {
            return true;
        }

        return false;
    }

    private Vector2Int AwayRandomPoint(Vector2Int position)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();

        foreach (Vector2Int point in _neighboringPoints)
        {
            Vector2Int randomPoint = position + point * awayLimit;

            if (CheckLimit(randomPoint) && _heightmap[randomPoint.y, randomPoint.x] < .01f)
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

    private float VonNeumannNeighborhood(Vector2Int position)
    {
        //Per calcolare la nuova altezza del punto nella posizione position vengono presi in cosiderazione i 4 punti che circondano tale punto e quelli dietro questi.
        //Viene assegnato un peso a tali punti, in particolare avremo che il punto centrale deve avere un peso 3 volte maggiore rispetto agli altri e che la somma dei pesi dei
        //9 punti presi in considerazione deve essere uguale a 11. Inoltre ai punti dietro quelli che circondano il punto centrale é stata assegnato un peso che é la metá di questi per fari si
        //che influenzassero meno il calcolo della nuova altezza. Partendo da queste informazioni e risolvendo in il sistema che viene fuori avremo che:
        // - il peso del punto centrale é 11/3
        // - il peso dei punti che circondano p é 11/9
        // - il peso dei punti dietro quelli che circondano p é 11/18

        float centralPointWeight = 11.0f / 3.0f;
        float surroundingWeight = 11.0f / 9.0f;
        float beyondSurroundingWeight = 11.0f / 18.0f;

        float centralPointHeight = _heightmap[position.y, position.x];

        Vector2Int[] surroundingPoints =
        {
            position + Vector2Int.right,
            position + Vector2Int.left,
            position + Vector2Int.up,
            position + Vector2Int.down,
            position + Vector2Int.right * 2,
            position + Vector2Int.left * 2,
            position + Vector2Int.up * 2,
            position + Vector2Int.down * 2
        };

        float[] heights = new float[surroundingPoints.Length];

        for (int i = 0; i < surroundingPoints.Length; i++)
        {
            if (CheckLimit(surroundingPoints[i]))
            {
                heights[i] = _heightmap[surroundingPoints[i].y, surroundingPoints[i].x];
            }
            else
            {
                heights[i] = 0;
            }
        }

        float num = centralPointHeight * centralPointWeight;
        for (int i = 0; i < heights.Length; i++)
        {
            if (i < 4)
            {
                num += heights[i] * surroundingWeight;
            }
            else
            {
                num += heights[i] * beyondSurroundingWeight;
            }
        }

        float denom = centralPointWeight + (4 * surroundingWeight) + (4 * beyondSurroundingWeight);

        return num / denom;
    }
}