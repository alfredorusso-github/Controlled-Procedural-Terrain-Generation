using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class CoastlineAgent : MonoBehaviour
{
    private readonly struct Agent
    {
        private readonly int _vertex;
        private readonly Vector2 _direction;

        public Agent(int vertex, Vector2 direction)
        {
            _vertex = vertex;
            _direction = direction;
        }

        public int GetVertex()
        {
            return _vertex;
        }

        public Vector2 GetDirection()
        {
            return _direction;
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
    private Vector2 _center;
    private bool _firstTime;
    private HashSet<Vector2> _coastlinePoints;

    // Directions vector
    private readonly Vector2[] _directions =
    {
        Vector2.up,
        Vector2.down,
        Vector2.right,
        Vector2.left,
        Vector2.one,
        -Vector2.one,
        new Vector2(1, -1),
        new Vector2(-1, 1)
    };

    // variable for testing
    private int _elevatedVertex;

    private void Start()
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
        
        // Initialize coastline points
        _coastlinePoints = new HashSet<Vector2>();
        
        // Instantiate first agent
        Agent agent = new Agent((_x - 1) * (_y - 1), _directions[Random.Range(0, _directions.Length)]);
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

    public HashSet<Vector2> CoastlinePoints()
    {
        return _coastlinePoints;
    }

    private void GetCoastlineAgents(Agent agent)
    {
        if (agent.GetVertex() >= vertexLimit)
        {
            for (int i = 0; i < 2; i++)
            {
                GetCoastlineAgents(new Agent(agent.GetVertex() / 2, _directions[Random.Range(0, _directions.Length)]));
            }
        }
        else
        {
            _agents.Enqueue(agent);
        }
    }

    private IEnumerator Action()
    {
        Debug.Log("Started raising points...");
        
        while (_agents.Count != 0)
        {
            Agent agent = (Agent)_agents.Dequeue();
            
            Vector2 location;

            if (_firstTime)
            {
                location = GetStartingPoint();
                _center = location;
                _firstTime = false;
                _coastlinePoints.Add(location);
                Debug.Log("Island center: " + _center);
            }
            else
            {
                location = FindCoastlinePoint(agent);
            }

            for (int i = 0; i < coastlineTokens; i++)
            {
                List<Vector2> candidates = NearPoints(location);

                while (candidates.Count == 0)
                {
                    location = FindCoastlinePoint(agent);
                    candidates = NearPoints(location);
                }

                location = candidates[Random.Range(0, candidates.Count)];
                _coastlinePoints.Add(location);

                _heightmap[(int) location.y, (int) location.x] = Random.Range(.02f, maxHeight);
                _elevatedVertex++;
            }

            _td.SetHeights(0, 0, _heightmap);
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();

        Debug.Log("Vertex Elevated: " + _elevatedVertex);
        
        Debug.Log("Finished raising points...");

        yield return SmoothingAgent.Instance.Action();
    }

    private Vector2 GetStartingPoint()
    {
        return startingFromMapCenter ? new Vector2((_x - 1) * .5f, (_y - 1) * .5f) : new Vector2(Random.Range(0, _x), Random.Range(0, _y));
    }
    
    private Vector2 FindCoastlinePoint(Agent agent)
    {
        Vector2 startingLocation = RandomPoint();
        Vector2 location = startingLocation;

        // Check if the direction not lead to a point outside the map
        for (int i = 0; i < _x; i++)
        {
            if (IsInsideTerrain(location) && CheckNearPoint(location) && _heightmap[(int) location.y,  (int) location.x] != 0)
            {
                return location;
            }

            location += agent.GetDirection();
        }
        
        // if here means the direction lead outside the map, use opposite direction
        location = startingLocation;
        for (int i = 0; i < _x; i++)
        {
            if (IsInsideTerrain(location) && CheckNearPoint(location) && _heightmap[(int) location.y,  (int) location.x] != 0)
            {
                return location;
            }

            location -= agent.GetDirection();
        }

        return location;
    }

    private Vector2 RandomPoint()
    {
        return _coastlinePoints.ElementAt(Random.Range(0, _coastlinePoints.Count));
    }

    private bool CheckNearPoint(Vector2 location)
    {
        foreach (Vector2 point in _directions)
        {
            Vector2 tmp = point + location;
            if (IsInsideTerrain(tmp) && _heightmap[(int) tmp.y, (int) tmp.x] == 0)
            {
                return true;
            }
        }

        return false;
    }

    private List<Vector2> NearPoints(Vector2 location)
    {
        List<Vector2> nearestPoints = new List<Vector2>();

        foreach (Vector2 point in _directions)
        {
            Vector2 tmp = location + point;
            if (IsInsideTerrain(tmp) && _heightmap[(int) tmp.y, (int) tmp.x] == 0)
            {
                nearestPoints.Add(tmp);
            }
        }

        return nearestPoints;
    }

    private bool IsInsideTerrain(Vector2 point)
    {
        return point.x >= 0 && point.x < _x  && point.y >= 0 && point.y < _y;
    }
}