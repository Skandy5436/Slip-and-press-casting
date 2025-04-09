using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Slip_and_press_casting
{
    public class Slip_cast_mold : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Slip_cast_mold class.
        /// </summary>
        public Slip_cast_mold()
          : base("MoldGenerator", "MG",
              "Create a mold to 3d print for making pop mold for slip casting",
              "Slip and Press Casting", "MoldMaking_splitbyPlane")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Input Geometry", "G", "Base Brep geometry to split into mold parts", GH_ParamAccess.item);
            pManager.AddVectorParameter("Pull Direction", "D", "Pull direction for parting plane", GH_ParamAccess.item);
            pManager.AddNumberParameter("Offset Factor", "O", "Offset factor for parting plane position (0.0 - 1.0)", GH_ParamAccess.item, 0.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Mold Parts", "M", "Resulting mold Breps after split", GH_ParamAccess.list);
            pManager.AddPlaneParameter("Parting Plane", "P", "Plane used to split the mold", GH_ParamAccess.item);
            pManager.AddNumberParameter("Volumes", "V", "Volume of each mold part", GH_ParamAccess.list);
            pManager.AddBrepParameter("Split Bounding Boxes", "B", "Bounding boxes of mold parts", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Brep inputGeometry = null;
            Vector3d pullDirection = Vector3d.Unset;
            double planeOffsetFactor = 0.0;

            if (!DA.GetData(0, ref inputGeometry)) return;
            if (!DA.GetData(1, ref pullDirection)) return;
            if (!DA.GetData(2, ref planeOffsetFactor)) return;

            pullDirection.Unitize();

            BoundingBox bbox = inputGeometry.GetBoundingBox(true);
            Point3d bboxCenter = bbox.Center;

            Box moldBox = new Box(bbox);
            Brep moldBrep = moldBox.ToBrep();

            double offset = bbox.Diagonal.Length * planeOffsetFactor;
            Point3d planeOrigin = bboxCenter + (pullDirection * offset);

            Plane splitPlane;
            if (Math.Abs(pullDirection.X) > Math.Abs(pullDirection.Y) && Math.Abs(pullDirection.X) > Math.Abs(pullDirection.Z))
                splitPlane = new Plane(planeOrigin, Vector3d.XAxis);
            else if (Math.Abs(pullDirection.Y) > Math.Abs(pullDirection.X) && Math.Abs(pullDirection.Y) > Math.Abs(pullDirection.Z))
                splitPlane = new Plane(planeOrigin, Vector3d.YAxis);
            else
                splitPlane = new Plane(planeOrigin, Vector3d.ZAxis);

            double planeSize = bbox.Diagonal.Length * 2;
            PlaneSurface planeSurface = new PlaneSurface(splitPlane, new Interval(-planeSize, planeSize), new Interval(-planeSize, planeSize));

            Brep[] splitBreps = inputGeometry.Split(planeSurface.ToBrep(), Rhino.RhinoMath.SqrtEpsilon);
            Brep[] splitBoundingBox = moldBrep.Split(planeSurface.ToBrep(), Rhino.RhinoMath.SqrtEpsilon);

            if (splitBreps == null || splitBreps.Length < 2 || splitBoundingBox == null || splitBoundingBox.Length < 2)
            {
                Rhino.RhinoApp.WriteLine("Brep split failed. Try adjusting the parting plane offset.");
                return;
            }

            List<double> volumes = new List<double>();
            foreach (Brep part in splitBreps)
            {
                double volume = 0.0;
                VolumeMassProperties props = VolumeMassProperties.Compute(part);
                if (props != null) volume = props.Volume;
                volumes.Add(volume);
            }

            DA.SetDataList(0, splitBreps);
            DA.SetData(1, splitPlane);
            DA.SetDataList(2, volumes);
            DA.SetDataList(3, splitBoundingBox);
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
                    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                    using (var stream = assembly.GetManifestResourceStream("Slip_and_press_casting.icon-MoldMaking.png"))
                    {
                        if (stream != null)
                            return new System.Drawing.Bitmap(stream);
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
            get { return new Guid("FC6F15D4-6B46-41CF-BACF-FFD75D1ECBB0"); }
        }
    }
}