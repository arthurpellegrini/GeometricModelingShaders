using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HalfEdge
{
    public class HalfEdge
    {
        public int index;
        public Vertex sourceVertex;
        public Face face;
        public HalfEdge prevEdge;
        public HalfEdge nextEdge;
        public HalfEdge twinEdge;

        public HalfEdge(int index, Vertex sourceVertex, Face face, HalfEdge prevEdge = null, HalfEdge nextEdge = null,
            HalfEdge twinEdge = null)
        {
            this.index = index;
            this.sourceVertex = sourceVertex;
            this.face = face;
            this.prevEdge = prevEdge;
            this.nextEdge = nextEdge;
            this.twinEdge = twinEdge;
        }
    }

    public class Vertex
    {
        public int index;
        public Vector3 position;
        public HalfEdge outgoingEdge;

        public Vertex(int index, Vector3 position) : this(index, position, null)
        {
        }

        private Vertex(int index, Vector3 position, HalfEdge outgoingEdge)
        {
            this.index = index;
            this.position = position;
            this.outgoingEdge = outgoingEdge;
        }

        public List<HalfEdge> GetAdjacentEdges(List<HalfEdge> edges)
        {
            var adjacentEdges = new List<HalfEdge>();

            foreach (var edge in edges.Where(edge => edge.sourceVertex == this || edge.nextEdge.sourceVertex == this)
                         .Where(edge => !adjacentEdges.Contains(edge)))
            {
                adjacentEdges.Add(edge);
                adjacentEdges.Add(edge.twinEdge);
            }

            return adjacentEdges;
        }

        public List<HalfEdge> GetIncidentEdges(List<HalfEdge> edges)
        {
            var incidentEdges = new List<HalfEdge>();

            foreach (var edge in edges)
                if (edge.nextEdge.sourceVertex == this && outgoingEdge != edge)
                    if (!incidentEdges.Contains(edge))
                        incidentEdges.Add(edge);
            return incidentEdges;
        }

        public List<Face> GetAdjacentFaces(List<HalfEdge> edges)
        {
            var adjacentFaces = new Dictionary<int, Face>();
            var adjacentEdges = GetAdjacentEdges(edges);

            for (var i = 0; i < adjacentEdges.Count; i += 2)
            {
                var edge = adjacentEdges[i];
                var twinEdge = adjacentEdges[i + 1];
                adjacentFaces.TryAdd(edge.face.index, edge.face);

                if (twinEdge != null)
                    adjacentFaces.TryAdd(twinEdge.face.index, twinEdge.face);
            }

            return adjacentFaces.Values.ToList();
        }
    }

    public class Face
    {
        public int index;
        public HalfEdge edge;

        public Face(int index)
        {
            this.index = index;
        }

        public Face(int index, HalfEdge edge)
        {
            this.index = index;
            this.edge = edge;
        }

        public List<Vertex> GetVertices()
        {
            var faceVertices = new List<Vertex>();

            var he = edge;
            do
            {
                faceVertices.Add(he.sourceVertex);
                he = he.nextEdge;
            } while (edge != he);

            return faceVertices;
        }
    }

    public class HalfEdgeMesh
    {
        private List<Vertex> vertices;
        private List<HalfEdge> edges;
        private List<Face> faces;

        private int _verticesForTopology;

        public HalfEdgeMesh(Mesh mesh)
        {
            vertices = new List<Vertex>();
            edges = new List<HalfEdge>();
            faces = new List<Face>();
            var meshVertices = mesh.vertices;

            var mapOfIndex = new Dictionary<string, int>();

            for (var i = 0; i < mesh.vertexCount; i++) vertices.Add(new Vertex(i, meshVertices[i]));

            var shapes = mesh.GetIndices(0);
            var meshTopology = mesh.GetTopology(0);
            _verticesForTopology = meshTopology.Equals(MeshTopology.Triangles) ? 3 : 4;

            var indexVertex = 0;
            var indexHalfEdge = 0;
            var nbFaces = shapes.Length / _verticesForTopology;

            var cmp = 0;
            for (var i = 0; i < nbFaces; i++)
            {
                var f = new Face(i);
                var tempHalfEdges = new List<HalfEdge>();

                for (var j = 0; j < _verticesForTopology; j++)
                {
                    var v = vertices[shapes[indexVertex++]];
                    var halfEdge = new HalfEdge(indexHalfEdge++, v, f);
                    tempHalfEdges.Add(halfEdge);
                }

                var nbTempHalfEdge = tempHalfEdges.Count;
                for (var j = 0; j < nbTempHalfEdge; j++)
                {
                    var currentHalfEdge = tempHalfEdges[j];
                    var nextEdgeIndice = j == nbTempHalfEdge - 1 ? 0 : j + 1;
                    var previousEdgeIndice = j == 0 ? nbTempHalfEdge - 1 : j - 1;

                    currentHalfEdge.prevEdge = tempHalfEdges[previousEdgeIndice];
                    currentHalfEdge.nextEdge = tempHalfEdges[nextEdgeIndice];

                    currentHalfEdge.sourceVertex.outgoingEdge = currentHalfEdge;
                    edges.Add(currentHalfEdge);
                }

                for (var j = 0; j < _verticesForTopology; j++)
                {
                    var startIndex = edges[cmp].sourceVertex.index;
                    var endIndex = edges[cmp].nextEdge.sourceVertex.index;

                    var newKey = startIndex + "|" + endIndex;
                    mapOfIndex.Add(newKey, edges[cmp].index);

                    cmp++;
                }

                f.edge = tempHalfEdges[0];
                faces.Add(f);
            }

            foreach (var kp in mapOfIndex)
            {
                var key = kp.Key;
                var value = kp.Value;

                var startIndex = int.Parse(key.Split("|")[0]);
                var endIndex = int.Parse(key.Split("|")[1]);
                var reversedKey = "" + endIndex + "|" + startIndex;

                if (mapOfIndex.TryGetValue(reversedKey, out var reversedValue))
                {
                    edges[reversedValue].twinEdge = edges[value];
                    edges[value].twinEdge = edges[reversedValue];
                }
            }
        }

        public Mesh ConvertToFaceVertexMesh()
        {
            var newMesh = new Mesh();

            var vertices = new Vector3[this.vertices.Count];
            var quads = new int[faces.Count * _verticesForTopology];

            foreach (var v in this.vertices) vertices[v.index] = v.position;

            for (var i = 0; i < faces.Count; i++)
            {
                var f = faces[i];
                var halfEdge = f.edge.prevEdge;
                var offset = 0;
                var j = i * _verticesForTopology;
                do
                {
                    quads[j + offset] = halfEdge.sourceVertex.index;
                    offset++;
                    halfEdge = halfEdge.nextEdge;
                } while (f.edge.prevEdge != halfEdge);
            }

            newMesh.vertices = vertices;
            newMesh.SetIndices(quads, MeshTopology.Quads, 0);
            newMesh.RecalculateBounds();
            newMesh.RecalculateNormals();
            return newMesh;
        }
        
        public void SubdivideCatmullClark(int subdivisionCount)
        {
            for (var i = 0; i < subdivisionCount; i++)
            {
                this.CatmullClarkCreateNewPoints(out var facePoints, out var edgePoints, out var vertexPoints);
            
                foreach (var v in this.vertices)
                    v.position = vertexPoints[v.index];
            
                var edgesCopy = new List<HalfEdge>(this.edges);
                foreach (var edge in edgesCopy)
                    this.SplitEdge(edge, edgePoints[edge.index]);
            
                var faceCopy = new List<Face>(this.faces);
                foreach (var face in faceCopy)
                    this.SplitFace(face, facePoints[face.index]);
            }
        }

        void CatmullClarkCreateNewPoints(out List<Vector3> facePoints, out List<Vector3> edgePoints, out List<Vector3> vertexPoints)
        {
            edgePoints = new List<Vector3>();
            vertexPoints = new List<Vector3>();
            var midPoints = new List<Vector3>();

            facePoints = (from face in this.faces select face.GetVertices() into facesVertices let mean = facesVertices.Aggregate(Vector3.zero, (current, t) => current + t.position) select mean / facesVertices.Count).ToList();

            foreach (var edge in this.edges)
            {
                var startVertex = edge.sourceVertex.position;
                var endVertex = edge.nextEdge.sourceVertex.position;
                
                var midPoint = (startVertex + endVertex) / 2;

                midPoints.Add(midPoint);

                var mean = midPoint;
                
                if (edge.twinEdge != null)
                {
                    var indexC0 = edge.face.index;
                    var indexC1 = edge.twinEdge.face.index;

                    var c0 = facePoints[indexC0];
                    var c1 = facePoints[indexC1];
                    
                    mean = (startVertex + endVertex + c0 + c1) / 4;
                }

                edgePoints.Add(mean);
            }
            
            foreach (var currentVertex in this.vertices)
            {
                var adjacentFaces = currentVertex.GetAdjacentFaces(this.edges);
                var adjacentEdges = currentVertex.GetAdjacentEdges(this.edges);
                var incidentEdges = currentVertex.GetIncidentEdges(this.edges);

                var meanFacePoint = Vector3.zero;
                var meanMidPoint = Vector3.zero;
                
                foreach (var f in adjacentFaces)
                {
                    meanFacePoint += facePoints[f.index];
                }

                meanMidPoint = incidentEdges.Aggregate(meanMidPoint, (current, edge) => current + midPoints[edge.index]);

                Vector3 v;
                if (!adjacentEdges.Contains(null))
                {
                    var n = incidentEdges.Count;
                    var q = meanFacePoint / adjacentFaces.Count;
                    var r = meanMidPoint / n;

                    v = 1.0f * q / n + 2.0f * r / n + (n - 3.0f) / n * currentVertex.position;
                } 
                else
                {
                    var midPointSum = Vector3.zero;

                    for (var j = 1; j < adjacentEdges.Count; j += 2)
                    {
                        var twin = adjacentEdges[j];
                        var edge = adjacentEdges[j - 1];

                        if (twin == null)
                        {
                            midPointSum += midPoints[edge.index];
                        }
                    }
                    v = (midPointSum + currentVertex.position) / 3;
                }

                vertexPoints.Add(v);
            }
        }

        private void SplitEdge(HalfEdge edge, Vector3 splittingPoint)
        {            
            var edgePointExists = edge.twinEdge != null 
                                  && (edge.twinEdge.sourceVertex != edge.nextEdge.sourceVertex || edge.twinEdge.nextEdge.sourceVertex != edge.sourceVertex);
            
            var edgePointVertex = edgePointExists ? edge.twinEdge.nextEdge.sourceVertex : new Vertex(this.vertices.Count, splittingPoint);
            var edgePoint = new HalfEdge(this.edges.Count, edgePointVertex, edge.face, edge, edge.nextEdge);
            
            edge.nextEdge.prevEdge = edgePoint;
            edge.nextEdge = edgePoint;

            if (!edgePointExists)
            {
                edgePointVertex.outgoingEdge = edgePoint;
                this.vertices.Add(edgePointVertex);
            }
            else
            {
                HalfEdge nextTwinEdge = edge.twinEdge.nextEdge;

                nextTwinEdge.twinEdge = edge;
                edgePoint.twinEdge = edge.twinEdge;

                edge.twinEdge.twinEdge = edgePoint;
                edge.twinEdge = nextTwinEdge;
            }

            this.edges.Add(edgePoint);
        }

        private void SplitFace(Face face, Vector3 splittingPoint)
        {
            var facePointVertex = new Vertex(this.vertices.Count, splittingPoint);
            
            var edge = face.edge.nextEdge;
            HalfEdge lastNextToCenter = null;
            HalfEdge lastNextTwinToCenter = null;

            var indexHalfEdge = this.edges.Count;
            var indexFace = this.faces.Count - 1;
            var oldFaceCount = indexFace;
            
            do
            {
                var currentFace = indexFace == oldFaceCount ? face : new Face(indexFace, lastNextTwinToCenter);
                var prevEdge = edge.prevEdge;
                var nextEdge = edge.nextEdge;
                
                if (indexFace != oldFaceCount)
                {
                    this.faces.Add(currentFace);
                    if (lastNextTwinToCenter != null) lastNextTwinToCenter.face = currentFace;
                }
                
                var nextEdgeToCenter = new HalfEdge(indexHalfEdge++, edge.sourceVertex, currentFace, prevEdge, lastNextTwinToCenter);
                var twinNextEdgeToCenter = new HalfEdge(indexHalfEdge++, facePointVertex, null, null, edge, nextEdgeToCenter);
                
                if (lastNextTwinToCenter != null)
                {
                    lastNextTwinToCenter.prevEdge = nextEdgeToCenter;
                }

                prevEdge.face = currentFace;
                prevEdge.prevEdge.face = currentFace;
                prevEdge.nextEdge = nextEdgeToCenter;

                lastNextTwinToCenter = twinNextEdgeToCenter;
                nextEdgeToCenter.twinEdge = twinNextEdgeToCenter;
                edge.prevEdge = twinNextEdgeToCenter;
                facePointVertex.outgoingEdge = twinNextEdgeToCenter;

                this.edges.Add(nextEdgeToCenter);
                this.edges.Add(twinNextEdgeToCenter);

                lastNextToCenter ??= nextEdgeToCenter;
                    
                ++indexFace;
                edge = nextEdge.nextEdge;
            } while (edge.prevEdge != face.edge);
            
            lastNextTwinToCenter.face = face;
            lastNextToCenter.nextEdge = lastNextTwinToCenter;
            lastNextTwinToCenter.prevEdge = lastNextToCenter;

            this.vertices.Add(facePointVertex);
        }

        public string ConvertToCsvFormat(string separator = "\t")
        {
            var tabSize = Mathf.Max(edges.Count, faces.Count, vertices.Count);
            var strings = new List<string>(new string[tabSize]);

            for (var i = 0; i < edges.Count; i++)
            {
                var halfEdge = edges[i];
                strings[i] += halfEdge.index + separator;
                strings[i] += halfEdge.sourceVertex.index + "; " + halfEdge.nextEdge.sourceVertex.index + separator;
                strings[i] += halfEdge.prevEdge.index + "; " + halfEdge.nextEdge.index + "; ";
                strings[i] += halfEdge.twinEdge != null ? halfEdge.twinEdge.index : "null";
                strings[i] += separator + separator;
            }

            for (var i = edges.Count; i < tabSize; i++) strings[i] += separator + separator + separator + separator;

            for (var i = 0; i < faces.Count; i++)
            {
                var f = faces[i];
                strings[i] += f.index + separator + f.edge.index + separator + separator;
            }

            for (var i = faces.Count; i < tabSize; i++) strings[i] += separator + separator + separator;

            for (var i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                var pos = vertex.position;
                strings[i] += vertex.index + separator;
                strings[i] += pos.x.ToString("N02") + " ";
                strings[i] += pos.y.ToString("N02") + " ";
                strings[i] += pos.z.ToString("N02") + separator;
                strings[i] += vertex.outgoingEdge.index + separator + separator;
            }

            var header = "HalfEdges" + separator + separator + separator + separator + "Faces" + separator + separator +
                         separator + "Vertices\n" +
                         "Index" + separator + "Src Vertex Index,end" + separator +
                         "Prev + Next + Twin HalfEdge Index" + separator + separator +
                         "Index" + separator + "HalfEdge Index" + separator + separator +
                         "Index" + separator + "Position" + separator + "OutgoingEdgeIndex" + "\n";

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
                    var worldPositionStart = transform.TransformPoint(edges[i].sourceVertex.position);
                    var worldPositionEnd = transform.TransformPoint(edges[i].nextEdge.sourceVertex.position);
                    Gizmos.DrawLine(worldPositionStart, worldPositionEnd);
                    Handles.Label((worldPositionEnd + worldPositionStart) / 2, "E : " + i, style);
                }

            style.normal.textColor = Color.blue;
            if (drawFaces)
                for (var i = 0; i < faces.Count; i++)
                {
                    var e = faces[i].edge;
                    var p0 = transform.TransformPoint(e.sourceVertex.position);
                    var p1 = transform.TransformPoint(e.nextEdge.sourceVertex.position);
                    var p2 = transform.TransformPoint(e.nextEdge.nextEdge.sourceVertex.position);
                    var p3 = _verticesForTopology > 3
                        ? e.nextEdge.nextEdge.nextEdge.sourceVertex.position
                        : Vector3.zero;
                    p3 = transform.TransformPoint(p3);

                    var index1 = e.index;
                    var index2 = e.nextEdge.index;
                    var index3 = e.nextEdge.nextEdge.index;
                    var index4 = _verticesForTopology > 3 ? e.nextEdge.nextEdge.nextEdge.index : -1;

                    var str = $"{i} ({index1},{index2},{index3},{index4})";
                    Handles.Label((p0 + p1 + p2 + p3) / _verticesForTopology, str, style);
                }
        }
    }
}