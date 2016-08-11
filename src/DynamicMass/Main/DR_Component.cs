using System;
using System.Drawing;
using Grasshopper.Kernel;
using System.Collections.Generic;
using Rhino;
using Grasshopper;
using DynamicMass.Elements;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

namespace DynamicMass.Main
{
    /// <summary>
    /// Dynamic relaxation component
    /// </summary>
    public class DR_Component : GH_Component
    {
        private int counter;
        private int massType;

        private List<Point3d> myNodes = new List<Point3d>();
        private List<DR_Node> localNodes = new List<DR_Node>();
        private List<Point3d> outNodes = new List<Point3d>();

        private List<Point3d> mySupports = new List<Point3d>();
        private List<double> myStiffnesses = new List<double>();
        private List<double> myNatLengths = new List<double>();

        private List<double> myMassList = new List<double>();

        private List<Line> myBars = new List<Line>();
        private List<DR_Bar> localBars = new List<DR_Bar>();
        private List<Line> outBars = new List<Line>();

        private List<double> outMasses = new List<double>();
        private List<double> outStresses = new List<double>();
        private List<GH_Mesh> outMeshes = new List<GH_Mesh>();

        private double d;
        private double g;
        private double ts;

        private List<string> remarks = new List<string>();

        /// <summary>
        /// DR Component Constructor
        /// </summary>
        public DR_Component()
            : base("DynamicMass", "DynamicMass", "A funicular form-finding tool that uses dynamic masses", "Extra", "Rosebud")
        {
            d = 0;
            g = 0;
            ts = 0;
            counter = 0;
        }

        /// <summary>
        /// Grasshopper overidden inputs
        /// </summary>
        /// <param name="pManager"></param>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            // Geometry
            pManager.AddPointParameter("Nodes", "Nodes", "The initial nodes", GH_ParamAccess.list);
            pManager.AddLineParameter("Springs", "Springs", "The initial springs as lines", GH_ParamAccess.list);
            pManager.AddPointParameter("Supports", "Supports", "A list of support points (pinned)", GH_ParamAccess.list);

            // Springs
            pManager.AddNumberParameter("Stiffnesses", "Stiffnesses", "Stiffness constants for each spring (N/m)", GH_ParamAccess.list, 0.1);
            pManager.AddNumberParameter("NatLengths", "NatLengths", "Natural lengths of each spring", GH_ParamAccess.list, 0.0);

            // Masses
            pManager.AddNumberParameter("MassDensity", "MassDensity", "Mass density for each node (kg, kg/m OR kg/m2 depending on MassType)", GH_ParamAccess.list, 1.0);
            pManager.AddIntegerParameter("MassType", "MassType", "0: Constant, 1: Dynamic (edge length), 2: Dynamic (area based)", GH_ParamAccess.item, 1);

            // Globals
            pManager.AddNumberParameter("Damping", "Damping", "Viscous damping coefficient", GH_ParamAccess.item, 0.95);
            pManager.AddNumberParameter("Gravity", "Gravity", "Gravitational constant", GH_ParamAccess.item, 9.81);
            pManager.AddNumberParameter("TimeStep", "TimeStep", "TimeStep (only Euler integration here at the moment)", GH_ParamAccess.item, 0.005);

