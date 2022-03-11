using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

[RequireComponent(typeof(Terrain))]

public class TerrainScript : MonoBehaviour{
    public bool makeItFlat = false;

    //Coastline agent
    public int CoastlineTokens = 5;
    public int limit = 5;
    ArrayList agents = new ArrayList();

    //Smoothing agent
    public int smoothingAgentsNr;
    public int returnValue;
    public int smoothingTokens;

    //Beach agent
    public bool UseBeachAgents;
    public int beachAgentsNr;
    public int BeachTokens;
    public float heighLimit;
    public int randomWalkSize;
    public int awayLimit;
    [Range(.003f, .01f)] public float beachHeight;
    private ArrayList shorelinePoint = new ArrayList();

    //Terrain data
    private Terrain terrain;
    private TerrainData td;
    private int x;
    private int y;
    private float [,] heightmap;

    //SplatMap
    [System.Serializable]
    public class SplatHeights{
        public int textureIndex;
        public float startingHeight;
    }
    public SplatHeights[] splatHeights;

    
    // Start is called before the first frame update
    void Start()
    {
        //Getting terrain information
        terrain = GetComponent<Terrain> ();
        td = terrain.terrainData;
        x = td.heightmapResolution;
        y = td.heightmapResolution;

        //Inizialize heighmap
        heightmap = new float[x,y];
        flat();
        makeSplatMap();

        //Make flat terrain if the checkbox is checked
        if(makeItFlat){
            return;
        }

        //Place first coastline agent on terrain and retrieve his children.
        //The children are placed on the terrain and then they are ready for the action.
        //Vector2[] border = new Vector2[]{new Vector2(0, 0), new Vector2(x-1, y-1)}; 
        CoastlineAgents ca = new CoastlineAgents(CoastlineTokens, getDirection(), (x-1)*(y-1));
        GetCoastlineAgents(ca);
        StartCoroutine(CoastlineActionsCoroutine());

    }

    //----------------------------- Utilities -----------------------------
    private void flat(){
        
        for(int i=0; i<x; i++){
            for(int j=0; j<y; j++){
                heightmap[i,j] = 0.0f;
            }
        }
        td.SetHeights(0, 0, heightmap);
    }

    private void makeSplatMap(){
        float[, ,] splatMap = new float[td.alphamapHeight, td.alphamapWidth, td.alphamapLayers];

        for(int y=0; y<td.alphamapHeight; y++){
            for(int x=0; x<td.alphamapWidth; x++){

                float height = heightmap[x, y];

                float[] splat = new float[splatHeights.Length];

                for(int i=0; i<splatHeights.Length; i++){
                    
                    if(i == splatHeights.Length - 1 && height >= splatHeights[i].startingHeight){
                        splat[i] = 1;
                    }
                    else if(height >= splatHeights[i].startingHeight && height <= splatHeights[i+1].startingHeight){
                        splat[i] = 1;
                    }
                }

                for(int j=0; j<splatHeights.Length; j++){
                    splatMap[x, y, j] = splat[j];
                }
            }
        } 

        td.SetAlphamaps(0, 0, splatMap);
    }

    private Vector2[] convertArrayList(ArrayList points){
        Vector2[] result = new Vector2[points.Count];

        int i = 0;
        foreach(Vector2 point in points){
            result[i] = point;
            i++;
        }

        return result;
    }

    private Vector2[] convertList(List<Vector2> points){
        Vector2[] result = new Vector2[points.Count];

        int i = 0;
        foreach(Vector2 point in points){
            result[i] = point;
            i++;
        }

        return result;
    }

    private void writePoints(Vector2 start, Vector2 end){
        //Debug.Log("start: " + start.ToString() + ", end: " + end.ToString());
        for(int i=(int)start.x; i<(int)end.x; i++){
            for(int j=(int)start.y; j<(int)end.y; j++){
                heightmap[j,i] = .001f;
            }
        }
        td.SetHeights(0, 0, heightmap);
    }

    private void writePoint(Vector2 point){
        heightmap[(int)point.y, (int)point.x] = Random.Range(0.5f, 0.6f);
        td.SetHeights(0, 0, heightmap);
    }

