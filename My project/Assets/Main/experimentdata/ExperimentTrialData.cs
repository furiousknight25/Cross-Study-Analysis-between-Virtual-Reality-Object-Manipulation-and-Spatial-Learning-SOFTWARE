using UnityEngine;

[CreateAssetMenu(fileName = "NewTrialData", menuName = "Experiment/Trial Data")]
public class ExperimentTrialData : ScriptableObject
{
    public string TrialID;
    public int MnemonicLoad;
    public int PropositionCount;
    public float PropositionalDensity;
    public string FullPassage;
    public string[] Chunks;
    public string[] Foils;
}