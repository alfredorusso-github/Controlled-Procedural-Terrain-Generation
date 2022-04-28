using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(Terrain))]
public class CoastlineAgent : MonoBehaviour
{
    struct Agent
    {
        int vertex;

        public Agent(int vertex){
            this.vertex = vertex;
        }

        public int getVertex(){
            return vertex;
        }
    }

    //Terrain data
    private Terrain terrain;
    private TerrainData td;
    private int x;
    private int y;
    private float [,] heightmap;

    //Coastline agent
    public bool startingFromMapCenter;
    public int CoastlineTokens;
    public int VertexLimit;
    private Queue agents;
    [Range(0.03f, 1.0f)] public float maxHeight;

    // Directions vector
    Vector2Int[] directions = new Vector2Int[]{
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.one,
        -Vector2Int.one,
        new Vector2Int(1, -1),
        new Vector2Int(-1 ,1)
    };

    // variable for testing
    private int elevatedVertex = 0;

    void Start(){
        
        //Getting terrain information
        terrain = GetComponent<Terrain> ();
        td = terrain.terrainData;
        x = td.heightmapResolution;
        y = td.heightmapResolution;

        //Initialize heighmap
        heightmap = new float[x,y];

        // Initialize Queue
        agents = new Queue();

        // Instantiate first agent
        Agent agent = new Agent((x-1) * (y-1));
        GetCoastlineAgents(agent);

        Debug.Log("Number of agents: " + agents.Count);

        if(CoastlineTokens * agents.Count > x*y){
            Debug.Log("Number of Tokens too high, reduce it");
            return;
        }

        StartCoroutine(Action());
    }

    private void GetCoastlineAgents(Agent agent){
        
        if(agent.getVertex() >= VertexLimit){
            for(int i=0; i<2; i++){
                GetCoastlineAgents(new Agent(agent.getVertex()/2));
            }                    
        }
        else{
            agents.Enqueue(agent);
        }
    }

    private IEnumerator Action(){

        while(agents.Count != 0){

            Vector2Int location = getCoastlinePoints();
            
            Agent agent = (Agent)agents.Dequeue();

            for (int i = 0; i < CoastlineTokens; i++){

                // Points to score
                List<Vector2Int> candidates = nearPoints(location);

                while(candidates.Count == 0){
                    location = getCoastlinePoints();
                    candidates = nearPoints(location);
                }

                List<float> scores = new List<float>();

                for (int j = 0; j < candidates.Count; j++){
                    Vector2 attractor = getAttractor();
                    Vector2 repulsor = getRepulsor(attractor);
                    scores.Add(scorePoint(candidates[j], agent, attractor, repulsor));
                }

                location = candidates[scores.IndexOf(scores.Max())];
                heightmap[location.y, location.x] = Random.Range(.02f, maxHeight);
                elevatedVertex ++;
            }

            td.SetHeights(0, 0, heightmap);
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();

        Debug.Log("Vertex Elevated: " + elevatedVertex);

        yield return SmoothingAgent.Instance.Action();
    }

    private Vector2Int getCoastlinePoints(){
        List<Vector2Int> coastlinePoints = new List<Vector2Int>();

        for (int i = 0; i < x; i++){
            for (int j = 0; j < y; j++){
                Vector2Int tmp = new Vector2Int(i, j);
                if(heightmap[tmp.y, tmp.x] !=0 && checkNearPoint(tmp)){
                    coastlinePoints.Add(tmp);
                }
            }
        }

        if(coastlinePoints.Count != 0){
            return coastlinePoints[Random.Range(0, coastlinePoints.Count)];
        }

        if(startingFromMapCenter){
            Debug.Log("Starting from map center " + "x:" + (x/2) + " y:" + (y/2));
            return new Vector2Int(x/2, y/2);
        }

        return new Vector2Int(Random.Range(0, x), Random.Range(0, y));
    }

    private bool checkNearPoint(Vector2Int location){
        
        foreach (Vector2Int point in directions){
            Vector2Int tmp = point + location;
            if(heightmap[tmp.y, tmp.x] == 0){
                return true;
            }
        }
        return false;
    }  

    private List<Vector2Int> nearPoints(Vector2Int location){

        List<Vector2Int> nearestPoints = new List<Vector2Int>();

        foreach(Vector2Int point in directions){
            Vector2Int tmp = location + point;
            if(checkLimit(tmp) && heightmap[tmp.y, tmp.x] == 0){
                nearestPoints.Add(tmp);
            }
        }

        return nearestPoints;
    }

    private Vector2 getAttractor(){
        return new Vector2(Random.Range(0, x), Random.Range(0, y));
    }

    private Vector2 getRepulsor(Vector2 attractor){

        Vector2 center = new Vector2(x/2, y/2);
        
        Vector2 attractorDirection = (attractor - center).normalized;

        // Calculating repulsor
        Vector2 repulsor = new Vector2(Random.Range(0, x), Random.Range(0, y));
        Vector2 repulsorDirection = (repulsor - center).normalized;

        while(attractorDirection == repulsorDirection){
            repulsor = new Vector2(Random.Range(0, x), Random.Range(0, y));
            repulsorDirection = (repulsor - center).normalized;
        }

        return repulsor;
    }

    private float scorePoint(Vector2 point, Agent agent, Vector2 attractor, Vector2 repulsor){
        float result = Mathf.Pow(Vector2.Distance(point, repulsor), 2.0f) - Mathf.Pow(Vector2.Distance(point, attractor), 2.0f) + (3 * Mathf.Pow(getClosestDistance(point, agent), 2.0f));
        return result;
    }

    private float getClosestDistance(Vector2 point, Agent agent){

        //Order of the point inside the array to respect the Point point received from input: left, right, up, down 
        Vector2[] borderPoints = new Vector2[]{
            new Vector2(0, point.y), new Vector2(x, point.y), 
            new Vector2(point.x, y), new Vector2(point.x, 0)
        };

        float[] result = new float[borderPoints.Length];
        for(int i=0; i<result.Length; i++){
            result[i] = Vector2.Distance(point, borderPoints[i]);
        }

        return result.OrderBy(a => a).ToArray()[0];
    }

    private bool checkLimit(Vector2Int point){
        if(point.x > 0 && point.x < x-1 && point.y > 0 && point.y < y-1){
            return true;
        }

        return false;
    }

}
