using UnityEngine;

[RequireComponent(typeof(Terrain))]

public class SplatMap : MonoBehaviour
{
    //Terrain data
    private Terrain _terrain;
    private TerrainData _td;
    private int _x;
    private int _y;
    private float [,] _heightmap;

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
        _terrain = GetComponent<Terrain> ();
        _td = _terrain.terrainData;
        _x = _td.heightmapResolution;
        _y = _td.heightmapResolution;

        //Initialize heighmap
        _heightmap = new float[_x,_y];

        // Flat all point
        flatAllPoints();

        // Change texture of the map to sea level
        MakeSplatMap();
    }

    public void MakeSplatMap(){

        _heightmap = _td.GetHeights(0, 0, _x, _y);

        float[, ,] splatMap = new float[_td.alphamapHeight, _td.alphamapWidth, _td.alphamapLayers];

        for(int y=0; y<_td.alphamapHeight; y++){
            for(int x=0; x<_td.alphamapWidth; x++){

                float height = _heightmap[x, y];

                float[] splat = new float[splatHeights.Length];

                for(int i=0; i<splatHeights.Length; i++){
                    
                    if(i == splatHeights.Length - 1 && height >= splatHeights[i].startingHeight){
                        splat[i] = 1;
                    }
                    else if(height >= splatHeights[i].startingHeight && height < splatHeights[i+1].startingHeight){
                        splat[i] = 1;
                    }
                }

                for(int j=0; j<splatHeights.Length; j++){
                    splatMap[x, y, j] = splat[j];
                }
            }
        } 

        _td.SetAlphamaps(0, 0, splatMap);
    }

    private void flatAllPoints(){
        
        for(int i=0; i<_x; i++){
            for(int j=0; j<_y; j++){
                _heightmap[i,j] = 0.0f;
            }
        }
        _td.SetHeights(0, 0, _heightmap);
    }
}
