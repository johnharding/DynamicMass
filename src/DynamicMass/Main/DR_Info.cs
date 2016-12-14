using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicMass.Main
{
    class DR_Info : Grasshopper.Kernel.GH_AssemblyInfo
    {
        public override string Description
        {
            get { return "Funicular forms at your fingertips"; }
        }
        public override System.Drawing.Bitmap Icon
        {
            get { return DynamicMass.Properties.Resources.dynamicmass02; }
        }
        public override string Name
        {
            get { return "DynamicMass"; }
        }
        public override string Version
        {
            //first release
            get { return "0.3.0"; }
        }
        public override Guid Id
        {
            get { return new Guid("{19D0565A-50F4-4139-92ED-DA0A35459274}"); }
        }

        public override string AuthorName
        {
            get { return "John Harding"; }
        }
        public override string AuthorContact
        {
            get { return "johnharding@fastmail.fm"; }
        }
    }
}
