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

    public float VonNeumannNeighborhood(Vector2Int position){
        //Per calcolare la nuova altezza del punto nella posizione position vengono presi in cosiderazione i 4 punti che circondano tale punto e quelli dietro questi.
        //Viene assegnato un peso a tali punti, in particolare avremo che il punto centrale deve avere un peso 3 volte maggiore rispetto agli altri e che la somma dei pesi dei
        //9 punti presi in considerazione deve essere uguale a 11. Inoltre ai punti dietro quelli che circondano il punto centrale é stata assegnato un peso che é la metá di questi per fari si
        //che influenzassero meno il calcolo della nuova altezza. Partendo da queste informazioni e risolvendo in il sistema che viene fuori avremo che:
        // - il peso del punto centrale é 11/3
        // - il peso dei punti che circondano p é 11/9
        // - il peso dei punti dietro quelli che circondano p é 11/18

        float centralPointWeight = 11.0f/3.0f;
        float surroundingWeight = 11.0f/9.0f;
        float beyondSurroundingWeight = 11.0f/18.0f;

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

        for(int i=0; i<surroundingPoints.Length; i++){
            
            if( CheckLimit(surroundingPoints[i]) ){
                heights[i] = _heightmap[surroundingPoints[i].y, surroundingPoints[i].x];
            }
            else{
                heights[i] = _heightmap[position.y, position.x];
            }            
        }

        float num = centralPointHeight * centralPointWeight;
        for(int i=0; i<heights.Length; i++){
            if(i<4){
                num += heights[i] * surroundingWeight;
            }
            else{
                num += heights[i] * beyondSurroundingWeight;
            }
        }

        float denom = centralPointWeight + (4*surroundingWeight) + (4*beyondSurroundingWeight); 

        return num/denom;
    }

    private Vector2Int GetNeighboringPoint(Vector2Int position){

        // if the agents never move itself outside the maps, the list will always have at least three point inside
        List<Vector2Int> check = new List<Vector2Int>();
        foreach(Vector2Int point in _neighboringPoint){
            if((CheckLimit(position + point))){
                check.Add(position + point);
            }
        }

        return check[Random.Range(0, check.Count)];
    }

    private bool CheckLimit(Vector2Int point){
        if(point.x >= 0 && point.x < _x && point.y >= 0 && point.y < _y){
            return true;
        }

        return false;
    }
}