    //----------------------------- Coastline Agent -----------------------------
    private void GetCoastlineAgents(CoastlineAgents ca){
        
        if(ca.getVertex() >= this.limit){
            for(int i=0; i<2; i++){
                GetCoastlineAgents(new CoastlineAgents(ca.getTokens(), getDirection(), ca.getVertex()/2));
            }                    
        }
        else{
            agents.Add(ca);
        }
    }

    private void CoastlineActions(){

        List<System.TimeSpan> fsp = new List<System.TimeSpan>();
        List<System.TimeSpan> actions = new List<System.TimeSpan>();
        
        foreach(CoastlineAgents agent in agents){
            
            System.DateTime start = System.DateTime.Now;
            Vector2 startingPoint = findStartingPoint(agent, new Vector2(256.0f, 256.0f));
            System.DateTime end = System.DateTime.Now;
            fsp.Add(end-start);

            //Per come ho strutturato l'algoritmo non ha senso calcolare l'attrattore e il repulsore quando si piazza l'agente perché dato che questo viene piazzato
            //al centro della mappa (in questo caso nel punto (256, 256)) utilizzando lo stesso attrattore e repulsore ottengo per ogni token gli stessi punti elevati.
            //Vector2[] ar = getAttractorAndRepulsor(new Vector2[]{new Vector2(0, 0), new Vector2(x-1, y-1)});

            start = System.DateTime.Now;
            for(int i=0; i<agent.getTokens(); i++){

                Vector2[] adjacentPoints = new Vector2[]{
                    new Vector2(startingPoint.x+1, startingPoint.y), 
                    new Vector2(startingPoint.x, startingPoint.y+1), 
                    new Vector2(startingPoint.x-1, startingPoint.y), 
                    new Vector2(startingPoint.x, startingPoint.y-1),
                    new Vector2(startingPoint.x+1, startingPoint.y+1), 
                    new Vector2(startingPoint.x+1, startingPoint.y-1), 
                    new Vector2(startingPoint.x-1, startingPoint.y-1), 
                    new Vector2(startingPoint.x-1, startingPoint.y+1)
                };
                
                List<Vector2> check = new List<Vector2>();
                foreach(Vector2 point in adjacentPoints){
                    if((point.x >= 0 && point.x <= (x-1)) && (point.y >= 0 && point.y <= (y-1))){
                        check.Add(point);
                    }
                }

                Vector2[] shuffledArray = shuffle(convertList(check));
                List<float> scores = new List<float>();

                for(int j=0; j<shuffledArray.Length; j++){
                    float tmp = scorePoint(shuffledArray[j], agent, getAttractorAndRepulsor(new Vector2[]{new Vector2(0, 0), new Vector2(x-1, y-1)}));
                    scores.Add(tmp);
                }

                startingPoint = shuffledArray[scores.IndexOf(scores.Max())];

                if(heightmap[(int)startingPoint.y, (int)startingPoint.x] == 0){
                    heightmap[(int)startingPoint.y, (int)startingPoint.x] = Random.Range(0.02f, 0.5f);
                }
            }
            end = System.DateTime.Now;
            actions.Add(end-start);
        }

        Debug.Log("Finding point milliseconds: " + fsp.Sum(item => item.Milliseconds));
        Debug.Log("Actions: " + actions.Sum(item => item.Milliseconds));
        //Debug.Log("Number Point Elevated: " + pointToElevate.Count);

        td.SetHeights(0, 0, heightmap);
    }

