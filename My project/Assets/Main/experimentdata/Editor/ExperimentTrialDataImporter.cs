using UnityEngine;
using UnityEditor;
using System.IO;

public class ExperimentTrialDataImporter : EditorWindow
{
    [System.Serializable]
    private class JsonWrapper
    {
        public TrialDataNode[] Items;
    }

    [System.Serializable]
    private class TrialDataNode
    {
        public string TrialID;
        public int MnemonicLoad;
        public int PropositionCount;
        public float PropositionalDensity;
        public string FullPassage;
        public string[] Chunks;
        // Targets removed!
        public string[] Foils;
    }

    [MenuItem("Tools/Import Experiment Stimuli JSON")]
    public static void ImportJSON()
    {
        string filePath = EditorUtility.OpenFilePanel("Select Experiment JSON", "", "json");
        
        if (string.IsNullOrEmpty(filePath)) return;

        string jsonContent = File.ReadAllText(filePath);
        string wrappedJson = "{\"Items\":" + jsonContent + "}";
        JsonWrapper data = JsonUtility.FromJson<JsonWrapper>(wrappedJson);

        if (data == null || data.Items == null || data.Items.Length == 0)
        {
            Debug.LogError("Failed to parse JSON. Ensure the file matches the expected structure.");
            return;
        }

        string targetFolder = "Assets/ExperimentStimuli";
        if (!AssetDatabase.IsValidFolder(targetFolder))
        {
            AssetDatabase.CreateFolder("Assets", "ExperimentStimuli");
        }

        int createdCount = 0;
        foreach (TrialDataNode node in data.Items)
        {
            ExperimentTrialData newAsset = ScriptableObject.CreateInstance<ExperimentTrialData>();
            
            newAsset.TrialID = node.TrialID;
            newAsset.MnemonicLoad = node.MnemonicLoad;
            newAsset.PropositionCount = node.PropositionCount;
            newAsset.PropositionalDensity = node.PropositionalDensity;
            newAsset.FullPassage = node.FullPassage;
            newAsset.Chunks = node.Chunks;
            newAsset.Foils = node.Foils;

            string baseAssetPath = $"{targetFolder}/{node.TrialID}.asset";
            string uniqueAssetPath = AssetDatabase.GenerateUniqueAssetPath(baseAssetPath);

            AssetDatabase.CreateAsset(newAsset, uniqueAssetPath);
            createdCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>Success:</color> Imported {createdCount} Trial Data assets to {targetFolder}.");
    }
}