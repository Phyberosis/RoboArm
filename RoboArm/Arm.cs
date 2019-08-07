using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace RoboArm
{
    interface Arm
    {
        float[] getServoAngles();

        Vector3 getCursor();

        Vector3 getGimbal();

        float getGrip();

        void setGrip(float percent);

        bool setPose(Vector3 position, Vector3 orientation);
    }
}
