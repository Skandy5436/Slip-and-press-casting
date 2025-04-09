using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Drawing;

namespace Slip_and_press_casting
{
    public class MyComponent1 : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public MyComponent1()
          : base("Planarity Checker ", "Pl Checker",
        "Checks if surface are planar or not",
        "Slip and Press Casting", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Brep", "B", "Input Brep to analyze", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBooleanParameter("IsPlanar", "A", "Planarity result (true/false)", GH_ParamAccess.list);
            pManager.AddLineParameter("Normals", "B", "Face normals for visualization", GH_ParamAccess.list);
            pManager.AddColourParameter("FaceColors", "C", "Colors based on deviation", GH_ParamAccess.list);
            pManager.AddNumberParameter("Deviation", "D", "Planarity deviation values", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Brep brep = null;
            if (!DA.GetData(0, ref brep)) return;

            List<bool> results = new List<bool>();
            List<Line> normalLines = new List<Line>();
            List<Color> faceColors = new List<Color>();
            List<double> planarityDeviation = new List<double>();

            for (int i = 0; i < brep.Faces.Count; i++)
            {
                BrepFace face = brep.Faces[i];
                bool isPlanar = face.IsPlanar();
                results.Add(isPlanar);

                // Compute centroid safely
                AreaMassProperties areaProps = AreaMassProperties.Compute(face);
                if (areaProps == null)
                {
                    normalLines.Add(Line.Unset);
                    faceColors.Add(Color.Gray);
                    planarityDeviation.Add(0.0);
                    continue;
                }

                Point3d centroid = areaProps.Centroid;
                Vector3d normal = face.NormalAt(0.5, 0.5);
                normal.Unitize();
                Point3d normalEnd = centroid + normal * 1.0;
                normalLines.Add(new Line(centroid, normalEnd));

                // Compute planarity deviation
                Plane bestFit;
                double deviation = ComputePlanarityDeviation(brep, face, out bestFit);
                planarityDeviation.Add(deviation);

                Color color = deviation < 0.1 ? Color.Green :
                              deviation < 0.5 ? Color.Yellow :
                              Color.Red;
                faceColors.Add(color);
            }

            DA.SetDataList(0, results);
            DA.SetDataList(1, normalLines);
            DA.SetDataList(2, faceColors);
            DA.SetDataList(3, planarityDeviation);
        }

        private double ComputePlanarityDeviation(Brep brep, BrepFace face, out Plane bestFitPlane)
        {
            List<Point3d> controlPoints = new List<Point3d>();

            foreach (int edgeIndex in face.AdjacentEdges())
            {
                BrepEdge edge = brep.Edges[edgeIndex];
                Curve curve = edge.ToNurbsCurve();

                if (curve != null)
                {
                    int sampleCount = 10;
                    for (int i = 0; i <= sampleCount; i++)
                    {
                        double t = curve.Domain.ParameterAt((double)i / sampleCount);
                        controlPoints.Add(curve.PointAt(t));
                    }
                }
            }

            Plane.FitPlaneToPoints(controlPoints, out bestFitPlane);

            double maxDeviation = 0.0;
            foreach (Point3d pt in controlPoints)
            {
                double deviation = Math.Abs(bestFitPlane.DistanceTo(pt));
                if (deviation > maxDeviation)
                    maxDeviation = deviation;
            }

            return maxDeviation;
        
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
                    using (System.IO.Stream stream = assembly.GetManifestResourceStream("Slip and press casting.Planarity1.png"))
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
            get { return new Guid("DEBCB289-2CFB-4456-827D-38EF1BAA7075"); }
        }
    }
}