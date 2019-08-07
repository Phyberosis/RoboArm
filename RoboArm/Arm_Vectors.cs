using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RoboArm
{
    class Arm_Vectors
    {
        // project raw onto target
        private Vector3 project(Vector3 raw, Vector3 target)
        {
            Vector3 dir = Vector3.Normalize(target);
            float l = Vector3.Dot(raw, dir);
            Vector3 result = Vector3.Multiply(dir, l);

            return result;
        }

        // squash raw onto plain
        private Vector3 squash(Vector3 raw, Vector3 normal)
        {
            Vector3 difference = project(raw, normal);
            Vector3 result = Vector3.Subtract(raw, difference);

            return result;
        }

        // get angle between a and b
        private float angleBetween(Vector3 a, Vector3 b)
        {
            float top = Vector3.Dot(a, b);
            float bot = a.Length() * b.Length();
            float raw = (float) Math.Acos(top / bot);

            return raw;
        }

        public bool setPose(Vector3 position, Vector3 orientation)
        {
            orientation = Vector3.Normalize(orientation);
            Vector3 adjusted = getAdjustedTarget(orientation, position);

            return true;
        }

        private Vector3 getAdjustedTarget(Vector3 heading, Vector3 target)
        {
            Vector3 wristFlexAxis = Vector3.Cross(target, Vector3.UnitZ);
            if(wristFlexAxis.Length() == 0) wristFlexAxis = Vector3.UnitY;

            Vector3 flexComponent = squash(heading, wristFlexAxis);
            Vector3 handOffsetDir = Vector3.Cross(flexComponent, target);
            if(handOffsetDir.Length() == 0)
            {
                Vector3 targetXY = new Vector3(target.X, target.Y, 0);
                handOffsetDir = Vector3.Cross(flexComponent, targetXY);
            }
            handOffsetDir = Vector3.Normalize(handOffsetDir);


        }

        public void test()
        {
            Vector3 Z = Vector3.UnitZ;
            Vector3 Z3 = Vector3.Multiply(Z, 3);
            Vector3 X4 = Vector3.Multiply(Vector3.UnitX, 4);
            Vector3 X5 = Vector3.Multiply(Vector3.UnitX, 5);
            Vector3 h5 = new Vector3(4, 0, 3);

            Vector3 a = new Vector3(2, 0, 0);
            Vector3 b = new Vector3(-1, 0, 0);

            float resultF =  angleBetween(a, b) * 180f / (float) Math.PI;
            Vector3 resultV = Z;

            //project x5 onto h5
            //expect vector of length 4
            Vector3 h = Vector3.Normalize(h5);
            float l = Vector3.Dot(X5, h);
            resultV = project(new Vector3(2, 0, 0), new Vector3(2, 2, 0));

            Console.WriteLine(resultF + " " + a.ToString() + " " + b.ToString());
        }
    }
}
