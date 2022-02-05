public class CoastlineAgents{

    private int tokens;

    public enum directions {Nord, Sud, Est, Ovest, NordEst, NordOvest, SudEst, SudOvest};
    private directions direction;
    
    //private Vector2 seedPoint;

    //private Vector2[] border;

    //private bool horizontal;

    private int numVertex;

    public CoastlineAgents(int tokens, int direction, int numVertex){
        this.tokens = tokens;
        this.direction = convertDirection(direction);
        //this.seedPoint = seedPoint;
        //this.border = border;
        //this.horizontal = horizontal;
        this.numVertex = numVertex;
    }

    private directions convertDirection(int direction){
        directions[] dir = new[] {directions.Nord, directions.Sud, directions.Est, directions.Ovest, directions.NordEst, directions.NordEst, directions.SudEst, directions.SudOvest};
        return dir[direction];
    }

    public string getDirection(){
        return this.direction.ToString();
    }

    public int getTokens(){
        return this.tokens;
    }

    /*public Vector2 getSeedPoint(){
        return this.seedPoint;
    }*/

    /*public bool isHorizontal(){
        return this.horizontal;
    }*/

    /*public Vector2[] getBorder(){
        return this.border;
    }*/

    public int getVertex(){
        return  this.numVertex;
    }

    public override string ToString(){
        return "|Tokens: " + this.tokens + "|Direction: " + this.direction.ToString() + "|Vertex: " + this.numVertex + "|";
    }
}
