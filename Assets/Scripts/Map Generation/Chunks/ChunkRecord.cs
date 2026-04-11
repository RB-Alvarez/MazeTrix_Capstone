// Note: saves to a local file, will work on saving to either the database or maybe PlayerPrefs

using System;
using UnityEngine;

[Serializable]
public class ChunkRecord
{
    public Vector2Int chunkCoord;
    public Vector3Int originCell;
    public int seed;
    public bool useRandomSeed;

    public ChunkRecord() { }

    public ChunkRecord(Vector2Int coord, Vector3Int origin, int seed, bool useRandom)
    {
        this.chunkCoord = coord;
        this.originCell = origin;
        this.seed = seed;
        this.useRandomSeed = useRandom;
    }
}