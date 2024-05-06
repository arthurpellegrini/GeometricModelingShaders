using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace WingedEdge
{
    public class WingedEdge
    {
        public int index;
        public Vertex startVertex;

        public Vertex endVertex;
        public Face leftFace;
        public Face rightFace;
        public WingedEdge startCWEdge;
        public WingedEdge startCCWEdge;
        public WingedEdge endCWEdge;
        public WingedEdge endCCWEdge;

        public WingedEdge(int index, Vertex startVertex, Vertex endVertex, Face leftFace, Face rightFace)
        {
            this.index = index;
            this.startVertex = startVertex;
            this.endVertex = endVertex;
            this.leftFace = leftFace;
            this.rightFace = rightFace;
        }
    }

    public class Vertex
    {
        public int index;
        public Vector3 position;
        public WingedEdge edge;

        public Vertex(int index, Vector3 position)
        {
            this.index = index;
            this.position = position;
        }
    }

    public class Face
    {
        public int index;
        public WingedEdge edge;

        public Face(int index)
        {
            this.index = index;
        }
    }

    public class WingedEdgeMesh
    {
        private List<Vertex> vertices;
        private List<WingedEdge> edges;
        private List<Face> faces;

        private int nVerticesForTopology;

        public WingedEdgeMesh(Mesh mesh)
        {
            vertices = new List<Vertex>();
            edges = new List<WingedEdge>();
            faces = new List<Face>();
            var meshVertices = mesh.vertices;

            for (var i = 0; i < mesh.vertexCount; i++) vertices.Add(new Vertex(i, meshVertices[i]));

            var shapes = mesh.GetIndices(0);

            var meshTopology = mesh.GetTopology(0);

            nVerticesForTopology = meshTopology.Equals(MeshTopology.Triangles) ? 3 : 4;

            var mapWingedEdges = new Dictionary<long, WingedEdge>();

            var nbFaces = shapes.Length / nVerticesForTopology;
            var indexVertex = 0;
            var indexWingedEdge = 0;

            for (var i = 0; i < nbFaces; i++)
            {
                var currentFace = new Face(i);
                faces.Add(currentFace);
                var faceVertex = new Vertex[nVerticesForTopology];


                for (var j = 0; j < nVerticesForTopology; j++)
                {
                    var v = vertices[shapes[indexVertex++]];
                    faceVertex[j] = v;
                }

                var wingedEdges = new WingedEdge[nVerticesForTopology];

                for (var j = 0; j < nVerticesForTopology; j++)
                {
                    var start = faceVertex[j];
                    var end = j < nVerticesForTopology - 1 ? faceVertex[j + 1] : faceVertex[0];

                    long min = Mathf.Min(start.index, end.index);
                    long max = Mathf.Max(start.index, end.index);

                    var key = min + (max << 32);

                    WingedEdge we;
                    if (!mapWingedEdges.TryGetValue(key, out we))
                    {
                        we = new WingedEdge(indexWingedEdge++, start, end, null, currentFace);
                        mapWingedEdges.TryAdd(key, we);
                        edges.Add(we);
                        currentFace.edge = we;

                        start.edge = we;
                        // end.edge = we;
                    }

                    wingedEdges[j] = we;
                }

                for (var j = 0; j < nVerticesForTopology; j++)
                {
                    var currentWingedEdge = wingedEdges[j];
                    if (!currentWingedEdge.rightFace.Equals(currentFace))
                    {
                        currentWingedEdge.leftFace = currentFace;

                        currentWingedEdge.startCCWEdge =
                            j < nVerticesForTopology - 1 ? wingedEdges[j + 1] : wingedEdges[0];
                        currentWingedEdge.endCWEdge =
                            j == 0 ? wingedEdges[nVerticesForTopology - 1] : wingedEdges[j - 1];
                        currentWingedEdge.endVertex.edge =
                            j == 0 ? wingedEdges[nVerticesForTopology - 1] : wingedEdges[j - 1];
                    }
                    else
                    {
                        currentWingedEdge.rightFace = currentFace;

                        currentWingedEdge.startCWEdge =
                            j == 0 ? wingedEdges[nVerticesForTopology - 1] : wingedEdges[j - 1];
                        currentWingedEdge.endCCWEdge =
                            j < nVerticesForTopology - 1 ? wingedEdges[j + 1] : wingedEdges[0];
                        currentWingedEdge.endVertex.edge =
                            j < nVerticesForTopology - 1 ? wingedEdges[j + 1] : wingedEdges[0];
                    }
                }
            }
        }

        public Mesh ConvertToFaceVertexMesh()
        {
            Mesh newMesh = new Mesh();
            Vector3[] vertices = new Vector3[this.vertices.Count];
            int[] quads = new int[faces.Count * nVerticesForTopology];
            
            for (int i = 0; i < this.vertices.Count; i++)
            {
                vertices[i] = this.vertices[i].position;
            }
            
            for (int i = 0; i < faces.Count; i++)
            {
                Face face = faces[i];
                WingedEdge currentEdge = face.edge;
                WingedEdge firstEdge = currentEdge;
                WingedEdge previousEdge = firstEdge;
                int offset = 0;
                int index = i * nVerticesForTopology;
                do
                {
                    int indiceVertex = (currentEdge.endCWEdge != null && currentEdge.endCWEdge.index == previousEdge.index)
                        ? currentEdge.endVertex.index
                        : currentEdge.startVertex.index;
                    quads[index + offset++] = indiceVertex;
                    WingedEdge tmp = currentEdge;
                    
                    currentEdge = (currentEdge.endCWEdge != null && (currentEdge.endCWEdge.index == firstEdge.index || currentEdge.endCWEdge.index == previousEdge.index))
                        ? currentEdge.startCCWEdge
                        : currentEdge.endCCWEdge;
                    previousEdge = tmp;
                } while (currentEdge != firstEdge);
            }

            newMesh.vertices = vertices;
            newMesh.SetIndices(quads, MeshTopology.Quads, 0);
            newMesh.RecalculateBounds();
            newMesh.RecalculateNormals();
            return newMesh;
        }


        public string ConvertToCsvFormat(string separator = "\t")
        {
            var tabSize = Mathf.Max(edges.Count, faces.Count, vertices.Count);

            var strings = new List<string>(new string[tabSize]);

            for (var i = 0; i < edges.Count; i++)
            {
                var we = edges[i];
                strings[i] += we.index + separator;
                strings[i] += we.startVertex.index + ", " + we.endVertex.index + separator;

                if (we.startCWEdge != null)
                    strings[i] += we.startCWEdge.index + ", ";
                else
                    strings[i] += "null, ";

                if (we.endCWEdge != null)
                    strings[i] += we.endCWEdge.index;
                else
                    strings[i] += "null";

                strings[i] += separator;

                if (we.startCCWEdge != null)
                    strings[i] += we.startCCWEdge.index;
                else
                    strings[i] += "null";

                strings[i] += ", ";

                if (we.endCCWEdge != null)
                    strings[i] += we.endCCWEdge.index;
                else
                    strings[i] += "null";

                strings[i] += separator + separator;
            }

            for (var i = edges.Count; i < tabSize; i++)
                strings[i] += separator + separator + separator + separator + separator;

            for (var i = 0; i < faces.Count; i++)
            {
                var f = faces[i];
                strings[i] += f.index + separator;

                if (f.edge != null)
                    strings[i] += f.edge.index;
                else
                    strings[i] += "null";

                strings[i] += separator + separator;
            }

            for (var i = faces.Count; i < tabSize; i++) strings[i] += separator + separator + separator;

            for (var i = 0; i < vertices.Count; i++)
            {
                var v = vertices[i];
                strings[i] += v.index + separator;
                strings[i] += v.position.x.ToString("N03") + " ";
                strings[i] += v.position.y.ToString("N03") + " ";
                strings[i] += v.position.z.ToString("N03") + " " + separator;

                if (v.edge != null)
                    strings[i] += v.edge.index;
                else
                    strings[i] += "null";
            }

            var header = "WingedEdges" + separator + separator + separator + separator + separator + "Faces" +
                         separator + separator + separator + "Vertices" + "\n"
                         + "Index" + separator + "start+endVertex index" + separator + "CW index" + separator +
                         "CCW index" + separator + separator
                         + "Index" + separator + "WingedEdge index" + separator + separator
                         + "Index" + separator + "Position" + separator + "WingedEdge index" + "\n";

            return header + string.Join("\n", strings);
        }


        public void DrawGizmos(bool drawVertices, bool drawEdges, bool drawFaces, Transform transform)
        {
            var style = new GUIStyle
            {
                fontSize = 15,
                normal =
                {
                    textColor = Color.red
                }
            };


            if (drawVertices)
                for (var i = 0; i < vertices.Count; i++)
                {
                    var worldPosition = transform.TransformPoint(vertices[i].position);
                    Handles.Label(worldPosition, i.ToString(), style);
                }

            style.normal.textColor = Color.black;

            if (drawEdges)
                for (var i = 0; i < edges.Count; i++)
                {
                    var worldPositionStart = transform.TransformPoint(edges[i].startVertex.position);
                    var worldPositionEnd = transform.TransformPoint(edges[i].endVertex.position);
                    Gizmos.DrawLine(worldPositionStart, worldPositionEnd);
                    Handles.Label((worldPositionEnd + worldPositionStart) / 2, "E : " + i, style);
                }

            style.normal.textColor = Color.blue;

            if (drawFaces)
                for (var i = 0; i < faces.Count; i++)
                {
                    var face = faces[i];
                    var currentEdge = face.edge;
                    var firstEdge = currentEdge;
                    var previousEdge = firstEdge;
                    var index = i * nVerticesForTopology;
                    var textToDisplay = "F" + i + " : (";
                    var sumPosition = Vector3.zero;
                    do
                    {
                        var indiceVertex = currentEdge.endCWEdge != null &&
                                           currentEdge.endCWEdge.index == previousEdge.index
                            ? currentEdge.endVertex.index
                            : currentEdge.startVertex.index;

                        textToDisplay += indiceVertex + " ";
                        sumPosition += transform.TransformPoint(vertices[indiceVertex].position);
                        var tmp = currentEdge;

                        currentEdge = currentEdge.endCWEdge != null &&
                                      (currentEdge.endCWEdge.index == firstEdge.index ||
                                       currentEdge.endCWEdge.index == previousEdge.index)
                            ? currentEdge.startCCWEdge
                            : currentEdge.endCCWEdge;
                        previousEdge = tmp;
                    } while (currentEdge != firstEdge);

                    textToDisplay += ")";
                    Handles.Label(sumPosition / nVerticesForTopology, textToDisplay);
                }
        }
    }
}