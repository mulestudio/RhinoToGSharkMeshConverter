using GShark.Core;
using GShark.Geometry;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using Rhino.Render.ChangeQueue;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using gs = GShark.Geometry;

namespace MeshClosestPointTest
{
    public class GSharkClosestPoint : Command
    {
        public GSharkClosestPoint()
        {
            // Rhino only creates one instance of each command class defined in a
            // plug-in, so it is safe to store a refence in a static property.
            Instance = this;
        }

        ///<summary>The only instance of this command.</summary>
        public static GSharkClosestPoint Instance { get; private set; }

        ///<returns>The command name as it appears on the Rhino command line.</returns>
        public override string EnglishName => "GSharkClosestPoint";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: start here modifying the behaviour of your command.

            //Mesh
            gs.Mesh gsharkmesh;
            using (GetObject go = new GetObject())
            {
                go.SetCommandPrompt("Please select a mesh");
                go.GeometryFilter = ObjectType.Mesh;
                go.Get();
                if (go.CommandResult() != Result.Success)
                    return go.CommandResult();

                Rhino.Geometry.Mesh mesh = go.Objects()[0].Mesh();

                RhinoApp.WriteLine("Mesh selected successfully!");
                gsharkmesh = RhinoMeshToGSharkMesh(mesh);
                //RhinoApp.WriteLine(gsharkmesh.ToString());
            }


            Point3d pt0;
            gs.Point3 gspoint = new gs.Point3(0, 0, 0);
            using (GetPoint getPointAction = new GetPoint())
            {
                getPointAction.SetCommandPrompt("Please select the start point");
                if (getPointAction.Get() != GetResult.Point)
                {
                    RhinoApp.WriteLine("No start point was selected.");
                    return getPointAction.CommandResult();
                }
                pt0 = getPointAction.Point();
                gspoint = new gs.Point3(pt0.X, pt0.Y, pt0.Z);
            }
            Point3 CP = gsharkmesh.ClosestPoint(gspoint);
            //RhinoApp.WriteLine(CP.ToString());

            Point3d CPRhino = new Point3d(CP.X, CP.Y, CP.Z);
            // Add the point to the Rhino document (bake the point)
            Guid pointId = doc.Objects.AddPoint(CPRhino);
            // If you want to display the point in the viewport immediately, update the display
            doc.Views.Redraw();
            // Optionally, you can also select the baked point to highlight it
            doc.Objects.Select(pointId);

            return Result.Success;

        }

        /// <summary>
        /// Function to convert a Rhino.Geometry.Mesh to GShark.Geometry.Mesh 
        /// </summary>
        /// <param name="rhinoMesh">Rhino.Geometry.Mesh.</param>
        /// <returns>GShark.Geometry.Mesh</returns>

        public gs.Mesh RhinoMeshToGSharkMesh(Rhino.Geometry.Mesh rhinoMesh)
        {
            // Estracts the mesh vertices as a list of GShark.Geometry.Point3 
            List<gs.Point3> mesh_vertices = rhinoMesh.Vertices.Select(v => new gs.Point3(v.X, v.Y, v.Z)).ToList();

            // List of vertices extracted from the faces using BoundaryVertexIndexList() and GetNgonAndFacesEnumerable()
            // This is to avoid the central vertex of Ngon faces
            var verticesFromFaces = new HashSet<Point3>();

            var faceIndexes = new List<List<int>>();
            var Faces = rhinoMesh.GetNgonAndFacesEnumerable();
            foreach (Rhino.Geometry.MeshNgon face in Faces)
            {
                var indices = face.BoundaryVertexIndexList();

                // Error message that shows how many Ngon are detected
                if (indices.Length > 4) {RhinoApp.WriteLine("Ngon detected.");}

                var indicesList = indices.ToList();
                var coversionList = new List<int>();

                for (int i = 0; i<indicesList.Count; i++) 
                {
                    var pointToAdd = mesh_vertices[((int)indicesList[i])];
                    verticesFromFaces.Add(pointToAdd);
                    int indexOfVertexWeAreAdding = verticesFromFaces.ToList().IndexOf(pointToAdd);
                    coversionList.Add(indexOfVertexWeAreAdding);
                }
                faceIndexes.Add(coversionList);
            }
            return new gs.Mesh(verticesFromFaces.ToList(), faceIndexes);
        }
    } 
}
