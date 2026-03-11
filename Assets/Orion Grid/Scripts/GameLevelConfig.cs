using UnityEngine;

[CreateAssetMenu(menuName = "MemoryGame/Level Config")]
public class GameLevelConfig : ScriptableObject
{
    public int levelIndex;

    [Header("Board")]
    public int columns = 3;
    public int rows    = 4;

    [Header("Rules")]
    public float timeLimit         = 60f;
    public int   baseScore         = 100;
    public float comboBonus        = 0.25f;
    public int   timeBonusPerSec   = 10;

    [Header("Icons")]
    public Sprite[] cardIcons;

    public int TotalCards => columns * rows;
    public int TotalPairs => TotalCards / 2;
}