    private IEnumerator CoastlineActionsCoroutine(){
        
        foreach(CoastlineAgents agent in agents){
            
            Vector2 startingPoint = findStartingPoint(agent, new Vector2(256.0f, 256.0f));

            //Per come ho strutturato l'algoritmo non ha senso calcolare l'attrattore e il repulsore quando si piazza l'agente perché dato che questo viene piazzato
            //al centro della mappa (in questo caso nel punto (256, 256)) utilizzando lo stesso attrattore e repulsore ottengo per ogni token gli stessi punti elevati.
            //Vector2[] ar = getAttractorAndRepulsor(new Vector2[]{new Vector2(0, 0), new Vector2(x-1, y-1)});

            for(int i=0; i<agent.getTokens(); i++){

                Vector2[] adjacentPoints = new Vector2[]{
                    new Vector2(startingPoint.x+1, startingPoint.y), 
                    new Vector2(startingPoint.x, startingPoint.y+1), 
                    new Vector2(startingPoint.x-1, startingPoint.y), 
                    new Vector2(startingPoint.x, startingPoint.y-1),
                    new Vector2(startingPoint.x+1, startingPoint.y+1), 
                    new Vector2(startingPoint.x+1, startingPoint.y-1), 
                    new Vector2(startingPoint.x-1, startingPoint.y-1), 
                    new Vector2(startingPoint.x-1, startingPoint.y+1)
                };
                
                List<Vector2> check = new List<Vector2>();
                foreach(Vector2 point in adjacentPoints){
                    if((point.x >= 0 && point.x <= (x-1)) && (point.y >= 0 && point.y <= (y-1))){
                        check.Add(point);
                    }
                }

                Vector2[] shuffledArray = shuffle(convertList(check));
                List<float> scores = new List<float>();

                for(int j=0; j<shuffledArray.Length; j++){
                    float tmp = scorePoint(shuffledArray[j], agent, getAttractorAndRepulsor(new Vector2[]{new Vector2(0, 0), new Vector2(x-1, y-1)}));
                    scores.Add(tmp);
                }

                startingPoint = shuffledArray[scores.IndexOf(scores.Max())];

                if(heightmap[(int)startingPoint.y, (int)startingPoint.x] == 0){
                    heightmap[(int)startingPoint.y, (int)startingPoint.x] = Random.Range(0.02f, 0.5f);
                }
            }

            td.SetHeights(0, 0, heightmap);
            yield return new WaitForEndOfFrame();
        }

        yield return smoothActionCoroutine();
    }

    private Vector2 findStartingPoint(CoastlineAgents ca, Vector2 position){
        Vector2 startingPoint = position;

        while(isSurroundingLanded(startingPoint) == new Vector2(-1, -1)){
            switch(ca.getDirection()){

                case "Nord": 
                    startingPoint.Set(startingPoint.x, startingPoint.y+1);
                    //Debug.Log("Nord sp" + startingPoint);
                    break;

                case "Sud":
                    startingPoint.Set(startingPoint.x, startingPoint.y-1);
                    //Debug.Log("Sud sp" + startingPoint);
                    break;
                
                case "Est":
                    startingPoint.Set(startingPoint.x-1, startingPoint.y);
                    //Debug.Log("Est sp" + startingPoint);
                    break;
                
                case "Ovest":
                    startingPoint.Set(startingPoint.x+1, startingPoint.y);
                    //Debug.Log("Ovest sp" + startingPoint);
                    break;

                case "NordEst": 
                    startingPoint.Set(startingPoint.x+1, startingPoint.y+1);
                    //Debug.Log("NordEst sp" + startingPoint);
                    break;                

                case "NordOvest": 
                    startingPoint.Set(startingPoint.x-1, startingPoint.y+1);
                    //Debug.Log("NordOvest sp" + startingPoint);
                    break;

                case "SudEst": 
                    startingPoint.Set(startingPoint.x+1, startingPoint.y-1);
                    //Debug.Log("SudEst sp" + startingPoint);
                    break;

                case "SudOvest": 
                    startingPoint.Set(startingPoint.x+1, startingPoint.y-1);
                    //Debug.Log("SudOvest sp" + startingPoint);
                    break;
            }
        }
    
        return getStartingPoint(startingPoint);
    }

    private Vector2 isSurroundingLanded(Vector2 position){
        Vector2[] adjacentPoints = new Vector2[]{
            new Vector2(position.x+1, position.y), 
            new Vector2(position.x, position.y+1), 
            new Vector2(position.x-1, position.y), 
            new Vector2(position.x, position.y-1),
            new Vector2(position.x+1, position.y+1), 
            new Vector2(position.x+1, position.y-1), 
            new Vector2(position.x-1, position.y-1), 
            new Vector2(position.x-1, position.y+1)
        };

        //Qui non viene usato lo shuffle perché inutile, dato che viene controllato solo se nei punti adiacenti alla posizione data esiste almeno un punto che
        //che si trova al di sotto del livello del mare.
        foreach(Vector2 point in adjacentPoints){
            if(heightmap[(int)point.y, (int)point.x] == 0){
                return point;
            }
        }

        return new Vector2(-1, -1);
    }

