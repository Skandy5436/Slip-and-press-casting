using System;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;

namespace Slip_and_press_casting
{
  public class Slip_and_press_castingInfo : GH_AssemblyInfo
  {
    public override string Name => "Slip and press casting";

    //Return a 24x24 pixel bitmap to represent this GHA library.
    public override Bitmap Icon => null;

    //Return a short string describing the purpose of this GHA library.
    public override string Description => "";

    public override Guid Id => new Guid("37844f8b-2fe1-4131-a70c-e8e15dbc9e36");

    //Return a string identifying you or your company.
    public override string AuthorName => "";

    //Return a string representing your preferred contact details.
    public override string AuthorContact => "";
  }
}