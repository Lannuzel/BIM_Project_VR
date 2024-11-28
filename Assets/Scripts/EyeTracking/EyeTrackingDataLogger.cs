using System.IO;
using UnityEngine;

public class EyeTrackingDataLogger : MonoBehaviour
{
    [SerializeField] private EyeTrackingRay eyeTrackingRay;
    private StreamWriter csvWriter;
    [SerializeField] private string csvFilePath = "EyeTrackingData.csv";

    private void Start()
    {
        csvFilePath = Path.Combine(Application.persistentDataPath,"EyeTrackingData.csv");

        csvWriter = new StreamWriter(csvFilePath, false);  // false écrase le fichier existant
        csvWriter.WriteLine("Time;RayOriginX;RayOriginY;RayOriginZ;HitPointX;HitPointY;HitPointZ;ObjectHit");
    }

    private void Update()
    {
        if (eyeTrackingRay != null && eyeTrackingRay.TryGetRayHit(out RaycastHit hit))
        {
            csvWriter.WriteLine($"{Time.time};{eyeTrackingRay.transform.position.x};{eyeTrackingRay.transform.position.y};{eyeTrackingRay.transform.position.z};" +
                                $"{hit.point.x};{hit.point.y};{hit.point.z};{hit.transform.name}");
        }
    }

    private void OnDestroy()
    {
        if (csvWriter != null)
        {
            csvWriter.Close();
        }
    }
}