    private Vector2 getStartingPoint(Vector2 position){
        Vector2[] adjacentPoints = new Vector2[]{
            new Vector2(position.x+1, position.y), 
            new Vector2(position.x, position.y+1), 
            new Vector2(position.x-1, position.y), 
            new Vector2(position.x, position.y-1),
            new Vector2(position.x+1, position.y+1), 
            new Vector2(position.x+1, position.y-1), 
            new Vector2(position.x-1, position.y-1), 
            new Vector2(position.x-1, position.y+1)
        };
        
        //Sappiamo che quando entriamo in tale metodo a partire dalla posizione in input esiste almeno un punto adiacente che si trova al di sotto del livello del mare,
        //tuttavia é possibile che esista piú di un punto adiacente al di sotto del livello del mare e usiamo la funzione shuffle mischire l'array sopra e prendere uno dei 
        //punti in maniera casuale.
        Vector2[] tmp = shuffle(adjacentPoints); 
        foreach(Vector2 point in tmp){
            if(heightmap[(int)point.y, (int)point.x] == 0){
                return point;
            }
        }

        return new Vector2(-1, -1);
    }

    private Vector2[] shuffle(Vector2[] array){
        System.Random r = new System.Random();

        array = array.OrderBy(x => r.Next()).ToArray();

        return array;
    }

    //Direction are intended in world coordinates.
    private int getDirection(){
        return Random.Range(0, 8);       
    }

    private float scorePoint(Vector2 point, CoastlineAgents agent, Vector2[] ar){
        float result = Mathf.Pow(Vector2.Distance(point, ar[1]), 2.0f) - Mathf.Pow(Vector2.Distance(point, ar[0]), 2.0f) + (3 * Mathf.Pow(getClosestDistance(point, agent), 2.0f));
        //Debug.Log("border: " + agent.getBorder()[0].ToString() + " " + agent.getBorder()[1].ToString() + " Point: " + point.ToString() + " Score: " +  result);
        return result;
    }

    private float getClosestDistance(Vector2 point, CoastlineAgents agent){

        //Order of the point inside the array to respect the Point point received from input: left, right, up, down 
        Vector2[] borderPoints = new Vector2[]{new Vector2(0, point.y), new Vector2(x, point.y), 
            new Vector2(point.x, y), new Vector2(point.x, 0)};

        float[] result = new float[borderPoints.Length];
        for(int i=0; i<result.Length; i++){
            result[i] = Vector2.Distance(point, borderPoints[i]);
        }

        return result.OrderBy(a => a).ToArray()[0];
    }

    private Vector2[] getAttractorAndRepulsor(Vector2[] range){
        int attractorX = Random.Range((int)range[0].x, (int)range[1].x);
        int attractorY = Random.Range((int)range[0].y, (int)range[1].y);

        //L'idea é ti utilizzare la formula per trovare il punto diametricalmente opposto a un punto dato(nel nostro caso l'attrattore) di un cerchio,
        //che nel nostro caso sará il cerchio circosritto al quadrato che é il nostro piano.
        //Dato un punto di coordinate (a, b) il punto diametricalmente opposto avrá la coordinata x che sará (a1 + a)/2 = 256 (il valore 256 mi é dato dalla coordinata x del centro del cerchio)
        //Lo stesso vale per la coordinata y, che sará (b1 + b)/2 = 256. a1 e b1 sono le incognite del punto diametricalmente opposto che cerco. 
        //Inoltre il punto di partenza che é l'attrattore viene calcolato in modo randomico all'interno del piano.
        //int repulsorX = (x-1) - attractorX;
        //int repulsorY = (y-1) - attractorY;

        int repulsorX = Random.Range((int)range[0].x, (int)range[1].x);
        int repulsorY = Random.Range((int)range[0].y, (int)range[1].y);

        //Debug.Log("Attractor: (" + attractorX + ", " + attractorY + ") " + "Repulsor: (" + repulsorX + ", " + repulsorY + ")");

        return new Vector2[]{new Vector2(attractorX, attractorY), new Vector2(repulsorX, repulsorY)};
    }

    private void checkAR(){
        
        foreach(CoastlineAgents agent in agents){
            Vector2[] ar = getAttractorAndRepulsor(new Vector2[]{new Vector2(0, 0), new Vector2(x-1, y-1)});
            
            string result = "";
            for(int i=0; i<ar.Length; i++){
                if(i==0){
                    result += "Attractor: " + ar[i].ToString(); 
                }
                else{
                    result += " Repulsor: " + ar[i].ToString();
                }
            }
            Debug.Log(result);
        }
    }
    
