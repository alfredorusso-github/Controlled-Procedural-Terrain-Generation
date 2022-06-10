using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Terrain))]
public class SmoothingAgent : MonoBehaviour
{

    //Terrain data
    private Terrain _terrain;
    private TerrainData _td;
    private int _x;
    private int _y;
    private float [,] _heightmap;

    //Smoothing agent
    public int smoothingAgentsNr;
    public int returnValue;
    public int smoothingTokens;
    private HashSet<Vector2> _coastlinePoints;

    // Neighboring Point
    private readonly Vector2Int[] _neighboringPoint = {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.one,
        -Vector2Int.one,
        new Vector2Int(1, -1),
        new Vector2Int(-1, 1)
    };

    // Instance of this class
    public static SmoothingAgent Instance;

    private void Awake() {
        
        if(Instance == null){
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else{
            Destroy(gameObject);
        }

    }

    void Start(){

        //Getting terrain information
        _terrain = GetComponent<Terrain> ();
        _td = _terrain.terrainData;
        _x = _td.heightmapResolution;
        _y = _td.heightmapResolution;

        //Initialize heightmap
        _heightmap = new float[_x, _y];
    }

    public IEnumerator Action(){  

        _heightmap = _td.GetHeights(0, 0, _x ,_y);
        _coastlinePoints = FindObjectOfType<CoastlineAgent>().CoastlinePoints();

        Debug.Log("Starting smoothing...");

        for (int i = 0; i < smoothingAgentsNr; i++){
            
            Vector2Int startingPoint = Vector2Int.RoundToInt(_coastlinePoints.ElementAt(Random.Range(0, _coastlinePoints.Count)));

            //Count for checking when the smoothing agent need to return to startingPoint
            int count = 0;

            Vector2Int location = startingPoint;

            for (int j = 0; j < smoothingTokens; j++){
                if(count > smoothingTokens/returnValue){
                    location = startingPoint;
                    count = 0;
                }
                else{

                    //adjusting the value of the point in position location
                    float newHeight = VonNeumannNeighborhood(location);
                    _heightmap[location.y, location.x] = newHeight;

                    location = GetNeighboringPoint(location);
                    count++;
                }
            }
            
            _td.SetHeights(0, 0, _heightmap);
            yield return new WaitForEndOfFrame();
        }

        yield return new WaitForEndOfFrame();

        Debug.Log("Finish smoothing...");
                
        yield return BeachAgent.Instance.Action();
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

    private Vector2Int GetNeighboringPoint(Vector2Int position){

        // if the agents never move itself outside the maps, the list will always have at least three point inside
        List<Vector2Int> check = new List<Vector2Int>();
        foreach(Vector2Int point in _neighboringPoint){
            if((IsInsideTerrain(position + point))){
                check.Add(position + point);
            }
        }

        return check[Random.Range(0, check.Count)];
    }

    private bool IsInsideTerrain(Vector2Int point){
        if(point.x >= 0 && point.x < _x && point.y >= 0 && point.y < _y){
            return true;
        }

        return false;
    }
}
