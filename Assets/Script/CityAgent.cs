using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Terrain))]

public class CityAgent : MonoBehaviour
{
    //Terrain data
    private Terrain terrain;
    private TerrainData td;
    private int x;
    private int y;
    private float[,] heightmap;

    //City agent
    public int cityAgentsNr;
    public int cityTokens;
    public GameObject roads = null;
    public GameObject palace = null;
    public int gap;
    [Range(5, 20)] public int distance;
    public int maxNHouse
    {
        get
        {
            if (gap == 1)
            {
                return (distance / gap) - 2;
            }
            return (distance / gap) - 1;
        }
    }
    [HideInInspector]
    public int NumberOfHouse = 1;

    // Instance of this class
    public static CityAgent Instance;

    // OnDrawGizmos
    List<Vector2Int> points;
    bool start;

    Vector3 point;

    void Start()
    {

        //Getting terrain information
        terrain = GetComponent<Terrain>();
        td = terrain.terrainData;
        x = td.heightmapResolution;
        y = td.heightmapResolution;

        //Initialize heighmap
        heightmap = new float[x, y];

        points = new List<Vector2Int>();

        StartCoroutine(Action());
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        if (start)
        {
            foreach (var point in points)
            {
                Gizmos.DrawSphere(GetHeight(point), .2f);
            }
        }
    }

    private Vector3 GetHeight(Vector2 location)
    {
        //Create object to store raycast data
        RaycastHit hit;

        //Create origin for raycast that is above the terrain. I chose 100.
        Vector3 origin = new Vector3(location.x, 100, location.y);

        //Send the raycast.
        Physics.Raycast(origin, Vector3.down, out hit);

        return hit.point;
    }

    private IEnumerator Action()
    {

        heightmap = td.GetHeights(0, 0, x, y);

        List<Vector2Int> validPoints = getValidPoints();
        Debug.Log("Number of valid points: " + validPoints.Count);

        // this.start = true;

        // Getting terrain position
        Vector3 terrainPos = terrain.GetPosition();

        for (int i = 0; i < cityAgentsNr; i++)
        {

            Vector2Int location = validPoints[Random.Range(0, validPoints.Count)];

            Vector2Int previousLocation = location;

            location = getNewLocation(location, previousLocation);

            for (int j = 0; j < cityTokens; j++)
            {

                createRoad(location, previousLocation, terrainPos);
                createPalace(location, previousLocation, terrainPos);

                yield return new WaitForEndOfFrame();

                Vector2Int tmp = location;
                location = getNewLocation(location, previousLocation);

                if (location == tmp)
                {
                    Debug.Log("Agent can't find good point to place road");
                    break;
                }

                previousLocation = tmp;
            }

        }

        yield return TreeAgent.Instance.Action();
    }