            // Reset
            pManager.AddBooleanParameter("Reset", "Reset", "Reset the system", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Grasshopper overidden outputs
        /// </summary>
        /// <param name="pManager"></param>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Nodes", "Nodes", "The relaxed nodes", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Valence", "Valence", "The valency of each node", GH_ParamAccess.list);
            pManager.AddNumberParameter("Masses", "Masses", "The nodal masses currently being applied (kg)", GH_ParamAccess.list);
            pManager.AddLineParameter("Bars", "Bars", "The relaxed bars", GH_ParamAccess.list);
            pManager.AddNumberParameter("Forces", "Forces", "The spring forces (N)", GH_ParamAccess.list);
            pManager.AddIntegerParameter("Iterations", "Iterations", "The number of iterations since last reset", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Neighbours", "Neighbours", "The nodal neighbour indices", GH_ParamAccess.tree);
            pManager.AddMeshParameter("Meshes", "Meshes", "The triangular meshes if used", GH_ParamAccess.list);
        }

        /// <summary>
        /// Component solveinstance method. Called by grasshopper when solution is expired.
        /// </summary>
        /// <param name="DA"></param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            #region getdata

            bool reset = false;
            DA.GetData("Reset", ref reset);
            if (reset)
            {
                counter = 0;
                return;
            }

            // These global inputs can be dynamic, hence we get them here
            DA.GetData("Damping", ref d); //damping
            DA.GetData("Gravity", ref g); //global forces
            DA.GetData("TimeStep", ref ts); //timestep

            #endregion

            #region initialise

            // If we are in real-time mode, then DON'T UPDATE EVERYTHING!
            if (counter == 0)
            {
                myNodes.Clear();
                mySupports.Clear();
                myStiffnesses.Clear();
                myNatLengths.Clear();
                myBars.Clear();
                myMassList.Clear();
                remarks.Clear();

                localNodes.Clear();
                localBars.Clear();

                // Get things
                DA.GetDataList<Point3d>("Nodes", myNodes);
                DA.GetDataList<Line>("Springs", myBars);
                DA.GetDataList<Point3d>("Supports", mySupports);

                DA.GetDataList<double>("Stiffnesses", myStiffnesses);
                DA.GetDataList<double>("NatLengths", myNatLengths);

                DA.GetDataList<double>("MassDensity", myMassList);
                DA.GetData("MassType", ref massType);  // Maybe make dynamic...



                // STIFF
                // Number of bars must equal stiffnesses
                if (myStiffnesses.Count != myBars.Count && myStiffnesses.Count != 1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Stiffness list count must be either equal to the spring list count or a single value");
                    return;
                }

                // Cover the case of just one stiffness supplied 
                else if (myStiffnesses.Count == 1)
                {
                    for (int i = 1; i < myBars.Count; i++)
                    {
                        myStiffnesses.Add(myStiffnesses[0]);
                    }
                    remarks.Add("single value stiffness copied for all springs");
                }



                // NAT
                // Number of bars must equal natural lengths
                if (myNatLengths.Count != myBars.Count && myNatLengths.Count != 1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "NatLength list count must be either equal to the spring list count or a single value");
                    return;
                }


                // Cover the case of just one natLength supplied 
                else if (myNatLengths.Count == 1)
                {
                    for (int i = 1; i < myBars.Count; i++)
                    {
                        myNatLengths.Add(myNatLengths[0]);
                    }
                    remarks.Add("single value natLength copied for all springs");
                }


                //MASS
                // Number of masses must equal vertex count
                if (myMassList.Count != myNodes.Count && myMassList.Count != 1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Mass list count must be either equal to the vertex list count or a single value");
                    return;
                }


                // Cover the case of just one massValue supplied 
                else if (myMassList.Count == 1)
                {
                    for (int i = 1; i < myNodes.Count; i++)
                    {
                        myMassList.Add(myMassList[0]);
                    }
                    remarks.Add("single value mass copied for all nodes");
                }




                // ADD THINGS
                // Add the nodes to the system
                for (int i = 0; i < myNodes.Count; i++)
                {

                    localNodes.Add(new DR_Node(myNodes[i], mySupports, massType, myMassList[i], i, ts));

                }

                // Delete duplicate nodes




                // Add the bars as a final thing.
                for (int i = 0; i < myBars.Count; i++)
                {

                    localBars.Add(new DR_Bar(myBars[i], localNodes, myStiffnesses[i]));

                    // Sending -1 gives natural length to be the current length
                    localBars[i].AssignNat(myNatLengths[i]);

                    // Record the nodal neighbours
                    localBars[i].NodalNeighbours(localNodes);
                }
            }

