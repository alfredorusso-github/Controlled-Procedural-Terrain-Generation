using UnityEngine;

public class CommentedScript : MonoBehaviour
{
    //----------------------------- Coastline Agent -----------------------------
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
            } 
    */

    // private Vector2 getEdgePoint(Vector2 start, Vector2 end){
        
    //     int iORj = Random.Range(0, 2);

    //     int i = 0;
    //     int j = 0;
    //     if(iORj == 0){
    //         i = Random.Range((int)start.x, (int)end.x);
    //         int choice = Random.Range(0, 2);
    //         if(choice == 0){
    //             j = (int)start.y;
    //         }
    //         else{
    //             j = (int)end.y;
    //         }
    //     }
    //     else if(iORj == 1){
    //         j = Random.Range((int)start.y, (int)end.y);
    //         int choice = Random.Range(0, 2);
    //         if(choice == 0){
    //             i = (int)start.x;
    //         }
    //         else{
    //             i = (int)end.x;
    //         }
    //     }

    //     //Debug.Log("i: " + i + " j: " + j);

    //     return new Vector2(i, j);
    // }
    
    // private Vector2 getRepulsor(Vector2 attractor)
    // {
        // Vector2 attractorDirection = (attractor - _center).normalized;

        // Calculating repulsor
        // Vector2 repulsor = new Vector2(Random.Range(0, x), Random.Range(0, y));
        // Vector2 repulsorDirection = (repulsor - _center).normalized;
        //
        // while (attractorDirection == repulsorDirection)
        // {
        //     repulsor = new Vector2(Random.Range(0, x), Random.Range(0, y));
        //     repulsorDirection = (repulsor - _center).normalized;
        // }
        //
    //     return repulsor;
    // }


    //----------------------------- City Agent -----------------------------

    // private IEnumerator cityActionCoroutine(){
        
    //     List<Vector2> coastlinePoints = getCoastlinePoints();
    //     System.Random random = new System.Random();
    //     Vector2 location = coastlinePoints[random.Next(coastlinePoints.Count)];

    //     // Getting the wolrd position for the agent and place it on the terrain
    //     Vector3 pos = terrain.GetPosition();
    //     Vector3 tmp = GetTerrainPos(pos.x+location.x, pos.z+location.y);
    //     var agent = Instantiate(cityAgent, tmp + (Vector3.up * 0.5f), Quaternion.identity);

    //     // Speed of the agent 
    //     float speed = 10.0f;      

    //     float aHeight = averageHeight();

    //     for(int i=0; i<cityAgentsNr; i++){

    //         for(int j=0; j<cityTokens; j++){
                
    //             int randomDistance = Random.Range(2, distance+1);

    //             // Getting the points where to place the house
    //             location += new Vector2((int)(Random.insideUnitCircle * randomDistance).x, (int)(Random.insideUnitCircle * randomDistance).y) * randomDistance;
    //             // while(!checkLocation(location, aHeight)){
    //             //     location += InsideArc(distance);
    //             // }

    //             Vector3 worldLocation = GetTerrainPos(pos.x+location.x, pos.z+location.y);

    //             Vector3 dir = (worldLocation - agent.transform.position).normalized; 

    //             agent.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

    //             float length = Vector3.Distance(worldLocation, agent.transform.position);
    //             Quaternion rot_road = Quaternion.LookRotation(dir, Vector3.up);
    //             Vector3 scale_road = new Vector3(1.0f, 0.06f, length);

    //             GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //             road.transform.localScale = scale_road;
    //             road.transform.rotation = rot_road;
    //             road.transform.position = worldLocation;
    //             road.transform.Translate(-Vector3.forward * length * 0.5f);
    //             road.layer = 8;

    //             while(agent.transform.position != worldLocation){
                    
    //                 // Moving the agent towards the new location on the terrain.
    //                 agent.transform.position = Vector3.MoveTowards(agent.transform.position, worldLocation, speed * Time.deltaTime);
    //                 yield return 0;
    //             }

    //             //Placing house
    //             // GameObject house = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //             // house.transform.position = agent.transform.position;

    //             // house.AddComponent<RayCaster>();

    //             // // Setting layer in order to avoid collision with agent
    //             // house.layer = 6;

    //             // // Setting house children of terrain
    //             // house.transform.parent = terrain.transform;

    //             yield return new WaitForSeconds(1);

    //         }

    //         location = coastlinePoints[random.Next(coastlinePoints.Count)];
    //         agent.transform.position = GetTerrainPos(pos.x+location.x, pos.z+location.y);
    //         yield return new WaitForSeconds(1);
    //     }
    // }


    // for(int k=0; k<points.Length; k++){

        //     if(!start){
        //         if(checkLocation(points[k], location) && !roadPoints.Contains(points[k])){
        //             checkedPoints.Add(points[k]);
        //         }
        //     }
        //     else{
        //         if(checkLocation(points[k], location)){
        //             checkedPoints.Add(points[k]);
        //         }
        //     }

        //     // Debug.Log("PrevLocation: " + prevLocation + " Point: " + points[k].ToString() + " CheckPoint: " + chcekPoint(points[k], prevLocation));
        // }

        // if(checkedPoints.Count != 0){
        //     return checkedPoints[0];
        // }
        
}
