using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Slip_and_press_casting
{
    public class UndercutDetector : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public UndercutDetector()
          : base("Undercut Detector ", "UcD",
        "Check if there are undercuts ",
        "Slip and Press Casting", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Input brep", GH_ParamAccess.item);
            pManager.AddVectorParameter("Pull Direction", "D", "Direction of pull or extrusion", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min Draft Angle", "A", "Minimum draft angle in degrees", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Undercut Faces", "U", "Faces with insufficient draft angle", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Brep brep = null;
            Vector3d pullDirection = Vector3d.Unset;
            double minDraftAngle = 0;

            if (!DA.GetData(0, ref brep)) return;
            if (!DA.GetData(1, ref pullDirection)) return;
            if (!DA.GetData(2, ref minDraftAngle)) return;

            List<Brep> undercutFaces = new List<Brep>();

            foreach (BrepFace face in brep.Faces)
            {
                // Ensure the face is trimmed and has proper parameters
                if (!face.IsValid) continue;

                Vector3d normal = face.NormalAt(0.5, 0.5);
                if (!normal.IsValid) continue;

                double angle = Vector3d.VectorAngle(normal, pullDirection) * (180.0 / Math.PI);
                if (angle < minDraftAngle)
                {
                    Brep singleFaceBrep = face.DuplicateFace(false);
                    undercutFaces.Add(singleFaceBrep);
                }
            }

            DA.SetDataList(0, undercutFaces);
        }

        

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                try
                {
                    System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    using (System.IO.Stream stream = assembly.GetManifestResourceStream("Slip_and_press_casting.undercut.png"))
                    {
                        if (stream != null)
                        {
                            return new System.Drawing.Bitmap(stream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Rhino.RhinoApp.WriteLine("Error loading icon: " + ex.Message);
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6ADC0364-E28C-4040-8678-24033832D27F"); }
        }
    }
}