            // Add the remarks AFTER the warnings and errors
            //for (int i = 0; i < remarks.Count; i++)
            //{
            // AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, remarks[i]);
            //}


            // Last gasp 'let's get out of here' check
            if (localNodes == null || localNodes.Count < 2)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Not enough stuff to calculate");
                return;
            }

            // Check system is statically determinate
            bool flag = false;
            for (int i = 0; i < localNodes.Count; i++)
            {
                if (localNodes[i].Valency > 3)
                    flag = true;
            }

            if (flag)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "One or more nodes have valency > 3. System is not statically determinate!");
            }




            // Now run the simulation
            ReLaX();
            counter++;

            #endregion

            #region output
            // Copy the node data. This is so wrong at the moment...
            List<int> outValency = new List<int>();

            for (int i = 0; i < localNodes.Count; i++)
            {
                outMasses.Add(localNodes[i].Mass);
                outNodes.Add(localNodes[i].Pos);
                outValency.Add(localNodes[i].Valency);

                GH_Mesh gMesh = new GH_Mesh(localNodes[i].triMesh);
                outMeshes.Add(gMesh);
            }

            // Copy the bar data
            for (int i = 0; i < localBars.Count; i++)
            {
                outBars.Add(localBars[i].Line);
                // MPa output not Pa.
                //outStresses.Add(localBars[i].Stress / 1000000);
                outStresses.Add(localBars[i].Tension);
            }

            DA.SetDataList(0, outNodes);
            DA.SetDataList(1, outValency);
            DA.SetDataList(2, outMasses);
            DA.SetDataList(3, outBars);
            DA.SetDataList(4, outStresses);
            DA.SetData(5, counter);
            //TODO Output neighbours
            DA.SetDataList(7, outMeshes);

            outNodes.Clear();
            outMasses.Clear();
            outValency.Clear();
            outBars.Clear();
            outStresses.Clear();

            outMeshes.Clear();

            #endregion
        }

        /// <summary>
        /// The Dynamic Relaxation solver
        /// </summary>
        private void ReLaX()
        {
            // 1. Reset mass and stress
            for (int i = 0; i < localNodes.Count; i++)
            {
                localNodes[i].ResetMass(massType);
                //localNodes[i].ResetStress();
            }

            // 2. Calculate forces from bars
            for (int i = 0; i < localBars.Count; i++)
            {
                localBars[i].Influence(localNodes);
            }

            // 3. Now calculate the mass for this iteration
            switch (massType)
            {
                case 0:
                    for (int i = 0; i < localNodes.Count; i++)
                    {
                        localNodes[i].Mass = myMassList[i];
                    }
                    break;
                case 1:
                    for (int i = 0; i < localBars.Count; i++)
                    {
                        localBars[i].DynamicMass(localNodes, myMassList);
                    }
                    break;

                case 2:
                    for (int i = 0; i < localNodes.Count; i++)
                    {
                        localNodes[i].AreaMass(localNodes, myMassList[i]);
                    }
                    break;
            }


            // 4. Apply gravity using the correct masess and damp the system
            for (int i = 0; i < localNodes.Count; i++)
            {
                localNodes[i].Gravity(g);
                localNodes[i].Damp(d);
            }

            // 5. Update the nodes and bars
            for (int i = 0; i < localNodes.Count; i++)
            {
                localNodes[i].Update();
            }

            for (int i = 0; i < localBars.Count; i++)
            {
                localBars[i].Update(localNodes);
            }

        }

        /// <summary>
        /// Override the ComponentGUID
        /// </summary>
        public override Guid ComponentGuid
        {
            //generated at http://www.newguid.com/
            get { return new Guid("2315396b-68d9-4633-8dfc-4b2c2fb4c61f"); }
        }

        /// <summary>
        /// Grasshopper Icon override
        /// </summary>
        protected override Bitmap Icon
        {
            get
            {
                return DynamicMass.Properties.Resources.dynamicmass02;
            }
        }


        public override void CreateAttributes()
        {
            m_attributes = new DR_Attributes(this);
        }

    }
}
