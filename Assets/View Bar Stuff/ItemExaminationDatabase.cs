using UnityEngine;

[CreateAssetMenu(fileName = "ItemExaminationDatabase", menuName = "C&Z/Item Examination Database")]
public class ItemExaminationDatabase : ScriptableObject
{
    public static ItemExaminationDatabase instance;

    [System.Serializable]
    public class ItemExamination
    {
        public string itemName;
        [TextArea] public string description;
        public AudioClip clip;
    }

    public ItemExamination[] entries;

    void OnEnable()
    {
        instance = this;
    }

    public ItemExamination GetExamination(string itemName)
    {
        if (entries == null) return null;
        foreach (ItemExamination entry in entries)
            if (entry.itemName == itemName)
                return entry;
        return null;
    }
}
