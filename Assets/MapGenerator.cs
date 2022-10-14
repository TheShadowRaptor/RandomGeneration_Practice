using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{
    // Cave shape integar
    public int caveShapeNum;
    //Size of map generation
    public int width;
    public int height;

    // Randomizes Map
    public string seed;
    public bool useRandomSeed;

    //How much should be filled with wall (%)
    [Range(0, 100)]
    public int randomFillPercent;

    //Map 2D array ---- Defins a grid of integers ---- any tile = 0 (empty) any tile = 1 (Wall)
    int[,] map;

    // Start is called before the first frame update
    void Start()
    {
        GenerateMap();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            GenerateMap();
        }
    }

    void GenerateMap()
    {
        // Generates the map
        map = new int[width, height];

        //Randomly fills the map
        RandomFillMap();

        // Changes cave shape
        for (int i = 0; i < caveShapeNum; i++)
        {
            SmoothMap();
        }

        MeshGenerator meshGenerater = GetComponent<MeshGenerator>();
        meshGenerater.GenerateMesh(map, 1);
    }

    void RandomFillMap()
    {
        // Picks a randomSeed
        if (useRandomSeed)
        {
            seed = Time.time.ToString();
        }

        // Returns a random seed HashCode
        System.Random pseudoRandom = new System.Random(seed.GetHashCode());

        // For each tile
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // If the last tiles on the map
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    // Become a wall
                    map[x, y] = 1;
                }
                else
                {
                    // If less then fillpercent (Wall) If greater than fillpercent (BlankTile)
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercent) ? 1 : 0;
                }                
            }
        }
    }

    void SmoothMap()
    {
        // For each tile
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbourWallTiles = GetSurroundingWallCount(x, y);

                if (neighbourWallTiles > 4) map[x, y] = 1;
                else if (neighbourWallTiles < 4) map[x, y] = 0;
            }
        }
    }

    // Check how much neighbouringTiles with walls does this tile have
    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        // Loop through 3 by 3 grid centered on tile gridX, griY (look at all neighbours), 
        
        for (int neighbourX = gridX - 1; neighbourX <= gridX + 1; neighbourX++)
        {
            for (int neighbourY = gridY - 1; neighbourY <= gridY + 1; neighbourY++)
            {
                // Check if safe inside map
                if (neighbourX >= 0 && neighbourX < width && neighbourY >= 0 && neighbourY < height)
                {
                    // Not looking at original tile
                    if (neighbourX != gridX || neighbourY != gridY)
                    {
                        // If neighbour = Wall (WallCountIncreases)
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                // If outside of map
                else
                {
                    wallCount++;
                }
            }
        }

        return wallCount;
    }

//    private void OnDrawGizmos()
//    {
//        Draws gizmo using random map generation
//        if (map != null)
//        {
//            For each tile
//            for (int x = 0; x < width; x++)
//            {
//                for (int y = 0; y < height; y++)
//                {
//                    Colors(Wall) Black, (Floor)White
//                    Gizmos.color = (map[x, y] == 1) ? Color.black : Color.white;
//                    Gizmo Posiiton
//                    Vector3 pos = new Vector3(-width / 2 + x + 0.5f, 0, -height / 2 + y + 0.5f);

//                    The Gizmo
//                    Gizmos.DrawCube(pos, Vector3.one);
//                }
//            }
//        }
//    }
}






