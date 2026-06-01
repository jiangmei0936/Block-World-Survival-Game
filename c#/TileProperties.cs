// TileProperties.cs
using UnityEngine;

[CreateAssetMenu(fileName = "NewTileData", menuName = "Tile Data")]
public class TileProperties : ScriptableObject
{
    public string tileName;
    public TileType type;
    public int health = 1;
    public Item dropItem;
    public int dropAmount = 1;
    public float dropChance = 1f;
    public ParticleSystem destroyEffect;
}