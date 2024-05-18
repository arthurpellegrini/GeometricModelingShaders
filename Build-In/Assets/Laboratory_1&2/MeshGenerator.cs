using UnityEditor;
using UnityEngine;

using WingedEdge;
using HalfEdge;

internal enum Shape
{
    Box,
    Chips,
    RegularPolygon,
    RegularEllipsoid,
    Pacman
}

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshGenerator : MonoBehaviour
{
    [SerializeField] private bool showQuadGizmos = true;
    [SerializeField] private bool showXxxEdgeGizmos = true;
    [SerializeField] private int nbSubdivisions = 0;
    [SerializeField] private Shape selectedShape = Shape.Box;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private WingedEdgeMesh _wingedEdgeMesh;
    private HalfEdgeMesh _halfEdgeMesh;

    private void Start()
    {
        _meshFilter = GetComponent<MeshFilter>();

        _meshFilter.mesh = selectedShape switch
        {
            Shape.Box => CreateBox(new Vector3(1, 1, 1)),
            Shape.Chips => CreateChips(new Vector3(1, 1, 1)),
            Shape.RegularPolygon => CreateRegularPolygon(new Vector3(4, 0, 4), 6),
            Shape.RegularEllipsoid => CreateRegularPolygon(new Vector3(3, 0, 4), 12),
            Shape.Pacman => CreatePacman(new Vector3(4, 0, 4), 6),
            _ => _meshFilter.mesh
        };

        _halfEdgeMesh = new HalfEdgeMesh(_meshFilter.mesh);
        _wingedEdgeMesh = new WingedEdgeMesh(_meshFilter.mesh);
        
        _halfEdgeMesh.SubdivideCatmullClark(nbSubdivisions);
        _meshFilter.mesh = _halfEdgeMesh.ConvertToFaceVertexMesh();
        // _meshFilter.mesh = _wingedEdgeMesh.ConvertToFaceVertexMesh();
        
        Debug.Log(_halfEdgeMesh.ConvertToCsvFormat());
        Debug.Log(_wingedEdgeMesh.ConvertToCsvFormat());
    }

    private Mesh CreateBox(Vector3 halfSize)
    {
        var mesh = new Mesh
        {
            name = "Box",
            vertices = new Vector3[8]
            {
                new (-halfSize.x, -halfSize.y, -halfSize.z),
                new (halfSize.x, -halfSize.y, -halfSize.z),
                new (halfSize.x, -halfSize.y, halfSize.z),
                new (-halfSize.x, -halfSize.y, halfSize.z),
                    
                new (-halfSize.x, halfSize.y, halfSize.z),
                new (halfSize.x, halfSize.y, halfSize.z),
                new (halfSize.x, halfSize.y, -halfSize.z),
                new (-halfSize.x, halfSize.y, -halfSize.z)
            }
        };
        
        var quads = new int[4 * 6]
        {
            0, 1, 2, 3,
            3, 2, 5, 4,
            4, 5, 6, 7,
            5, 2, 1, 6,
            7, 6, 1, 0,
            4, 7, 0, 3
        };
        
        mesh.SetIndices(quads, MeshTopology.Quads, 0);
        return mesh;
    }

    private Mesh CreateChips(Vector3 halfSize)
    {
        var mesh = new Mesh
        {
            name = "chips",
            vertices = new Vector3[8]
            {
                new (-halfSize.x, halfSize.y, -halfSize.z),
                new (halfSize.x, halfSize.y, -halfSize.z),
                new (halfSize.x, -halfSize.y, -halfSize.z),
                new (-halfSize.x, -halfSize.y, -halfSize.z),
            
                new (halfSize.x, halfSize.y, halfSize.z),
                new (-halfSize.x, halfSize.y, halfSize.z),
                new (-halfSize.x, -halfSize.y, halfSize.z),
                new (halfSize.x, -halfSize.y, halfSize.z)
            }
        };
        
        var quads = new int[3 * 4]
        {
            0, 1, 2, 3,
            4, 5, 6, 7,
            4, 1, 0, 5
        };
        
        mesh.SetIndices(quads, MeshTopology.Quads, 0);
        return mesh;
    }

    private Mesh CreateRegularPolygon(Vector3 halfSize, int nSectors)
    {
        var mesh = new Mesh
        {
            name = "Polygon"
        };

        var vertices = new Vector3[nSectors * 2 + 1];
        var quads = new int[4 * nSectors];


        var initialAngle = 360f / nSectors * Mathf.PI / 180; // en radian
        var currentAngle = initialAngle;

        for (var i = 0; i < nSectors * 2 + 1; i += 2)
        {
            vertices[i] = new Vector3(
                Mathf.Cos(currentAngle) * halfSize.x,
                0,
                Mathf.Sin(currentAngle) * halfSize.x);

            currentAngle += initialAngle;
        }

        for (var i = 1; i < nSectors * 2; i += 2)
        {
            var milieuX = (vertices[i - 1].x + vertices[i + 1].x) / 2;
            var milieuZ = (vertices[i - 1].z + vertices[i + 1].z) / 2;
            vertices[i] = new Vector3(milieuX, 0, milieuZ);
        }

        vertices[nSectors * 2] = Vector3.zero;

        var index = 0;
        var lastVertice = vertices.Length - 1;
        var beforeLastVertice = lastVertice - 1;
        for (var i = 0; i < quads.Length / 2; i += 2)
        {
            beforeLastVertice = i == 0 ? beforeLastVertice : i - 1;
            quads[index++] = lastVertice;
            quads[index++] = i + 1;
            quads[index++] = i;
            quads[index++] = beforeLastVertice;
        }

        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        return mesh;
    }

    private Mesh CreatePacman(Vector3 halfSize, int nSectors, float startAngle = Mathf.PI / 3,
        float endAngle = 5 * Mathf.PI / 3)
    {
        var mesh = new Mesh
        {
            name = "Pacman"
        };

        var vertices = new Vector3[nSectors * 2 + 2];
        var quads = new int[4 * nSectors];


        var initialAngle = (endAngle - startAngle) / nSectors; // en radian
        var currentAngle = initialAngle;

        for (var i = 0; i < nSectors * 2 + 1; i += 2)
        {
            vertices[i] = new Vector3(
                Mathf.Cos(currentAngle) * halfSize.x,
                0,
                Mathf.Sin(currentAngle) * halfSize.x);

            currentAngle += initialAngle;
        }

        for (var i = 1; i < nSectors * 2; i += 2)
        {
            var milieuX = (vertices[i - 1].x + vertices[i + 1].x) / 2;
            var milieuZ = (vertices[i - 1].z + vertices[i + 1].z) / 2;
            vertices[i] = new Vector3(milieuX, 0, milieuZ);
        }

        vertices[nSectors * 2 + 1] = Vector3.zero;

        var index = 0;
        var lastVertice = vertices.Length - 1;
        for (var i = 1; i < quads.Length / 2; i += 2)
        {
            var beforeLastVertice = i == 0 ? 0 : i - 1;
            quads[index++] = lastVertice;
            quads[index++] = i + 1;
            quads[index++] = i;
            quads[index++] = beforeLastVertice;
        }

        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        return mesh;
    }

    private void OnDrawGizmos()
    {
        if (!(_meshFilter && _meshFilter.mesh)) return;

        if (showXxxEdgeGizmos)
        {
            _wingedEdgeMesh?.DrawGizmos(true, true, true, transform);
            _halfEdgeMesh?.DrawGizmos(true, true, true, transform);
        }

        if (showQuadGizmos)
        {
            DrawQuadGizmos();
        }
    }

    private void DrawQuadGizmos()
    {
        var mesh = _meshFilter.mesh;
        var vertices = mesh.vertices;
        var quads = mesh.GetIndices(0);
        
        
        var style = new GUIStyle
        {
            fontSize = 15,
            normal =
            {
                textColor = Color.red
            }
        };
        
        for (var i = 0; i < vertices.Length; i++)
        {
            var worldPosition = transform.TransformPoint(vertices[i]);
            Handles.Label(worldPosition, i.ToString(), style);
        }
        
        Gizmos.color = Color.black;
        style.normal.textColor = Color.blue;
        
        for (var i = 0; i < quads.Length / 4; i++)
        {
            var index1 = quads[4 * i];
            var index2 = quads[4 * i + 1];
            var index3 = quads[4 * i + 2];
            var index4 = quads[4 * i + 3];

            var pt1 = transform.TransformPoint(vertices[index1]);
            var pt2 = transform.TransformPoint(vertices[index2]);
            var pt3 = transform.TransformPoint(vertices[index3]);
            var pt4 = transform.TransformPoint(vertices[index4]);
            
            Gizmos.DrawLine(pt1, pt2);
            Gizmos.DrawLine(pt2, pt3);
            Gizmos.DrawLine(pt3, pt4);
            Gizmos.DrawLine(pt4, pt1);
            
            var str = $"{i} ({index1},{index2},{index3},{index4})";
            Handles.Label((pt1 + pt2 + pt3 + pt4) / 4.0f, str, style);
        }
    }
}