    private Vector2 getEdgePoint(Vector2 start, Vector2 end){
        
        int iORj = Random.Range(0, 2);

        int i = 0;
        int j = 0;
        if(iORj == 0){
            i = Random.Range((int)start.x, (int)end.x);
            int choice = Random.Range(0, 2);
            if(choice == 0){
                j = (int)start.y;
            }
            else{
                j = (int)end.y;
            }
        }
        else if(iORj == 1){
            j = Random.Range((int)start.y, (int)end.y);
            int choice = Random.Range(0, 2);
            if(choice == 0){
                i = (int)start.x;
            }
            else{
                i = (int)end.x;
            }
        }

        //Debug.Log("i: " + i + " j: " + j);

        return new Vector2(i, j);
    }

    /*private void CoastlineActions(){
        ArrayList pointToElevate = new ArrayList(); 

        foreach(CoastlineAgents agent in agents){
            
            Vector2 point = getEdgePoint(agent.getBorder()[0], agent.getBorder()[1]);
            for(int i=0; i<agent.getTokens(); i++){

                //Debug.Log("EdgePoint: " + point.ToString());
                Vector2[] adjacentPoints = new Vector2[]{new Vector2(point.x+1, point.y), new Vector2(point.x, point.y+1), new Vector2(point.x-1, point.y), new Vector2(point.x, point.y-1),
                    new Vector2(point.x+1, point.y+1), new Vector2(point.x+1, point.y-1), new Vector2(point.x-1, point.y-1), new Vector2(point.x-1, point.y+1)};
                SortedDictionary<float, int> scores = new SortedDictionary<float, int>();
                for(int j=0; j<adjacentPoints.Length; j++){
                    float tmp = scorePoint(adjacentPoints[j], agent);
                    if(!scores.ContainsKey(tmp)){
                        scores.Add(tmp, j);
                    }
                }
                pointToElevate.Add(adjacentPoints[scores.Values.Last()]);
                point = adjacentPoints[scores.Values.Last()];
            }

        }

        foreach(Vector2 point in pointToElevate){
            Debug.Log("Point " + point.ToString());
        }
        writePoints(convertArrayList(pointToElevate));
    }*/

    /*Vector2[] border = ca.getBorder();

            for(int j = 0; j <= y-(int)border[1].y; j += (int)border[1].y){
                for(int i = 0; i <= x-(int)border[1].x; i += (int)border[1].x){
                    Vector2 point1 = new Vector2(border[0].x + i, border[0].y + j);
                    Vector2 point2 = new Vector2(border[1].x + i, border[1].y + j);
                    agents.Add(new CoastlineAgents(CoastlineTokens, getDirection(), getEdgePoint(point1, point2), new Vector2[]{point1, point2}, ca.isHorizontal(), ca.getVertex()));
                }
            } */


    //----------------------------- Smoothing Agent -----------------------------
    private void SmoothingAction(){
        
        System.DateTime start = System.DateTime.Now;
        for(int i=0; i<smoothingAgentsNr; i++){
            smooth(getStartingPoint());
        }
        flatPoints();

        td.SetHeights(0, 0, heightmap);
        System.DateTime end = System.DateTime.Now;
        Debug.Log("SmoothingAction Time: " + (end-start).Milliseconds);
    }

    private void smooth(Vector2 startingPoint){

        //Count for checking when the smoothing agent need to return to startingPoint
        int count = 0;

        Vector2 location = startingPoint;

        for(int i=0; i<smoothingTokens; i++){

            if(count > smoothingTokens/returnValue){
                location = startingPoint;
                count = 0;
            }
            else{
                //adjusting the value of the point in position location
                float newHeight = VonNeumannNeighborhood(location);
                heightmap[(int)location.y, (int)location.x] = newHeight;
                
                location = getNeighboringPoint(location);
                count++;
            }
        }
    }

