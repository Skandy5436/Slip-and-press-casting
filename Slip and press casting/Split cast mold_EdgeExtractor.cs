using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Slip_and_press_casting
{
    public class Split_cast_mold_EdgeExtractor : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Split_cast_mold_EdgeExtractor class.
        /// </summary>
        public Split_cast_mold_EdgeExtractor()
          : base("Brep_NakedEdges", "NE",
              "Extracts naked edges of input geometry for flap generation",
              "Slip and Press Casting", "MoldMaking")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Input Brep to extract naked edges from", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("NakedEdges", "NE", "Extracted naked edges from the Brep", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep brep = null;
            if (!DA.GetData(0, ref brep) || brep == null || !brep.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid Brep input.");
                return;
            }

            List<Curve> outputCurves = new List<Curve>();

            foreach (BrepFace face in brep.Faces)
            {
                int added = 0;

                foreach (BrepLoop loop in face.Loops)
                {
                    foreach (BrepTrim trim in loop.Trims)
                    {
                        BrepEdge edge = trim.Edge;

                        if (edge != null && edge.Valence == EdgeAdjacency.Naked)
                        {
                            Curve edgeCurve = edge.DuplicateCurve();
                            if (edgeCurve != null)
                            {
                                outputCurves.Add(edgeCurve);
                                added++;
                                if (added >= 2)
                                    break;
                            }
                        }
                    }
                    if (added >= 2)
                        break;
                }
            }

            DA.SetDataList(0, outputCurves);

        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6D32B30C-2C5D-44CD-8586-C3D1F927338B"); }
        }
    }
}