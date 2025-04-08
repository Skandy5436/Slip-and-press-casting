using System;
using System.Collections.Generic;

using Grasshopper;
using Grasshopper.Kernel;
using Rhino.Geometry;

using System.Drawing;
namespace Slip_and_press_casting
{
  public class Slip_and_press_castingComponent : GH_Component
  {
    /// <summary>
    /// Each implementation of GH_Component must provide a public 
    /// constructor without any arguments.
    /// Category represents the Tab in which the component will appear, 
    /// Subcategory the panel. If you use non-existing tab or panel names, 
    /// new tabs/panels will automatically be created.
    /// </summary>
    public Slip_and_press_castingComponent()
      : base("Draft Angle analysis", "DA Analysis",
        "Shows the Faces which can be casted or not",
        "Slip and Press Casting", "Analysis")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
            pManager.AddMeshParameter("Mesh", "M", "Mesh for draft amalysis", GH_ParamAccess.item);
            pManager.AddNumberParameter("minDraftangle","MD","Minimum draft angle in degrees",GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
            pManager.AddMeshParameter("validFaces", "VF", "Shows valid faces which can be molded(blue)", GH_ParamAccess.list);
            pManager.AddMeshParameter("problemFaces", "PF", "Shows problem faces whcih cannot be molded(red)", GH_ParamAccess.list);
            pManager.AddBooleanParameter("isMoldable", "isM", "True if all faces are moldable", GH_ParamAccess.item);
            pManager.AddNumberParameter("draftAngles", "DA", "Draft angles for each face in degrees",GH_ParamAccess.list) ;


    }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)

        {
            Mesh mesh = new Mesh();
            double minDraftAngle = 0.0;

            if (!DA.GetData(0, ref mesh)) return;
            if (!DA.GetData(1, ref minDraftAngle)) return;

            if (mesh == null || mesh.Faces.Count == 0)
            {
                DA.SetData(2, false);
                DA.SetDataList(0, new List<Mesh>());
                DA.SetDataList(1, new List<Mesh>());
                DA.SetDataList(3, new List<double>());
                return;
            }

            Vector3d pullDir = Vector3d.ZAxis;
            pullDir.Unitize();

            List<Mesh> validMeshList = new List<Mesh>();
            List<Mesh> problemMeshList = new List<Mesh>();
            List<double> angleList = new List<double>();
            bool hasUndercuts = false;

            mesh.FaceNormals.ComputeFaceNormals();

            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                Vector3d faceNormal = mesh.FaceNormals[i];

                double angle1 = Vector3d.VectorAngle(faceNormal, pullDir) * (180.0 / Math.PI);
                double angle2 = Vector3d.VectorAngle(faceNormal, -pullDir) * (180.0 / Math.PI);
                double angle = Math.Min(angle1, angle2);

                angleList.Add(angle);

                Mesh singleFace = ExtractFace(mesh, i);

                if (angle > 90.0)
                {
                    hasUndercuts = true;
                    singleFace.VertexColors.CreateMonotoneMesh(Color.Red);
                    problemMeshList.Add(singleFace);
                }
                else if (angle >= minDraftAngle || Math.Abs(angle) < 1e-3)
                {
                    singleFace.VertexColors.CreateMonotoneMesh(Color.Blue);
                    validMeshList.Add(singleFace);
                }
                else
                {
                    singleFace.VertexColors.CreateMonotoneMesh(Color.Red);
                    problemMeshList.Add(singleFace);
                }
            }

            bool isMoldable = !hasUndercuts && problemMeshList.Count == 0;

            DA.SetDataList(0, validMeshList);
            DA.SetDataList(1, problemMeshList);
            DA.SetData(2, isMoldable);
            DA.SetDataList(3, angleList);
        }











        // Helper function to isolate a mesh face
        private Mesh ExtractFace(Mesh mesh, int faceIndex)
        {
            Mesh faceMesh = new Mesh();
            MeshFace face = mesh.Faces[faceIndex];

            faceMesh.Vertices.Add(mesh.Vertices[face.A]);
            faceMesh.Vertices.Add(mesh.Vertices[face.B]);
            faceMesh.Vertices.Add(mesh.Vertices[face.C]);

            if (face.IsQuad)
            {
                faceMesh.Vertices.Add(mesh.Vertices[face.D]);
                faceMesh.Faces.AddFace(0, 1, 2, 3);
            }
            else
            {
                faceMesh.Faces.AddFace(0, 1, 2);
            }

            faceMesh.Normals.ComputeNormals();
            faceMesh.Compact();
            return faceMesh;
        }





        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// You can add image files to your project resources and access them like this:
        /// return Resources.IconForThisComponent;
        /// </summary>
        protected override System.Drawing.Bitmap Icon => null;

    /// <summary>
    /// Each component must have a unique Guid to identify it. 
    /// It is vital this Guid doesn't change otherwise old ghx files 
    /// that use the old ID will partially fail during loading.
    /// </summary>
    public override Guid ComponentGuid => new Guid("828fa935-6c7f-45bb-b89d-1e5fd3570ef5");
  }
}