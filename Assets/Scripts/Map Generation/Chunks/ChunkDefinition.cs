using System.IO;
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

    [Header("Player spawn")]
    public GameObject playerPrefab;
    public bool spawnPlayerAtFirstRoom = false;

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


    // CHUNKRECORD CONVERSION & PERSISTENCE
    public ChunkRecord ToRecord()
    {
        return new ChunkRecord(chunkCoord, GetEffectiveOrigin(), seed, useRandomSeed);
    }

    public static ChunkRecord RecordFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        return JsonUtility.FromJson<ChunkRecord>(json);
    }


    public static string RecordToJson(ChunkRecord record)
    {
        return JsonUtility.ToJson(record);
    }

    public static void SaveRecordToFile(ChunkRecord record, string filename)
    {
        if (record == null || string.IsNullOrEmpty(filename)) return;
        var path = Path.Combine(Application.persistentDataPath, filename);
        File.WriteAllText(path, RecordToJson(record));
    }


    public static ChunkRecord LoadRecordFromFile(string filename)
    {
        if (string.IsNullOrEmpty(filename)) return null;
        var path = Path.Combine(Application.persistentDataPath, filename);
        if (!File.Exists(path)) return null;
        var json = File.ReadAllText(path);
        return RecordFromJson(json);
    }

    // KEY FUNCTIONS FOR APPLYING CHUNK DEFINITIONS TO GENERATORS
    // basically this object serves as input from which the Generator can re/make a chunk

    public void ApplyToGenerator(DungeonGenerator_Seeded_Chunks generator, Vector3Int? overrideOrigin = null)
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

        // Player spawn
        generator.playerPrefab = playerPrefab;
        generator.spawnPlayerAtFirstRoom = spawnPlayerAtFirstRoom;

        // Seed
        generator.useRandomSeed = useRandomSeed;
        generator.seed = seed;
        if (!useRandomSeed)
        {
            generator.SetSeed(seed);
        }
    }

    // for loading from a record (those have less data than the full scriptable obj)
    public static void ApplyRecordAndGenerate(DungeonGenerator_Seeded_Chunks generator, ChunkRecord record)
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