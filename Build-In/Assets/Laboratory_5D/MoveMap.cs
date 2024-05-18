using UnityEngine;


public class MoveMap : MonoBehaviour
{
    public Material Material;
    public Vector2 Speed;
    public Vector2 Offset;
    private int CropOffsetID;
    
    void Start ()
    {
        CropOffsetID = Shader.PropertyToID("_CropOffset");
    }

    void Update ()
    {
        Material.SetVector(CropOffsetID, Speed * Time.time + Offset);
    }
}