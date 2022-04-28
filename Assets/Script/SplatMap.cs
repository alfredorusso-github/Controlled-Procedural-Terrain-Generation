using UnityEngine;

[RequireComponent(typeof(Terrain))]

public class SplatMap : MonoBehaviour
{
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

    // Instance of this class
    public static SplatMap Instance;

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

        // Flat all point
        flatAllPoints();

        // Change texture of the map to sea level
        makeSplatMap();
    }

    public void makeSplatMap(){

        heightmap = td.GetHeights(0, 0, x, y);

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

    private void flatAllPoints(){
        
        for(int i=0; i<x; i++){
            for(int j=0; j<y; j++){
                heightmap[i,j] = 0.0f;
            }
        }
        td.SetHeights(0, 0, heightmap);
    }
}
