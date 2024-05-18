using UnityEngine;

public class MeshToHeightMap : MonoBehaviour
{
    public Mesh mesh;
    public int resolution = 512; // Adjust as needed
    public Texture2D heightmapTexture;

    void Start()
    {
        // Create a new texture for the heightmap
        heightmapTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

        // Get the mesh data
        Vector3[] vertices = mesh.vertices;

        // Iterate through each vertex
        for (int i = 0; i < vertices.Length; i++)
        {
            // Calculate UV coordinates for heightmap texture
            Vector2 uv = new Vector2(vertices[i].x, vertices[i].z);

            // Map vertex position to texture coordinates
            int x = Mathf.FloorToInt(uv.x * resolution);
            int y = Mathf.FloorToInt(uv.y * resolution);

            // Set height value (Y-component of vertex position) to pixel
            float height = vertices[i].y;
            Color color = new Color(height, height, height, 1f);
            heightmapTexture.SetPixel(x, y, color);
        }

        // Apply changes and upload to GPU
        heightmapTexture.Apply();

        // Optionally, save the texture to a file
        //byte[] bytes = heightmapTexture.EncodeToPNG();
        //System.IO.File.WriteAllBytes("heightmap.png", bytes);
    }
}