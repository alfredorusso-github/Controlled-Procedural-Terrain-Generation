using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Terrain))]
public class CoastlineAgent : MonoBehaviour
{
    private readonly struct Agent
    {
        private readonly int _vertex;

        public Agent(int vertex)
        {
            _vertex = vertex;
        }

        public int GetVertex()
        {
            return _vertex;
        }
    }

    //Terrain data
    private Terrain _terrain;
    private TerrainData _td;
    private int _x;
    private int _y;
    private float[,] _heightmap;

    //Coastline agent
    public bool startingFromMapCenter;
    public int coastlineTokens;
    public int vertexLimit;
    [Range(0.03f, 1.0f)] public float maxHeight;
    private Queue _agents;
    private Vector2Int _center;
    private bool _firstTime;

    // Directions vector
    private readonly Vector2Int[] _directions =
    {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.one,
        -Vector2Int.one,
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1)
    };

    // variable for testing
    private int _elevatedVertex;

    void Start()
    {
        //Getting terrain information
        _terrain = GetComponent<Terrain>();
        _td = _terrain.terrainData;
        _x = _td.heightmapResolution;
        _y = _td.heightmapResolution;

        //Initialize heightmap
        _heightmap = new float[_x, _y];

        // Initialize Queue
        _agents = new Queue();

        // Instantiate first agent
        Agent agent = new Agent((_x - 1) * (_y - 1));
        GetCoastlineAgents(agent);
        Debug.Log("Number of agents: " + _agents.Count);

        if (coastlineTokens * _agents.Count > _x * _y)
        {
            Debug.Log("Number of Tokens too high, reduce it");
            return;
        }

        _firstTime = true;
        
        StartCoroutine(Action());
    }

    private void GetCoastlineAgents(Agent agent)
    {
        if (agent.GetVertex() >= vertexLimit)
        {
            for (int i = 0; i < 2; i++)
            {
                GetCoastlineAgents(new Agent(agent.GetVertex() / 2));
            }
        }
        else
        {
            _agents.Enqueue(agent);
        }
    }

    private IEnumerator Action()
    {
        while (_agents.Count != 0)
        {
            _ = (Agent)_agents.Dequeue();
            
            Vector2Int location = GetCoastlinePoints();

            if (_firstTime)
            {
                _center = location;
                _firstTime = false;
                Debug.Log("Island center: " + _center);
            }

            for (int i = 0; i < coastlineTokens; i++)
            {
                // Points to score
                List<Vector2Int> candidates = NearPoints(location);

                while (candidates.Count == 0)
                {
                    location = GetCoastlinePoints();
                    candidates = NearPoints(location);
                }

                List<float> scores = new List<float>();

                foreach (Vector2Int candidate in candidates)
                {
                    Vector2 attractor = GetAttractor();
                    Vector2 repulsor = GetRepulsor(attractor);
                    scores.Add(ScorePoint(candidate, attractor, repulsor));
                }

                location = candidates[scores.IndexOf(scores.Max())];
                _heightmap[location.y, location.x] = Random.Range(.02f, maxHeight);
                _elevatedVertex++;
            }

            _td.SetHeights(0, 0, _heightmap);
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();

        Debug.Log("Vertex Elevated: " + _elevatedVertex);

        yield return SmoothingAgent.Instance.Action();
    }

    private Vector2Int GetCoastlinePoints()
    {
        List<Vector2Int> coastlinePoints = new List<Vector2Int>();

        for (int i = 0; i < _x; i++)
        {
            for (int j = 0; j < _y; j++)
            {
                Vector2Int tmp = new Vector2Int(i, j);
                if (_heightmap[tmp.y, tmp.x] != 0 && CheckNearPoint(tmp))
                {
                    coastlinePoints.Add(tmp);
                }
            }
        }

        if (coastlinePoints.Count != 0)
        {
            return coastlinePoints[Random.Range(0, coastlinePoints.Count)];
        }

        // If here means that the first agent is placed on the terrain
        if (!startingFromMapCenter)
        {
            return new Vector2Int(Random.Range(0, _x), Random.Range(0, _y));
        }

        return new Vector2Int(_x / 2, _y / 2);
    }

    private bool CheckNearPoint(Vector2Int location)
    {
        foreach (Vector2Int point in _directions)
        {
            Vector2Int tmp = point + location;
            if (CheckLimit(tmp) && _heightmap[tmp.y, tmp.x] == 0)
            {
                return true;
            }
        }

        return false;
    }

    private List<Vector2Int> NearPoints(Vector2Int location)
    {
        List<Vector2Int> nearestPoints = new List<Vector2Int>();

        foreach (Vector2Int point in _directions)
        {
            Vector2Int tmp = location + point;
            if (CheckLimit(tmp) && _heightmap[tmp.y, tmp.x] == 0)
            {
                nearestPoints.Add(tmp);
            }
        }

        return nearestPoints;
    }

    private Vector2 GetAttractor()
    {
        return new Vector2(Random.Range(0, _x), Random.Range(0, _y));
    }

    private Vector2 GetRepulsor(Vector2 attractor)
    {
        Vector2 attractorDirection = (attractor - _center).normalized;

        //Calculating repulsor
        Vector2 repulsor = new Vector2(Random.Range(0, _x), Random.Range(0, _y));
        Vector2 repulsorDirection = (repulsor - _center).normalized;

        while (attractorDirection == repulsorDirection)
        {
            repulsor = new Vector2(Random.Range(0, _x), Random.Range(0, _y));
            repulsorDirection = (repulsor - _center).normalized;
        }

        return repulsor;
    }


    private float ScorePoint(Vector2 point, Vector2 attractor, Vector2 repulsor)
    {
        float result = Mathf.Pow(Vector2.Distance(point, repulsor), 2.0f) -
                       Mathf.Pow(Vector2.Distance(point, attractor), 2.0f) +
                       (3 * Mathf.Pow(GetClosestDistance(point), 2.0f));
        return result;
    }

    private float GetClosestDistance(Vector2 point)
    {
        //Order of the point inside the array to respect the Point point received from input: left, right, up, down 
        Vector2[] borderPoints =
        {
            new Vector2(0, point.y), new Vector2(_x, point.y),
            new Vector2(point.x, _y), new Vector2(point.x, 0)
        };

        float[] result = new float[borderPoints.Length];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = Vector2.Distance(point, borderPoints[i]);
        }

        return result.OrderBy(a => a).ToArray()[0];
    }

    private bool CheckLimit(Vector2Int point)
    {
        if (point.x >= 0 && point.x <= _x - 1 && point.y >= 0 && point.y <= _y - 1)
        {
            return true;
        }

        return false;
    }
}