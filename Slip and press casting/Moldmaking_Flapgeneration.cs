using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Slip_and_press_casting
{
    public class Moldmaking_Flapgeneration : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Moldmaking_Flapgeneration class.
        /// </summary>
        public Moldmaking_Flapgeneration()
          : base("Moldmaking_Flapgeneration", "flapGen",
              "Create planar surfaces as flaps for POP/ Concrete casting ",
              "Slip and Press Casting", "MoldMaking")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("inputBrep", "inBrep", "Base Brep with naked edges", GH_ParamAccess.item);
            pManager.AddNumberParameter("Flap Height", "FlapH", "Height of the flap to be extruded", GH_ParamAccess.item, 5.0); // default height
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Flap Surfaces", "F", "Generated flap surfaces", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep B = null;
            double H = 0.0;

            if (!DA.GetData(0, ref B) || B == null || !B.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Invalid Brep input.");
                return;
            }

            if (!DA.GetData(1, ref H)) return;

            List<Brep> flaps = new List<Brep>();
            double tol = RhinoDoc.ActiveDoc.ModelAbsoluteTolerance;

            // Get all naked edge curves from the Brep
            Curve[] nakedEdges = B.DuplicateNakedEdgeCurves(true, true);

            foreach (Curve edge in nakedEdges)
            {
                if (!edge.IsLinear(tol)) continue;

                // Find midpoint of the edge
                double midT;
                edge.LengthParameter(edge.GetLength() * 0.5, out midT);
                Point3d midPt = edge.PointAt(midT);

                // Find closest face to the edge midpoint
                BrepFace closestFace = null;
                double minDist = double.MaxValue;

                foreach (BrepFace face in B.Faces)
                {
                    double u, v;
                    if (face.ClosestPoint(midPt, out u, out v))
                    {
                        Point3d ptOnFace = face.PointAt(u, v);
                        double dist = midPt.DistanceTo(ptOnFace);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closestFace = face;
                        }
                    }
                }

                if (closestFace == null) continue;

                // Get the normal at that closest point
                double u2, v2;
                if (!closestFace.ClosestPoint(midPt, out u2, out v2)) continue;
                Vector3d normal = closestFace.NormalAt(u2, v2);
                normal.Unitize();

                // Offset edge along normal
                Curve offset = edge.DuplicateCurve();
                offset.Translate(normal * H);

                // Build 4-sided flap
                Line side1 = new Line(edge.PointAtStart, offset.PointAtStart);
                Line side2 = new Line(edge.PointAtEnd, offset.PointAtEnd);

                List<Curve> boundary = new List<Curve>
                {
                    edge,
                    new LineCurve(side1),
                    offset,
                    new LineCurve(side2)
                };

                Curve[] joined = Curve.JoinCurves(boundary, tol);
                if (joined.Length > 0)
                {
                    Brep[] flapBrep = Brep.CreatePlanarBreps(joined[0], tol);
                    if (flapBrep != null && flapBrep.Length > 0)
                        flaps.AddRange(flapBrep);
                }
            }

            DA.SetDataList(0, flaps);
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
                    using (System.IO.Stream stream = assembly.GetManifestResourceStream("Slip_and_press_casting.Asset 2.png"))
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
            get { return new Guid("B59EAE32-050B-4F31-8BA2-B1E6E610BDA3"); }
        }
    }
}