    private IEnumerator smoothActionCoroutine(){

        for(int i=0; i<smoothingAgentsNr; i++){

            Vector2 startingPoint = getStartingPoint();

            //Count for checking when the smoothing agent need to return to startingPoint
            int count = 0;

            Vector2 location = startingPoint;

            for(int j=0; j<smoothingTokens; j++){

                if(count > smoothingTokens/returnValue){
                    location = startingPoint;
                    count = 0;
                }
                else{
                    //adjusting the value of the point in position location
                    float newHeight = VonNeumannNeighborhood(location);
                    heightmap[(int)location.y, (int)location.x] = newHeight;
                    
                    location = getNeighboringPoint(location);
                    count++;
                }
            }

            td.SetHeights(0, 0, heightmap);
            yield return new WaitForEndOfFrame();
        }

        flatPoints();
        
        if(UseBeachAgents){
            yield return BeachActionCoroutine();
        }
        else{
            makeSplatMap();
        }
    }

    private Vector2 getStartingPoint(){

        int Y = Random.Range(0, y);
        int X = Random.Range(0, x);

        while(heightmap[Y, X] == 0){
            Y = Random.Range(0, y);
            X = Random.Range(0, x);
        }

        return new Vector2(X, Y); 
    }

    private Vector2 getNeighboringPoint(Vector2 position){

        Vector2 [] neighboringPoint = new Vector2[]{
            new Vector2(position.x+1, position.y), 
            new Vector2(position.x-1, position.y), 
            new Vector2(position.x, position.y+1), 
            new Vector2(position.x, position.y-1)
        };

        // if the agents never move itself outside the maps, the list will always have at least three point inside
        List<Vector2> check = new List<Vector2>();
        foreach(Vector2 point in neighboringPoint){
            if((point.x >= 0 && point.x <= (x-1)) && (point.y >= 0 && point.y <= (y-1))){
                check.Add(point);
            }
        }

        return check[Random.Range(0, check.Count)];
    }

