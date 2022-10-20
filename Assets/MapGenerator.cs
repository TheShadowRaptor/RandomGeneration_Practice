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

    [Range(0, 100)]
    public int _wallThreshHoldSize = 50;
    
    [Range(0, 100)]
    public int _roomThreshHoldSize = 50;

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

        ProcessMap();

        int borderSize = 10;
        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                {
                    borderedMap[x,y] = map[x - borderSize, y - borderSize];
                }
                else
                {
                    borderedMap[x,y] =  1;
                }
            }
        }

        MeshGenerator meshGenerater = GetComponent<MeshGenerator>();
        meshGenerater.GenerateMesh(borderedMap, 1);
    }

    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);

                    foreach (Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }           
        }
        return regions;
    }

    void ProcessMap()
    {
        List<List<Coord>> wallRegions = GetRegions(1);

        int wallThresholdSize = _wallThreshHoldSize;
        foreach(List<Coord> wallRegion in wallRegions)
        {
            if(wallRegion.Count < wallThresholdSize)
            {
                foreach(Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }               
        }

        List<List<Coord>> roomRegions = GetRegions(0);

        int roomThresholdSize = _roomThreshHoldSize;
        List<Room> survivingRooms = new List<Room>();

        foreach(List<Coord> roomRegion in roomRegions)
        {
            if(roomRegion.Count < roomThresholdSize)
            {
                foreach(Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = 1;
                }
            }  
            else
            {
                survivingRooms.Add(new Room(roomRegion, map));
            }
        }
        ConnectClosestRooms(survivingRooms);
    }

    void ConnectClosestRooms(List<Room> allRooms)
    {
        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach(Room roomA in allRooms)
        {
            possibleConnectionFound = false;

            foreach(Room roomB in allRooms)
            {
                if (roomA== roomB)
                {
                    continue;
                }

                if (roomA.IsConnected(roomB))
                {
                    possibleConnectionFound = false;
                    break;
                }

                for(int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for(int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileX - tileB.tileX, 2));

                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if (possibleConnectionFound)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }
    }

    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);
        Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 100);
    }

    Vector3 CoordToWorldPoint(Coord tile)
    {
        return new Vector3(-width / 2 + 0.5f + tile.tileX, 2, -height / 2 + 0.5f + tile.tileY);
    }

    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        int tileType = map[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while(queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add(tile);

            for(int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for(int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if (mapFlags[x, y] == 0 && map[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }
        return tiles;
    }

    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >=0 && y < height;
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
                if (IsInMapRange(neighbourX, neighbourY))
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

    struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }
        
    class Room
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;

        public Room()
        {

        }

        public Room(List<Coord> roomTiles, int[,] map)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            foreach (Coord tile in tiles)
            {
                for(int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                {
                    for(int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if (x == tile.tileX || y == tile.tileY)
                        {
                            if (map[x, y] == 1)
                            {
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }
        public static void ConnectRooms(Room roomA, Room roomB)
        {
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }
    }
}   






