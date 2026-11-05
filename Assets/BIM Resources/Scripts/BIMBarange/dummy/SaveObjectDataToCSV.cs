using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveObjectDataToCSV : MonoBehaviour
{
    [Header("List of GameObjects to Save")]
    public List<GameObject> objectsToSave = new List<GameObject>();

    [ContextMenu("Save To CSV")]
    void Start()
    {
        SaveToCSV();
    }
    public void SaveToCSV()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "CorrectedChairPositions.csv");

        using (StreamWriter writer = new StreamWriter(filePath))
        {
            // Write header with tab-separated values
            writer.WriteLine("Name; PositionX;PositionY;PositionZ");

            foreach (GameObject obj in objectsToSave)
            {
                if (obj != null)
                {
                    Vector3 pos = obj.transform.position;
                    string line = $"{obj.name};{pos.x.ToString("F4")};{pos.y.ToString("F4")};{pos.z.ToString("F4")}";
                    writer.WriteLine(line);
                }
            }
        }

        Debug.LogError($"TSV file saved to: {filePath}");
    }

}
