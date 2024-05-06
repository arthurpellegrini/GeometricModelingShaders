using UnityEditor;
using UnityEngine;
using WingedEdge;
using HalfEdge;

internal enum Shapes
{
    Box,
    Chips,
    RegularPolygon,
    RegularEllipsoid,
    Pacman
}

[RequireComponent(typeof(MeshFilter))]
public class VertexFaceMeshGenerator : MonoBehaviour
{
    [SerializeField] private bool mDisplayMeshInfo = true;
    [SerializeField] private bool mDisplayMeshEdges = true;
    [SerializeField] private bool mDisplayMeshVertices = true;
    [SerializeField] private bool mDisplayMeshFaces = true;
    [SerializeField] private Shapes mSelectedShapes = Shapes.Box;

    private MeshFilter _mMf;
    private WingedEdgeMesh _mWingedEdgeMesh;
    private HalfEdgeMesh _mHalfEdgeMesh;

    private void Start()
    {
        _mMf = GetComponent<MeshFilter>();

        _mMf.mesh = mSelectedShapes switch
        {
            Shapes.Box => CreateBox(new Vector3(1, 1, 1)),
            Shapes.Chips => CreateChips(new Vector3(1, 1, 1)),
            Shapes.RegularPolygon => CreateRegularPolygon(new Vector3(4, 0, 4), 6),
            Shapes.RegularEllipsoid => CreateRegularPolygon(new Vector3(3, 0, 4), 18),
            Shapes.Pacman => CreatePacman(new Vector3(4, 0, 4), 6),
            _ => _mMf.mesh
        };

        // _mWingedEdgeMesh = new WingedEdgeMesh(_mMf.mesh);
        // GUIUtility.systemCopyBuffer = _mWingedEdgeMesh.ConvertToCsvFormat(" ");
        // _mMf.mesh = _mWingedEdgeMesh.ConvertToFaceVertexMesh();
        // Debug.Log(_mWingedEdgeMesh.ConvertToCsvFormat(" "));

        _mHalfEdgeMesh = new HalfEdgeMesh(_mMf.mesh);
        GUIUtility.systemCopyBuffer = _mHalfEdgeMesh.ConvertToCsvFormat(" ");
        _mMf.mesh = _mHalfEdgeMesh.ConvertToFaceVertexMesh();
        Debug.Log(_mHalfEdgeMesh.ConvertToCsvFormat(" "));
    }

    private Mesh CreateBox(Vector3 halfSize)
    {
        var mesh = new Mesh
        {
            name = "Box"
        };

        var vertices = new Vector3[8];
        var quads = new int[4 * 6];

        vertices[0] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        vertices[1] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        vertices[2] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        vertices[3] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);

        vertices[4] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        vertices[5] = new Vector3(halfSize.x, halfSize.y, halfSize.z);
        vertices[6] = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        vertices[7] = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);

        quads[0] = 0;
        quads[1] = 1;
        quads[2] = 2;
        quads[3] = 3;

        quads[4] = 3;
        quads[5] = 2;
        quads[6] = 5;
        quads[7] = 4;

        quads[8] = 4;
        quads[9] = 5;
        quads[10] = 6;
        quads[11] = 7;

        quads[12] = 5;
        quads[13] = 2;
        quads[14] = 1;
        quads[15] = 6;

        quads[16] = 7;
        quads[17] = 6;
        quads[18] = 1;
        quads[19] = 0;

        quads[20] = 4;
        quads[21] = 7;
        quads[22] = 0;
        quads[23] = 3;

        mesh.vertices = vertices;
        mesh.SetIndices(quads, MeshTopology.Quads, 0);

        return mesh;
    }

    private Mesh CreateChips(Vector3 halfSize)
    {
        var mesh = new Mesh
        {
            name = "chips"
        };

        var vertices = new Vector3[8];
        var quads = new int[3 * 4];

        // Face 0 (0, 1, 2, 3)
        vertices[0] = new Vector3(-halfSize.x, halfSize.y, -halfSize.z);
        vertices[1] = new Vector3(halfSize.x, halfSize.y, -halfSize.z);
        vertices[2] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        vertices[3] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);

        // Face 1 (4, 5, 6, 7)
        vertices[4] = new Vector3(halfSize.x, halfSize.y, halfSize.z);
        vertices[5] = new Vector3(-halfSize.x, halfSize.y, halfSize.z);
        vertices[6] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);
        vertices[7] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);

        quads[0] = 0;
        quads[1] = 1;
        quads[2] = 2;
        quads[3] = 3;

        quads[4] = 4;
        quads[5] = 5;
        quads[6] = 6;
        quads[7] = 7;

        quads[8] = 4;
        quads[9] = 1;
        quads[10] = 0;
        quads[11] = 5;

        mesh.vertices = vertices;
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
        if (!(_mMf && _mMf.mesh && mDisplayMeshInfo)) return;

        _mWingedEdgeMesh?.DrawGizmos(mDisplayMeshVertices, mDisplayMeshEdges, mDisplayMeshFaces, transform);

        _mHalfEdgeMesh?.DrawGizmos(mDisplayMeshVertices, mDisplayMeshEdges, mDisplayMeshFaces, transform);

        var mesh = _mMf.mesh;
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


        if (mDisplayMeshVertices)
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


            if (mDisplayMeshEdges)
            {
                Gizmos.DrawLine(pt1, pt2);
                Gizmos.DrawLine(pt2, pt3);
                Gizmos.DrawLine(pt3, pt4);
                Gizmos.DrawLine(pt4, pt1);
            }

            if (!mDisplayMeshFaces) continue;
            var str = $"{i} ({index1},{index2},{index3},{index4})";
            Handles.Label((pt1 + pt2 + pt3 + pt4) / 4.0f, str, style);
        }
    }
}