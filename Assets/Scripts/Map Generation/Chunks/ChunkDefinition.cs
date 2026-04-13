using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "ChunkDefinition", menuName = "MazeTrix/Chunk Definition", order = 0)]
public class ChunkDefinition : ScriptableObject
{
    [Header("Chunk identity")]
    public Vector2Int chunkCoord = Vector2Int.zero; // logical chunk coordinates (x,y)
    public Vector3Int originCell = Vector3Int.zero; // explicit origin cell (can be used instead of coord)

    [Header("Chunk size (cells)")]
    public int width = 33;   // MATCH TO GENERATOR CONSTRAINTS
    public int height = 33;  // MATCH TO GENERATOR CONSTRAINTS

    [Header("Generator / tile settings")]
    public GameObject generatorPrefab; 
    public Tilemap wallTilemap;
    public Tilemap floorTilemap;
    public TileBase wallTile;
    public TileBase floorTile;
    public TileBase closedDoorTile;
    public TileBase openDoorTile;

    [Header("Player Repositioning")]
    [Tooltip("Should the player be repositioned to the first room when this chunk generates?")]
    public bool repositionPlayerAtFirstRoom = false;

    [Header("Seed")]
    public int seed = 0;
    public bool useRandomSeed = false;

    [Header("Runtime helpers (editor-only helpful defaults)")]
    [Tooltip("If non-zero, used to compute world origin as coord * chunkSize when applying by coord")]
    public int chunkSize = 0;

    
    public Vector3Int GetEffectiveOrigin() // effective origin = originCell
    {
        if (originCell != Vector3Int.zero) return originCell;
        if (chunkSize != 0) return new Vector3Int(chunkCoord.x * chunkSize, chunkCoord.y * chunkSize, 0);
        return Vector3Int.zero;
    }


    // Convert a ChunkDefinition -> ChunkRecord, keeps only the data that makes one chunk unique from another
    public ChunkRecord ToRecord()
    {
        return new ChunkRecord(chunkCoord, GetEffectiveOrigin(), seed, useRandomSeed);
    }

    // KEY FUNCTIONS FOR APPLYING CHUNK DEFINITIONS TO GENERATORS
    // basically this object serves as input from which the Generator can re/make a chunk

    public void ApplyToGenerator(DungeonGenerator_v4 generator, Vector3Int? overrideOrigin = null)
    {
        if (generator == null) return;

        // Basic grid / origin
        generator.width = Mathf.Max(1, width);
        generator.height = Mathf.Max(1, height);
        generator.originCell = overrideOrigin ?? GetEffectiveOrigin();

        // Tilemaps & tiles
        generator.wallTilemap = wallTilemap;
        generator.floorTilemap = floorTilemap;
        generator.wallTile = wallTile;
        generator.floorTile = floorTile;
        generator.closedDoorTile = closedDoorTile;
        generator.openDoorTile = openDoorTile;

        // Player repositioning: fully delegated to SpawnPlayerInMaze component.
        // Attach or configure a SpawnPlayerInMaze on the same GameObject as the generator.
        var spawner = generator.GetComponent<SpawnPlayerInMaze>();
        if (spawner == null)
        {
            spawner = generator.gameObject.AddComponent<SpawnPlayerInMaze>();
        }
        spawner.dungeonGenerator = generator;
        spawner.repositionPlayerAtFirstRoom = repositionPlayerAtFirstRoom;

        // Seed
        generator.useRandomSeed = useRandomSeed;
        generator.seed = seed;
        if (!useRandomSeed)
        {
            generator.SetSeed(seed);
        }
    }

    // for loading from a record (those have less data than the full scriptable obj)
    public static void ApplyRecordAndGenerate(DungeonGenerator_v4 generator, ChunkRecord record)
    {
        if (generator == null || record == null) return;

        generator.originCell = record.originCell;
        generator.useRandomSeed = record.useRandomSeed;
        generator.seed = record.seed;
        if (!record.useRandomSeed)
        {
            generator.SetSeed(record.seed);
        }

        // Use the generator's GenerateAsChunk API which accepts an origin.
        generator.GenerateAsChunk(record.originCell);
    }
}