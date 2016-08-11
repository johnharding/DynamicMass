using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Attributes;

namespace DynamicMass.Main
{
    class DR_Attributes : GH_ComponentAttributes
    {
        public DR_Attributes(DR_Component owner)
            : base(owner)
        {
        }


        protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
        {
            if (channel == GH_CanvasChannel.Wires)
            {
                base.Render(canvas, graphics, channel);
            }

            if (channel == GH_CanvasChannel.Objects)
            {

                //base.Render(canvas, graphics, channel);



                GH_Palette palette = GH_Palette.Pink;

                Color myColor = Color.LightGray;

                switch (Owner.RuntimeMessageLevel)
                {
                    case GH_RuntimeMessageLevel.Warning:
                        myColor = Color.Orange;
                        break;

                    case GH_RuntimeMessageLevel.Error:
                        myColor = Color.Red;
                        break;
                }

                if (Owner.Hidden) myColor = Color.Gray;
                if (Owner.Locked) myColor = Color.DarkGray;


                RectangleF myRect = new RectangleF(Bounds.Location, Bounds.Size);
                //myRect.Width = 40;
                GH_Capsule capsule = GH_Capsule.CreateCapsule(myRect, palette, 10, 0);


                //capsule.Render(graphics, Selected, Owner.Locked, false);

                capsule.Render(graphics, myColor);

                capsule.Dispose();
                capsule = null;

                base.RenderComponentCapsule(canvas, graphics, false, false, false, true, true, false);

                //graphics.FillRectangle(Brushes.Black, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);

                StringFormat format = new StringFormat();
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.Trimming = StringTrimming.EllipsisCharacter;

                //RectangleF textRectangle = Bounds;
                //textRectangle.Height = 40;
                //textRectangle.Offset(0, Bounds.Size.Height / 2);
                //graphics.DrawString("Erdős–Rényi", GH_FontServer.Small, Brushes.White, textRectangle, format);

                PointF iconLocation = new PointF(ContentBox.X - 4, ContentBox.Y + 84);
                graphics.DrawImage(Owner.Icon_24x24, iconLocation);

                format.Dispose();


                //SolidBrush myBrush = new SolidBrush(Color.Black);

                //this.RenderVariableParameterUI(canvas, graphics);
                //this.InputGrip.
                //graphics.FillEllipse(myBrush, Bounds.Location.X-2, Bounds.Location.Y + 10, 4, 4);
                //graphics.FillEllipse(myBrush, Bounds.Location.X-2, Bounds.Location.Y + 30, 4, 4);
                //graphics.FillEllipse(myBrush, Bounds.Location.X-2, Bounds.Location.Y + 50, 4, 4);
            }
        }
    }
}
