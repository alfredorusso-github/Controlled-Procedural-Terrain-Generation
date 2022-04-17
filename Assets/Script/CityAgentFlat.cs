using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CityAgentFlat : MonoBehaviour
{
    //Terrain data
    private Terrain terrain;
    private TerrainData td;
    private int x;
    private int y;
    private float [,] heightmap;
    private float [,] tmpHeightMap;

    //City agent
    public int cityAgentsNr;
    public int cityTokens;
    public GameObject roads = null;
    public GameObject palace = null;
    public int gap;
    [Range(5, 20)] public int distance;
    public int maxNHouse{
        get{
            if(gap == 1){
                return (distance/gap) - 2;
            }
            return (distance/gap) - 1;
        }
    }
    [HideInInspector]
    public int NumberOfHouse = 1;

    // Start is called before the first frame update
    void Start(){
        
        //Getting terrain information
        terrain = GetComponent<Terrain> ();
        td = terrain.terrainData;
        x = td.heightmapResolution;
        y = td.heightmapResolution;

        //Inizialize heighmap
        heightmap = new float[x,y];

        StartCoroutine(cityActionCoroutine());        
    }

    private IEnumerator cityActionCoroutine(){

        List<Vector2> roadPoints = new List<Vector2>();

        // Getting terrain position
        Vector3 pos = terrain.GetPosition();

        for(int i=0; i<cityAgentsNr; i++){

            Vector2 location = getStartingPoint();
            // roadPoints.Add(location);
            
            Vector2 prevLocation = location;
            location = getNewLocation(location, prevLocation, roadPoints);
            // roadPoints.Add(location);

            for(int j=0; j<cityTokens; j++){
                
                // Debug.Log("PrevLocation: " + prevLocation + " Location: " + location);

                createRoad(location, prevLocation, pos);
                // createPalace(location, prevLocation, pos);

                // yield return new WaitForSeconds(.5f);
                yield return new WaitForEndOfFrame();

                Vector2 tmp = location;
                location = getNewLocation(location, prevLocation, roadPoints, false);
                if(location == tmp){
                    break;
                }
                roadPoints.Add(location);
                prevLocation = tmp;
            }
        }
    }

    private Vector2 getStartingPoint(){
        return new Vector2(Random.Range(1, x-2), Random.Range(1, y-2));
    }

    private void createPalace(Vector2 location, Vector2 prevLocation, Vector3 pos){

        Vector3 worldLocation = new Vector3(location.x + pos.x, 0.05f, location.y + pos.z);
        Vector3 prevWorldLocation = new Vector3(prevLocation.x + pos.x, 0.05f, prevLocation.y + pos.z);

        Vector3 dir = (worldLocation - prevWorldLocation).normalized; 

        // Debug.Log("WorldLocation: " + worldLocation + " Prev: " + prevWorldLocation + " Direction: " + dir + " Cross with Vector3.up: " + Vector3.Cross(dir, Vector3.up));

        Vector3 palace_pos = Vector3.Lerp(worldLocation, prevWorldLocation, 0.5f);
        Quaternion rot_palace = Quaternion.LookRotation(Vector3.Cross(dir,Vector3.up), Vector3.up);

        var pal = Instantiate(palace, palace_pos , rot_palace);
        pal.transform.Translate(Vector3.up * 0.5f);
        pal.transform.Translate(-Vector3.forward);
        pal.AddComponent<RayCaster>();

        if(NumberOfHouse == 1){
            return;
        }

        Vector3 pos_right = palace_pos;
        Vector3 pos_left = palace_pos;

        for(int i=0; i<NumberOfHouse-1; i++){

            if(i % 2 == 0){
                pos_right += dir * gap;
                pal = Instantiate(palace, pos_right, rot_palace);
                pal.transform.Translate(Vector3.up * 0.5f);
                pal.transform.Translate(-Vector3.forward);
                pal.AddComponent<RayCaster>();
            }
            else{
                pos_left -= dir * gap;
                pal = Instantiate(palace, pos_left, rot_palace);
                pal.transform.Translate(Vector3.up * 0.5f);
                pal.transform.Translate(-Vector3.forward);
                pal.AddComponent<RayCaster>();
            }

        }
    }

    private void createRoad(Vector2 location, Vector2 prevLocation, Vector3 pos){
        
        Vector3 worldLocation = new Vector3(location.x + pos.x, 0.05f, location.y + pos.y);
        Vector3 prevWorldLocation = new Vector3(prevLocation.x + pos.x, 0.05f, prevLocation.y + pos.y);

        Vector3 dir = (worldLocation - prevWorldLocation).normalized; 

        float length = Vector3.Distance(worldLocation, prevWorldLocation);
        Quaternion rot_road = Quaternion.LookRotation(dir, Vector3.up);
        Vector3 scale_road = new Vector3(1.0f, 1.0f, length);

        var road = Instantiate(roads, worldLocation, rot_road);
        road.transform.Translate(-Vector3.forward * (length + 1.0f)  * 0.5f);
        road.transform.localScale = scale_road;
    }

    private Vector2 getNewLocation(Vector2 location, Vector2 prevLocation, List<Vector2> roadPoints, bool start = true){

        Vector2[] points;
        if(start){
            points = getPointsStart(location);
        }
        else{
            points = getPoints(location, prevLocation);
        }

        List<Vector2> checkedPoints = new List<Vector2>();

        for(int k=0; k<points.Length; k++){

            if(!start){
                if(checkLocation(points[k], location) && !roadPoints.Contains(points[k])){
                    checkedPoints.Add(points[k]);
                }
            }
            else{
                if(checkLocation(points[k], location)){
                    checkedPoints.Add(points[k]);
                }
            }

            // Debug.Log("PrevLocation: " + prevLocation + " Point: " + points[k].ToString() + " CheckPoint: " + chcekPoint(points[k], prevLocation));
        }

        if(checkedPoints.Count != 0){
            return checkedPoints[0];
        }
        

        // for(int k=0; k<points.Length; k++){

        //     if(checkLocation(points[k], location)){
        //         checkedPoints.Add(points[k]);
        //     }

        // }

        // if(checkedPoints.Count != 0){
        //     return checkedPoints[0];
        // }

        return location;
    }

    private Vector2[] getPoints(Vector2 location, Vector2 prevLocation){
        
        Vector2 dir = (location - prevLocation).normalized;
        Vector2 perpendicularDir = Vector2.Perpendicular(dir);

        Vector2[] points = new Vector2[3];

        points[0] = location + dir * distance;
        points[1] = location + perpendicularDir * distance;
        points[2] = location - perpendicularDir * distance;

        return shuffle(points);
    }

    private Vector2[] getPointsStart(Vector2 location){

        Vector2[] points = new Vector2[]{
            location + Vector2.down * distance,
            location + Vector2.up * distance,
            location + Vector2.right * distance,
            location + Vector2.left * distance
        };

        return shuffle(points);
    }

    // @param
    // location = k
    // prevLocation = location
    private bool checkLocation(Vector2 location, Vector2 prevLocation){

        // Check if the location is inside the terrain
        if( !(location.x >= 0 && location.x <= (x-2)) || !(location.y >= 0 && location.y <= (y-2)) ){
            return false;
        }

        // In order to don't place a road near another one, it is checked if to the left or to the right of the point, where the road will be placed, there is already another one

        // The direction in which the road will be placed
        Vector2 dir = (location - prevLocation).normalized;

        // Perpendicular direction that helps to check the right and left point
        Vector2 perpendicularDir = Vector2.Perpendicular(dir);

        RaycastHit hit;

        float dist = Vector2.Distance(location, prevLocation);

        // In order to not check the points near the prevLocation(location, not k)
        prevLocation += dir;

        for(int i=0; i<dist-1; i++){

            Vector2 right = prevLocation + perpendicularDir;
            Vector2 right2 = prevLocation + perpendicularDir * 2;

            Vector2 left = prevLocation - perpendicularDir;
            Vector2 left2 = prevLocation - perpendicularDir * 2;

            bool isHit1 = Physics.Raycast(new Vector3(right.x , 80.0f, right.y), Vector3.down, out hit, Mathf.Infinity);
            bool isHit2 = Physics.Raycast(new Vector3(right2.x , 80.0f, right2.y), Vector3.down, out hit, Mathf.Infinity);
            if( !(isHit1 && isHit2) ){
                if(hit.collider.name == "Quad"){
                    return false;
                }
            }

            isHit1 = Physics.Raycast(new Vector3(right.x , 80.0f, right.y), Vector3.down, out hit, Mathf.Infinity);
            isHit2 = Physics.Raycast(new Vector3(right2.x , 80.0f, right2.y), Vector3.down, out hit, Mathf.Infinity);
            if( !(isHit1 && isHit2) ){
                if(hit.collider.name == "Quad"){
                    return false;
                }
            }

            // Check if along the direction where will be placed the road there is already another road
            isHit1 = Physics.Raycast(new Vector3(prevLocation.x , 80.0f, prevLocation.y), Vector3.down, out hit, Mathf.Infinity); 
            if(isHit1){
                if(hit.collider.name == "Quad"){
                    return false;
                }
            }

            prevLocation += dir;

        }

        return true;
    }

    private Vector2[] shuffle(Vector2[] array){
        System.Random r = new System.Random();

        array = array.OrderBy(x => r.Next()).ToArray();

        return array;
    }
}
