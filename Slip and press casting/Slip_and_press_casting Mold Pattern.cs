using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Slip_and_press_casting
{
    public class MoldPattern : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public MoldPattern()
          : base("Mold Pattern ", "Pattern",
        "If mold should be waffled ",
        "Slip and Press Casting", "Pattern")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "M", "Input mesh to generate ribs on", GH_ParamAccess.item);
            pManager.AddNumberParameter("Rib Spacing", "S", "Spacing between ribs", GH_ParamAccess.item, 10.0);
            pManager.AddNumberParameter("Rib Thickness", "T", "Thickness of ribs", GH_ParamAccess.item, 2.0);
            pManager.AddNumberParameter("Rib Offset", "O", "Offset from the mesh boundary to start the ribs", GH_ParamAccess.item, 5.0);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddBrepParameter("Ribs", "R", "Generated rib geometry", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Mesh mesh = null;
            double ribSpacing = 0;
            double ribThickness = 0;
            double ribOffset = 0;

            // Get data from Grasshopper inputs
            if (!DA.GetData(0, ref mesh)) return;
            if (!DA.GetData(1, ref ribSpacing)) return;
            if (!DA.GetData(2, ref ribThickness)) return;
            if (!DA.GetData(3, ref ribOffset)) return;

            if (mesh == null || ribSpacing <= 0 || ribThickness <= 0 || ribOffset < 0)
                return;

            List<Brep> ribList = new List<Brep>();

            BoundingBox bbox = mesh.GetBoundingBox(true);
            double minX = bbox.Min.X + ribOffset;
            double maxX = bbox.Max.X - ribOffset;
            double minY = bbox.Min.Y + ribOffset;
            double maxY = bbox.Max.Y - ribOffset;
            double minZ = bbox.Min.Z + ribOffset;
            double maxZ = bbox.Max.Z - ribOffset;

            if (minX >= maxX || minY >= maxY || minZ >= maxZ)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "RibOffset too large.");
                return;
            }

            // X-direction ribs (YZ planes)
            for (double x = minX; x <= maxX; x += ribSpacing)
            {
                Plane sectionPlane = new Plane(new Point3d(x, 0, 0), Vector3d.XAxis);
                var curves = MeshPlaneIntersection(mesh, sectionPlane);
                foreach (var c in curves)
                {
                    if (c.IsValid && c.IsClosed)
                    {
                        Extrusion ext = Extrusion.Create(c, ribThickness, true);
                        if (ext != null)
                            ribList.Add(ext.ToBrep());
                    }
                }
            }

            // Y-direction ribs (XZ planes)
            for (double y = minY; y <= maxY; y += ribSpacing)
            {
                Plane sectionPlane = new Plane(new Point3d(0, y, 0), Vector3d.YAxis);
                var curves = MeshPlaneIntersection(mesh, sectionPlane);
                foreach (var c in curves)
                {
                    if (c.IsValid && c.IsClosed)
                    {
                        Extrusion ext = Extrusion.Create(c, ribThickness, true);
                        if (ext != null)
                            ribList.Add(ext.ToBrep());
                    }
                }
            }

            // Z-direction ribs (XY planes)
            for (double z = minZ; z <= maxZ; z += ribSpacing)
            {
                Plane sectionPlane = new Plane(new Point3d(0, 0, z), Vector3d.ZAxis);
                var curves = MeshPlaneIntersection(mesh, sectionPlane);
                foreach (var c in curves)
                {
                    if (c.IsValid && c.IsClosed)
                    {
                        Extrusion ext = Extrusion.Create(c, ribThickness, true);
                        if (ext != null)
                            ribList.Add(ext.ToBrep());
                    }
                }
            }

            DA.SetDataList(0, ribList);


        }
        private List<Curve> MeshPlaneIntersection(Mesh mesh, Plane plane)
        {
            List<Curve> result = new List<Curve>();
            var intersections = Rhino.Geometry.Intersect.Intersection.MeshPlane(mesh, plane);
            if (intersections != null)
            {
                foreach (Polyline p in intersections)
                {
                    if (p.IsValid && p.Count > 1)
                    {
                        Curve c = p.ToNurbsCurve();
                        result.Add(c);
                    }
                }
            }
            return result;
        
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
                    using (System.IO.Stream stream = assembly.GetManifestResourceStream("Slip and press casting.Waffle1.png"))
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
            get { return new Guid("EC628644-3588-4469-B885-CAACF0CA0786"); }
        }
    }
}