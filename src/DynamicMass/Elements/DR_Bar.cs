using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;
using Grasshopper.Kernel.Types;

namespace DynamicMass.Elements
{
    /// <summary>
    /// John's Bar Class
    /// </summary>
    public class DR_Bar
    {
        /// <summary>
        /// Youngs modulus of bar
        /// </summary>
        public double E { get; set; }

        /// <summary>
        /// Area of cross section in m2
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// Ref
        /// </summary>
        public int Reference { get; set; }

        /// <summary>
        /// TO node ref
        /// </summary>
        public int I1 { get; set; }

        /// <summary>
        /// FROM node ref
        /// </summary>
        public int I2 { get; set; }

        /// <summary>
        /// Force in the member (negative is compression)
        /// </summary>
        public double Tension { get; set; }

        /// <summary>
        /// F/A
        /// </summary>
        public double Stress { get; set; }

        /// <summary>
        /// Natural length of bar (rest length)
        /// </summary>
        public double NatLength { get; set; }

        /// <summary>
        /// Current length
        /// </summary>
        public double L { get; set; }

        /// <summary>
        /// Alive (legacy)
        /// </summary>
        public bool Live { get; set; }

        /// <summary>
        /// Rhino Line element
        /// </summary>
        public Line Line { get; set; }

        /// <summary>
        /// Constructor from an existing line
        /// 'Stiffness' factor needs removing at some point
        /// </summary>
        /// <param name="thisBar"></param>
        /// <param name="thisNode"></param>
        /// <param name="stiffness"></param>
        public DR_Bar(Line thisBar, List<DR_Node> thisNode, double stiffness)
        {

            // Find the 'FROM' node by getting nearest neighbour
            double minDis = double.PositiveInfinity;
            int ID = 0;

            for (int i = 0; i < thisNode.Count; i++)
            {
                double distance = thisBar.From.DistanceTo(thisNode[i].Pos);
                if (distance < minDis)
                {
                    minDis = distance;
                    ID = i;
                }
            }

            I1 = ID;

            // Find the 'TO' node by getting nearest neighbour
            minDis = double.PositiveInfinity;
            ID = 0;

            for (int i = 0; i < thisNode.Count; i++)
            {
                double distance = thisBar.To.DistanceTo(thisNode[i].Pos);
                if (distance < minDis)
                {
                    minDis = distance;
                    ID = i;
                }
            }

            I2 = ID;

            // Add line
            Line = new Rhino.Geometry.Line(thisNode[I1].Pos, thisNode[I2].Pos);
            L = Line.Length;

            // Increase Valency of the local nodes
            thisNode[I1].Valency++;
            thisNode[I2].Valency++;

            /* 
             * stiffness now EA/L:
             * 210 Youngs Modulus of Steel
             * 0.1m2 Approximate area of 254x254 Steel beam
             * TODO: Use variable elements - compatible with SharpFE
             */

            // E = stiffness * A / L
            // Note that in this case, we are using E to depict the spring constant and setting the area to 1.0.
            E = stiffness;
            Area = 1.0;

        }

        /// <summary>
        /// Assign the natural length
        /// </summary>
        /// <param name="nat"></param>
        public void AssignNat(double nat)
        {
            // Natural length equal to length for analysis
            // -1 gives natural length to be the current length
            if (nat == -1) NatLength = L;
            else NatLength = nat;
        }

        /// <summary>
        /// Alter stiffness on the fly
        /// </summary>
        /// <param name="newValue"></param>
        public void TweakStiffness(double newValue)
        {
            E = newValue;
        }

        /// <summary>
        /// Update the line to suit any new node references
        /// </summary>
        /// <param name="thisNode"></param>
        public void Update(List<DR_Node> thisNode)
        {
            Line = new Rhino.Geometry.Line(thisNode[I1].Pos, thisNode[I2].Pos);
            L = Line.Length;
        }

        /// <summary>
        /// Exert forces on connected nodes
        /// </summary>
        /// <param name="thisNode"></param>
        public void Influence(List<DR_Node> thisNode)
        {
            // Find the force (in N) (divide by length if we are not using springs)
            Tension = -E * Area * (NatLength - L);

            // Dividing by current length gives the unit vector
            //thisNode[I1].Vel += (thisNode[I2].Pos - thisNode[I1].Pos) * Tension;
            //thisNode[I2].Vel += (thisNode[I1].Pos - thisNode[I2].Pos) * Tension;

            // Dividing by current length gives the unit vector
            thisNode[I1].Vel += ((thisNode[I2].Pos - thisNode[I1].Pos) / L) * Tension;
            thisNode[I2].Vel += ((thisNode[I1].Pos - thisNode[I2].Pos) / L) * Tension;

        }

        /// <summary>
        /// Calculate dynamic masses
        /// </summary>
        /// <param name="thisNode"></param>
        /// <param name="masstype"></param>
        public void DynamicMass(List<DR_Node> thisNode, List<double> massValue)
        {

            thisNode[I1].Mass += massValue[I1] * L / 2;
            thisNode[I2].Mass += massValue[I2] * L / 2;


            //dynamic mass split between nodes

            //assumed mass per unit length for each node
            //thisNode[I1].Stress += Tension;
            //thisNode[I2].Stress += Tension;

            //Set the stress
            //Stress = Tension / Area;
        }



        public void NodalNeighbours(List<DR_Node> thisNode)
        {
            thisNode[I1].Neighbours.Add(I2);
            thisNode[I2].Neighbours.Add(I1);
        }

    }
}
