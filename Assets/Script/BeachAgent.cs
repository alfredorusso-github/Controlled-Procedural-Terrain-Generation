using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]

public class BeachAgent : MonoBehaviour
{
    //Terrain data
    private Terrain terrain;
    private TerrainData td;
    private int x;
    private int y;
    private float [,] heightmap;

    //Beach agent
    public int beachAgentsNr;
    public int BeachTokens;
    public float heighLimit;
    public int randomWalkSize;
    public int awayLimit;
    [Range(.003f, .01f)] public float beachHeight;

    Vector2Int[] neighboringPoints = new Vector2Int[]{
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
        terrain = GetComponent<Terrain> ();
        td = terrain.terrainData;
        x = td.heightmapResolution;
        y = td.heightmapResolution;

        //Initialize heighmap
        heightmap = new float[x,y]; 
    }

     public IEnumerator Action(){

        heightmap = td.GetHeights(0, 0, x, y);

        flatPoints();

        List<Vector2Int> shorelinePoints = getShorelinePoints();

        Debug.Log("Starting generting beach...");

        for (int i = 0; i < beachAgentsNr; i++){
            
            Vector2Int location = shorelinePoints[Random.Range(0, shorelinePoints.Count)];

            for (int j = 0; j < BeachTokens; j++){

                if(heightmap[location.y, location.x] >= heighLimit){
                    //location = random shoreline point
                    location = shorelinePoints[Random.Range(0, shorelinePoints.Count)];
                }

                //flatten area.
                heightmap[location.y, location.x] = Random.Range(.003f, beachHeight);
                foreach(Vector2Int point in getNeighboringPoints(location)){
                    heightmap[point.y, point.x] = Random.Range(.003f, beachHeight);
                }

                //smooth area. After i flatten the point and the nearby points i can smooth the area.
                float newHigh = VonNeumannNeighborhood(location);
                heightmap[location.y, location.x] = newHigh;
                foreach(Vector2Int point in getNeighboringPoints(location)){
                    heightmap[point.y, point.x] = VonNeumannNeighborhood(point);
                }
                
                //setting away with a random point in a short distance away from location
                Vector2Int away = awayRandomPoint(location);

                for(int k = 0; k < randomWalkSize; k++){

                    if(heightmap[away.y, away.x] <= .01f){

                        //flatten area around away
                        heightmap[away.y, away.x] = Random.Range(.003f, beachHeight);
                        foreach(Vector2Int point in getNeighboringPoints(away)){
                            heightmap[point.y, point.x] = Random.Range(.003f, beachHeight);
                        }

                        //smooth area around away
                        heightmap[away.y, away.x] = VonNeumannNeighborhood(away);
                        foreach(Vector2Int point in getNeighboringPoints(away)){
                            heightmap[point.y, point.x] = VonNeumannNeighborhood(point);
                        }
                        
                    }
                    
                    away = getNeighboringPoint(away);
                    if (away == -Vector2.one)
                    {
                        break;
                    }
                }

                location = getNeighboringPoint(location);
            }

            td.SetHeights(0, 0, heightmap);
            yield return new WaitForEndOfFrame();

        } 

        yield return new WaitForEndOfFrame();

        Debug.Log("Finish generating beach...");

        SplatMap.Instance.MakeSplatMap();
    }

    private List<Vector2Int> getShorelinePoints(){
        List<Vector2Int> shorelinePoints = new List<Vector2Int>();

        for(int i=0; i<x; i++){
            for(int j=0; j<y; j++){
                if(checkShorelinePoint(heightmap[j, i], i, j)){
                   shorelinePoints.Add(new Vector2Int(i, j));
                }
            }
        }

        return shorelinePoints;
    }

    private bool checkShorelinePoint(float heightMapValue, int i, int j){

        if(heightMapValue >= .003f && heightMapValue <= .006f && i + awayLimit < x && j + awayLimit < y){
            return true;
        }

        return false;
    }

    private List<Vector2Int> getNeighboringPoints(Vector2Int location){

        List<Vector2Int> validPoint = new List<Vector2Int>();
        foreach(Vector2Int point in neighboringPoints){
            if( checkNeighboringPoint(location + point)){
                validPoint.Add(location + point);
            }
        }

        return validPoint;
    }

    private Vector2Int getNeighboringPoint(Vector2Int location){

        List<Vector2Int> candidates = getNeighboringPoints(location);

        if (candidates.Count == 0)
        {
            Debug.Log("Impossible to find an away point");
            return -Vector2Int.one;
        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private bool checkLimit(Vector2Int location){

        if( (location.x >= 0 && location.x <= (x-1)) && (location.y >= 0 && location.y <= (y-1)) ){
            return true;
        }

        return false;
    }   

    private bool checkNeighboringPoint(Vector2Int point){

        if( (point.x >= 0 && point.x <= (x-1)) && (point.y >= 0 && point.y <= (y-1)) && heightmap[point.y, point.x] <= 0.01 ){
            return true;
        }

        return false;
    } 

    private Vector2Int awayRandomPoint(Vector2Int position){

        List<Vector2Int> candidates = new List<Vector2Int>();

        foreach (Vector2Int point in neighboringPoints){
            Vector2Int randomPoint = position + point * awayLimit; 

            if(checkLimit(randomPoint) &&  heightmap[randomPoint.y, randomPoint.x] < .01f){
                candidates.Add(randomPoint);
            }

        }

        return candidates[Random.Range(0, candidates.Count)];
    }

    private void flatPoints(){

        for(int i=0; i<x; i++){
            for(int j=0; j<y; j++){
                if(heightmap[j, i] < .003f){
                    heightmap[j, i] = 0.0f;
                }
            }
        }
        td.SetHeights(0, 0, heightmap);
    }

    private float VonNeumannNeighborhood(Vector2Int position){
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

        float centralPointHeight = heightmap[position.y, position.x]; 

        Vector2Int [] surroundingPoints = new Vector2Int[] {
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
            
            if( checkLimit(surroundingPoints[i]) ){
                heights[i] = heightmap[surroundingPoints[i].y, surroundingPoints[i].x];
            }
            else{
                heights[i] = 0;
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

}
