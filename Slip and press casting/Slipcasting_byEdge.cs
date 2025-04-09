using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;

namespace Slip_and_press_casting
{
    public class Slipcasting_byEdge : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Slipcasting_B class.
        /// </summary>
        public Slipcasting_byEdge()
          : base("MoldGeneratorbyEdge", "MG_edge",
              "SCreate a Mold by splitting input geometry by edges",
              "Slip and Press Casting", "MoldMaking")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("InputBrep", "B", "Input Brep geometry", GH_ParamAccess.item);
            pManager.AddNumberParameter("DraftAngleTolerance", "Tol", "Tolerance for comparing draft angles in degrees", GH_ParamAccess.item, 5.0);
            pManager.AddBooleanParameter("ManualOverride", "M", "Manually override the main pull direction", GH_ParamAccess.item, false);
            pManager.AddVectorParameter("OverrideDirection", "D", "Manual pull direction", GH_ParamAccess.item, Vector3d.ZAxis);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("MoldParts", "M", "Resulting mold parts", GH_ParamAccess.list);
            pManager.AddVectorParameter("PullDirections", "P", "Pull direction of each mold part", GH_ParamAccess.list);
            pManager.AddVectorParameter("MainPullDirection", "MP", "Main pull direction", GH_ParamAccess.item);
            pManager.AddLineParameter("Normals", "N", "Normal visualization arrows", GH_ParamAccess.list);
            pManager.AddBooleanParameter("HasDraft", "D", "Draft angle condition per face", GH_ParamAccess.list);
            pManager.AddIntegerParameter("MoldTags", "T", "Face-to-cluster tag", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep brep = null;
            double tolDeg = 5.0;
            bool manualOverride = false;
            Vector3d overrideDir = Vector3d.ZAxis;

            if (!DA.GetData(0, ref brep)) return;
            DA.GetData(1, ref tolDeg);
            DA.GetData(2, ref manualOverride);
            DA.GetData(3, ref overrideDir);

            if (brep == null || !brep.IsValid)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Invalid Brep");
                return;
            }

            double angleTolRad = RhinoMath.ToRadians(tolDeg);
            List<BrepFace> faces = new List<BrepFace>(brep.Faces);
            List<Vector3d> faceNormals = new List<Vector3d>();
            List<Point3d> faceCenters = new List<Point3d>();
            List<bool> draftFlags = new List<bool>();

            int largestIndex = -1;
            double largestArea = 0;

            for (int i = 0; i < faces.Count; i++)
            {
                BrepFace face = faces[i];
                Interval uDom = face.Domain(0);
                Interval vDom = face.Domain(1);
                double u = uDom.T0 + uDom.Length / 2;
                double v = vDom.T0 + vDom.Length / 2;

                Vector3d n = face.NormalAt(u, v);
                n.Unitize();
                faceNormals.Add(n);

                Point3d pt = face.PointAt(u, v);
                faceCenters.Add(pt);

                double area = AreaMassProperties.Compute(face)?.Area ?? 0;
                if (area > largestArea)
                {
                    largestArea = area;
                    largestIndex = i;
                }
            }

            Vector3d mainPull = (manualOverride) ? overrideDir : (largestIndex >= 0 ? faceNormals[largestIndex] : Vector3d.ZAxis);

            foreach (Vector3d normal in faceNormals)
            {
                double angle = Vector3d.VectorAngle(normal, mainPull);
                draftFlags.Add(angle > angleTolRad && Math.Abs(angle - Math.PI) > angleTolRad);
            }

            List<List<int>> clusters = new List<List<int>>();
            List<Vector3d> clusterDirs = new List<Vector3d>();
            bool[] used = new bool[faces.Count];

            for (int i = 0; i < faces.Count; i++)
            {
                if (used[i]) continue;
                List<int> cluster = new List<int>();
                Vector3d baseDir = faceNormals[i];
                cluster.Add(i);
                used[i] = true;

                for (int j = 0; j < faces.Count; j++)
                {
                    if (i == j || used[j]) continue;
                    Vector3d testDir = faceNormals[j];
                    double angle = Vector3d.VectorAngle(baseDir, testDir);
                    if (angle < angleTolRad || Math.Abs(angle - Math.PI) < angleTolRad)
                    {
                        Point3d centerI = faceCenters[i];
                        Point3d centerJ = faceCenters[j];
                        Line rayI = new Line(centerI, baseDir * 100);
                        Line rayJ = new Line(centerJ, testDir * 100);
                        if (!brep.IsPointInside(rayI.PointAt(1), 0.01, true) && !brep.IsPointInside(rayJ.PointAt(1), 0.01, true))
                        {
                            cluster.Add(j);
                            used[j] = true;
                        }
                    }
                }

                clusters.Add(cluster);
                clusterDirs.Add(baseDir);
            }

            List<Brep> moldedBreps = new List<Brep>();
            List<int> tagList = new List<int>(new int[faces.Count]);
            for (int i = 0; i < faces.Count; i++) tagList[i] = -1;

            for (int k = 0; k < clusters.Count; k++)
            {
                List<Brep> clusterBreps = new List<Brep>();
                foreach (int idx in clusters[k])
                {
                    BrepFace f = faces[idx];
                    Brep single = f.DuplicateFace(false);
                    clusterBreps.Add(single);
                    tagList[idx] = k;
                }
                Brep[] joined = Brep.JoinBreps(clusterBreps, 0.01);
                if (joined.Length > 0)
                    moldedBreps.Add(joined[0]);
            }

            List<Line> arrows = new List<Line>();
            for (int i = 0; i < faceCenters.Count; i++)
            {
                Line arrow = new Line(faceCenters[i], faceCenters[i] + faceNormals[i] * 2);
                arrows.Add(arrow);
            }

            DA.SetDataList(0, moldedBreps);
            DA.SetDataList(1, clusterDirs);
            DA.SetData(2, mainPull);
            DA.SetDataList(3, arrows);
            DA.SetDataList(4, draftFlags);
            DA.SetDataList(5, tagList);
        
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
            get { return new Guid("6091A10B-2607-426C-9650-795DE19CF4DB"); }
        }
    }
}