using System;
using UnityEngine;

[Serializable]
public class ChunkRecord
{
    public Vector2Int chunkCoord;
    public Vector3Int originCell;
    public int seed; // chunk specific seed calcd from world seed + chunk coord
    public bool useRandomSeed; // always false now because of new world seed system, keeping just in case

    public ChunkRecord() { }

    public ChunkRecord(Vector2Int coord, Vector3Int origin, int seed, bool useRandom)
    {
        this.chunkCoord = coord;
        this.originCell = origin;
        this.seed = seed;
        this.useRandomSeed = useRandom;
    }
}