    private List<Vector2Int> getValidPoints()
    {

        float ah = averageHeight();
        Debug.Log("Average height of the island: " + ah);

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                if (heightmap[j, i] > ah + .05f && checkSteepness(new Vector2(i, j)))
                {
                    points.Add(new Vector2Int(i, j));
                }
            }
        }

        return points;
    }

    private float averageHeight()
    {

        float sum = 0.0f;
        int nPoints = 0;

        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < y; j++)
            {
                if (heightmap[j, i] > 0.01f)
                {
                    sum += heightmap[j, i];
                    nPoints++;
                }
            }
        }

        return sum / nPoints;
    }

    private bool checkSteepness(Vector2 location)
    {

        //Create object to store raycast data
        RaycastHit hit;

        //Create origin for raycast that is above the terrain. I chose 100.
        Vector3 origin = new Vector3(location.x, 100, location.y);

        //Send the raycast.
        Physics.Raycast(origin, Vector3.down, out hit);

        if (Vector3.Angle(hit.normal, Vector3.up) < 7.0f)
        {
            return true;
        }

        return false;
    }

    private Vector2Int getNewLocation(Vector2Int location, Vector2Int previousLocation)
    {

        Vector2Int[] candidates;

        if (previousLocation != location)
        {
            candidates = getCandidates(location, previousLocation);
        }
        else
        {
            candidates = getCandidates(location);
        }

        List<Vector2Int> checkedCandidates = new List<Vector2Int>();

        for (int k = 0; k < candidates.Length; k++)
        {

            if (checkCandidate(candidates[k], location))
            {
                checkedCandidates.Add(candidates[k]);
            }

        }

        if (checkedCandidates.Count != 0)
        {
            return checkedCandidates[Random.Range(0, checkedCandidates.Count)];
        }

        return location;
    }

    private Vector2Int[] getCandidates(Vector2Int location)
    {

        Vector2Int[] points = new Vector2Int[]{
            location + Vector2Int.down * distance,
            location + Vector2Int.up * distance,
            location + Vector2Int.right * distance,
            location + Vector2Int.left * distance
        };

        return points;
    }

    private Vector2Int[] getCandidates(Vector2Int location, Vector2 prevLocation)
    {

        Vector2 dir = (location - prevLocation).normalized;
        Vector2 perpendicularDir = Vector2.Perpendicular(dir);

        Vector2Int[] points = new Vector2Int[3];

        points[0] = location + new Vector2Int((int)dir.x, (int)dir.y) * distance;
        points[1] = location + new Vector2Int((int)perpendicularDir.x, (int)perpendicularDir.y) * distance;
        points[2] = location - new Vector2Int((int)perpendicularDir.x, (int)perpendicularDir.y) * distance;

        return points;
    }

    // @param
    // location = k
    // prevLocation = location
    private bool checkCandidate(Vector2 location, Vector2 previousLocation)
    {

        // Check if the location is inside the terrain
        if (!(location.x >= 0 && location.x <= (x - 2)) || !(location.y >= 0 && location.y <= (y - 2)))
        {
            return false;
        }

        // Check the steepness of the point
        if (!checkSteepness(location))
        {
            return false;
        }

        // The direction in which the road will be placed
        Vector2 dir = (location - previousLocation).normalized;

        // Perpendicular direction that helps to check the right and left point
        Vector2 perpendicularDir = Vector2.Perpendicular(dir);

        RaycastHit hit1;
        RaycastHit hit2;

        float dist = Vector2.Distance(location, previousLocation);

        // In order to not check the points near the previousLocation(location, not k)
        previousLocation += dir;

        for (int i = 0; i < dist - 1; i++)
        {

            Vector2 right = previousLocation + perpendicularDir;
            Vector2 right2 = previousLocation + perpendicularDir * 2;

            Vector2 left = previousLocation - perpendicularDir;
            Vector2 left2 = previousLocation - perpendicularDir * 2;

            bool isHit1 = Physics.Raycast(new Vector3(right.x, 80.0f, right.y), Vector3.down, out hit1, Mathf.Infinity);
            bool isHit2 = Physics.Raycast(new Vector3(right2.x, 80.0f, right2.y), Vector3.down, out hit2, Mathf.Infinity);
            if (isHit1 && isHit2)
            {
                if ((hit1.collider.name == "Quad") != (hit2.collider.name == "Quad"))
                {
                    return false;
                }
            }

            isHit1 = Physics.Raycast(new Vector3(left.x, 80.0f, left.y), Vector3.down, out hit1, Mathf.Infinity);
            isHit2 = Physics.Raycast(new Vector3(left2.x, 80.0f, left2.y), Vector3.down, out hit2, Mathf.Infinity);
            if (isHit1 && isHit2)
            {
                if ((hit1.collider.name == "Quad") != (hit2.collider.name == "Quad"))
                {
                    return false;
                }
            }

            // Check if along the direction where will be placed the road, there is already another road
            isHit1 = Physics.Raycast(new Vector3(previousLocation.x, 80.0f, previousLocation.y), Vector3.down, out hit1, Mathf.Infinity);
            if (isHit1)
            {
                if (hit1.collider.name == "Quad")
                {
                    return false;
                }
            }

            previousLocation += dir;

        }

        return true;
    }

    private void createRoad(Vector2 location, Vector2 prevLocation, Vector3 pos)
    {

        Vector3 worldLocation = GetHeight(new Vector2(location.x + pos.x, location.y + pos.z));
        Vector3 prevWorldLocation = GetHeight(new Vector2(prevLocation.x + pos.x, prevLocation.y + pos.z));

        Vector3 dir = (worldLocation - prevWorldLocation).normalized;

        float length = Vector3.Distance(worldLocation, prevWorldLocation);
        Quaternion rot_road = Quaternion.identity;
        if (dir != Vector3.zero)
        {
            rot_road = Quaternion.LookRotation(dir, Vector3.up);
        }
        Vector3 scale_road = new Vector3(1.0f, 1.0f, length);

        var road = Instantiate(roads, worldLocation, rot_road);
        road.transform.Translate(Vector3.up * .1f);
        road.transform.Translate(-Vector3.forward * (length + 1.0f) * 0.5f);
        road.transform.localScale = scale_road;
    }

    private void createPalace(Vector2 location, Vector2 prevLocation, Vector3 pos)
    {

        Vector3 worldLocation = GetHeight(new Vector2(location.x + pos.x, location.y + pos.z));
        Vector3 prevWorldLocation = GetHeight(new Vector2(prevLocation.x + pos.x, prevLocation.y + pos.z));

        Vector3 dir = (worldLocation - prevWorldLocation).normalized;

        // Debug.Log("WorldLocation: " + worldLocation + " Prev: " + prevWorldLocation + " Direction: " + dir + " Cross with Vector3.up: " + Vector3.Cross(dir, Vector3.up));

        Vector3 palace_pos = Vector3.Lerp(worldLocation, prevWorldLocation, 0.5f);
        Quaternion rot_palace = Quaternion.identity;
        if (dir != Vector3.zero)
        {
            rot_palace = Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up), Vector3.up);
        }

        var pal = Instantiate(palace, palace_pos, rot_palace);
        pal.transform.Translate(Vector3.up * 0.5f);
        pal.transform.Translate(-Vector3.forward);
        // pal.AddComponent<RayCaster>();

        if (NumberOfHouse == 1)
        {
            return;
        }

        Vector3 pos_right = palace_pos;
        Vector3 pos_left = palace_pos;

        for (int i = 0; i < NumberOfHouse - 1; i++)
        {

            if (i % 2 == 0)
            {
                pos_right += dir * gap;
                pal = Instantiate(palace, pos_right, rot_palace);
                pal.transform.Translate(Vector3.up * 0.5f);
                pal.transform.Translate(-Vector3.forward);
                // pal.AddComponent<RayCaster>();
            }
            else
            {
                pos_left -= dir * gap;
                pal = Instantiate(palace, pos_left, rot_palace);
                pal.transform.Translate(Vector3.up * 0.5f);
                pal.transform.Translate(-Vector3.forward);
                // pal.AddComponent<RayCaster>();
            }

        }
    }
}