    private float VonNeumannNeighborhood(Vector2 position){
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

        float centralPointHeight = heightmap[(int)position.y, (int)position.x]; 
        
        Vector2 [] surroundingPoints = new Vector2[] {
            new Vector2(position.x+1, position.y), 
            new Vector2(position.x-1, position.y), 
            new Vector2(position.x, position.y+1),
            new Vector2(position.x, position.y-1), 
            new Vector2(position.x+2, position.y), 
            new Vector2(position.x-2, position.y), 
            new Vector2(position.x, position.y+2),
            new Vector2(position.x, position.y-2)
        };

        float [] heights = new float[surroundingPoints.Length];

        for(int i=0; i<surroundingPoints.Length; i++){
            
            if( (surroundingPoints[i].x >= 0 && surroundingPoints[i].x <= (x-1)) && (surroundingPoints[i].y >= 0 && surroundingPoints[i].y <= (y-1)) ){
                heights[i] = heightmap[(int)surroundingPoints[i].y, (int)surroundingPoints[i].x];
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

    //----------------------------- Beach Agent -----------------------------

    private void BeachAction(){
        System.DateTime start = System.DateTime.Now;
        getShorelinePoint();

        for(int i=0; i<beachAgentsNr; i++){
            generateBeach((Vector2) shorelinePoint[Random.Range(0, shorelinePoint.Count)]);
        }
        System.DateTime end = System.DateTime.Now;
        Debug.Log("Beach Action Time: " + (end-start).Milliseconds);
    }

    private void generateBeach(Vector2 startingPoint){
        Vector2 location = startingPoint;

        for(int i=0; i<BeachTokens; i++){

            if(heightmap[(int)location.y, (int)location.x] >= heighLimit){
                //location = random shoreline point
                location = (Vector2) shorelinePoint[Random.Range(0, shorelinePoint.Count)];
            }

            //flatten area.
            heightmap[(int) location.y, (int)location.x] = Random.Range(.003f, beachHeight);
            foreach(Vector2 point in neighboringPoint(location)){
                heightmap[(int) point.y, (int)point.x] = Random.Range(.003f, beachHeight);
            }
            
            //smooth area. After i flatten the point and the nearby points i can smooth the area.
            float newHigh = VonNeumannNeighborhood(location);
            heightmap[(int) location.y, (int) location.x] = newHigh;
            foreach(Vector2 point in neighboringPoint(location)){
                heightmap[(int) point.y, (int)point.x] = VonNeumannNeighborhood(point);
            }

            //setting away with a random point in a short distance away from location
            Vector2 away = awayRandomPoint(location);

            for(int j=0; j<randomWalkSize; j++){

                if(heightmap[(int) away.y, (int) away.x] <= 0.01f){
                    
                    //flatten area around away
                    heightmap[(int) away.y, (int) away.x] = Random.Range(.003f, beachHeight);
                    foreach(Vector2 point in neighboringPoint(away)){
                        heightmap[(int) point.y, (int) point.x] = Random.Range(.003f, beachHeight);
                    }

                    //smooth area around away
                    heightmap[(int) away.y, (int) away.x] = VonNeumannNeighborhood(away);
                    foreach(Vector2 point in neighboringPoint(away)){
                        heightmap[(int) point.y, (int) point.x] = VonNeumannNeighborhood(point);
                    }
                    
                }

                away = getNeighboringPoint(away);                
            }

            location = getNeighboringPoint(location);
        }

        td.SetHeights(0, 0, heightmap);
    }

    private IEnumerator BeachActionCoroutine(){
        getShorelinePoint();

        for(int i=0; i<beachAgentsNr; i++){
            
            Vector2 location = (Vector2) shorelinePoint[Random.Range(0, shorelinePoint.Count)];

            for(int j=0; j<BeachTokens; j++){

                if(heightmap[(int)location.y, (int)location.x] >= heighLimit){
                    //location = random shoreline point
                    location = (Vector2) shorelinePoint[Random.Range(0, shorelinePoint.Count)];
                }

                //flatten area.
                heightmap[(int) location.y, (int)location.x] = Random.Range(.003f, beachHeight);
                foreach(Vector2 point in neighboringPoint(location)){
                    heightmap[(int) point.y, (int)point.x] = Random.Range(.003f, beachHeight);
                }
                
                //smooth area. After i flatten the point and the nearby points i can smooth the area.
                float newHigh = VonNeumannNeighborhood(location);
                heightmap[(int) location.y, (int) location.x] = newHigh;
                foreach(Vector2 point in neighboringPoint(location)){
                    heightmap[(int) point.y, (int)point.x] = VonNeumannNeighborhood(point);
                }

                //setting away with a random point in a short distance away from location
                Vector2 away = awayRandomPoint(location);

                for(int k=0; k<randomWalkSize; k++){

                    if(heightmap[(int) away.y, (int) away.x] <= 0.01f){
                        
                        //flatten area around away
                        heightmap[(int) away.y, (int) away.x] = Random.Range(.003f, beachHeight);
                        foreach(Vector2 point in neighboringPoint(away)){
                            heightmap[(int) point.y, (int) point.x] = Random.Range(.003f, beachHeight);
                        }

                        //smooth area around away
                        heightmap[(int) away.y, (int) away.x] = VonNeumannNeighborhood(away);
                        foreach(Vector2 point in neighboringPoint(away)){
                            heightmap[(int) point.y, (int) point.x] = VonNeumannNeighborhood(point);
                        }
                        
                    }

                    away = getNeighboringPoint(away);                
                }

                location = getNeighboringPoint(location);
            }

            td.SetHeights(0, 0, heightmap);
            yield return new WaitForEndOfFrame();
        }

        makeSplatMap();
    }

    private void getShorelinePoint(){

        for(int i=0; i<x; i++){
            for(int j=0; j<y; j++){
                if(heightmap[j, i] >= .003f && heightmap[j, i] <= .006f){
                    if(i + awayLimit < x && j + awayLimit < y){
                        shorelinePoint.Add(new Vector2(i, j));
                    }
                }
            }
        }
    }

    private Vector2[] neighboringPoint(Vector2 position){


        Vector2[] points = new Vector2[]{
            new Vector2(position.x+1, position.y), 
            new Vector2(position.x-1, position.y), 
            new Vector2(position.x, position.y+1), 
            new Vector2(position.x, position.y-1),
            new Vector2(position.x+1, position.y+1),
            new Vector2(position.x-1, position.y+1),
            new Vector2(position.x+1, position.y-1),
            new Vector2(position.x-1, position.y-1)            
        };

        ArrayList check = new ArrayList();
        foreach(Vector2 point in points){
            if( (point.x >= 0 && point.x <= (x-1)) && (point.y >= 0 && point.y <= (y-1)) && heightmap[(int) point.y, (int)point.x] <= 0.01){
                check.Add(point);
            }
        }

        return convertArrayList(check);
        
    }

    private Vector2 awayRandomPoint(Vector2 position){

        Vector2 dir = (new Vector2((x-1)/2, (y-1)/2) - position).normalized;

        return position - (dir*awayLimit);
    }

}