using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Rhino.Geometry;

namespace DynamicMass.Elements
{
    /// <summary>
    /// John's node class with way too many properties that aren't used!
    /// </summary>
    /// 
    public class DR_Node
    {

        /// <summary>
        /// ID number for this node
        /// </summary>
        public int Reference { get; set; }

        /// <summary>
        /// Something to send back to Rhino
        /// </summary>
        public Point3d Pos { get; set; }

        /// <summary>
        /// Velocity
        /// </summary>
        public Vector3d Vel { get; set; }


        /// <summary>
        /// A measure of the absolute stress at the node
        /// </summary>
        public double Stress { get; set; }

        /// <summary>
        /// Mass in kg
        /// </summary>
        public double Mass { get; set; }

        /// <summary>
        /// Descriptive type of node (i.e. free, pinned, etc...)
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Timestep for iteration. Why this isn't global I have no idea - maybe for kicks.
        /// </summary>
        public double TimeStep { get; set; }

        /// <summary>
        /// Number of incident edges
        /// </summary>
        public int Valency { get; set; }

        /// <summary>
        /// A list of neighbouring nodes
        /// </summary>
        public List<int> Neighbours;

        /// <summary>
        /// Normal vector at the node
        /// </summary>
        private Vector3d Normal { get; set; }

        /// <summary>
        /// trimesh
        /// </summary>
        public Mesh triMesh;

        /// <summary>
        /// this
        /// </summary>
        /// <param name="point"></param>
        /// <param name="supports"></param>
        /// <param name="massType"></param>
        /// <param name="massValue"></param>
        /// <param name="I"></param>
        public DR_Node(Point3d point, List<Point3d> supports, int massType, double massValue, int I, double ts)
        {
            Pos = point;
            Type = "free";
            TimeStep = ts;
            Random rnd = new Random(I);
            Neighbours = new List<int>();
            Reference = I;


            for (int i = 0; i < supports.Count; i++)
            {
                if (Pos.DistanceTo(supports[i]) < 0.001)
                {
                    Type = "pinned";
                }
            }

            switch (massType)
            {

                case 0:
                    Mass = massValue;
                    break;

                case 1:
                    Mass = 0.0;
                    break;

                case 2:
                    Mass = 0.0;
                    break;

                default:
                    Mass = 0.0;
                    break;
            }

        }

        /// <summary>
        /// Add gravity force to DZ
        /// </summary>
        /// <param name="g"></param>
        public void Gravity(double g)
        {
            Vel += new Vector3d(0, 0, Mass * g);
        }
        /// <summary>
        /// Wind load
        /// </summary>
        /// <param name="windLoader"></param>
        public void Wind(double windLoader)
        {
            Vel += new Vector3d(windLoader, 0, 0);
        }

        /// <summary>
        /// Vertical dead load (these should just be imposed vector loads)
        /// </summary>
        /// <param name="deadLoad"></param>
        public void Dead(double deadLoad)
        {
            Vel += new Vector3d(0, 0, deadLoad);
        }

        /// <summary>
        /// Clear the list of neighbouring nodes
        /// </summary>
        public void ClearNeighbours()
        {
            Neighbours.Clear();
        }

        /// <summary>
        /// Just Euler integration
        /// </summary>
        public void Update()
        {

            if (Type == "free" || Type == "load")
            {
                Pos += Vel * TimeStep;
            }

            // Freedom in the plane
            if (Type == "roller")
            {
                Pos += new Vector3d(Vel.X, Vel.Y, 0.0);
            }

            if (Type == "fixed")
            {
            }

        }

        /// <summary>
        /// Reset the dynamic mass
        /// </summary>
        /// <param name="masstype"></param>
        public void ResetMass(int masstype)
        {
            switch (masstype)
            {

                // do nothing
                case 0:
                    break;

                // reset mass used for dynamic methods
                case 1:
                    Mass = 0.0;
                    break;

                // reset mass used for dynamic methods
                case 2:
                    Mass = 0.0;
                    break;
            }

        }


        /// <summary>
        /// Get the area mass
        /// </summary>
        /// <param name="thisNode"></param>
        /// <param name="massValue"></param>
        public void AreaMass(List<DR_Node> thisNode, double massValue)
        {
            if (Neighbours.Count == 3)
            {
                triMesh = new Mesh();

                for (int i = 0; i < Neighbours.Count; i++)
                {
                    triMesh.Vertices.Add(thisNode[Neighbours[i]].Pos);
                }

                triMesh.Faces.AddFace(0, 1, 2);
                Vector3f myVec = triMesh.FaceNormals[0];

                Mass += Rhino.Geometry.AreaMassProperties.Compute(triMesh).Area * massValue;
            }
        }



        /// <summary>
        /// Reset the nodal stress
        /// </summary>
        public void ResetStress()
        {
            Stress = 0;
        }

        /// <summary>
        /// Imposed load
        /// </summary>
        /// <param name="force3d"></param>
        public void Force(Vector3d force3d)
        {
            Vel += force3d;
        }

        /// <summary>
        /// System damping
        /// </summary>
        /// <param name="d"></param>
        public void Damp(double d)
        {
            Vel *= d;
        }
    }

}
