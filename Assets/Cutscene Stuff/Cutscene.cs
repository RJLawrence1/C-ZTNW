using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewCutscene", menuName = "Cutscenes/Cutscene")]
public class Cutscene : ScriptableObject
{
    [Header("Cutscene Steps")]
    public List<CutsceneStep> steps = new List<CutsceneStep>();
}
