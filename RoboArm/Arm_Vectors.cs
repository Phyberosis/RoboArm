using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace RoboArm
{
    class ArmSection
    {
        private bool valid = false;

        private const float minR = 0, maxR = 180;
        private float rot = (maxR+minR)/2f;

        private ArmSection parent;
        private Vector3 origin, head, self;
        public readonly float length;

        public ArmSection(ArmSection _parent, float _length)
        {
            parent = _parent;
            if(_parent == null)
            {
                origin = Vector3.Zero;
            }
            else
            {
                origin = parent.getHead();
            }
            length = _length;
        }

        public bool setRotation(float r)
        {
            bool inRange = true;
            float last = rot;
            if(r < minR)
            {
                rot = minR;
                inRange = false;
            }
            else if(r > maxR)
            {
                rot = maxR;
                inRange = false;
            }
            else
            {
                rot = r;
                inRange = true;
            }

            if(rot != last)
            {
                valid = false;
            }

            return inRange;
        }

        public void setOrientation(Vector3 orientation)
        {

        }

        private void recalculate()
        {
            valid = true;
        }

        public Vector3 getSelf()
        {
            if (!valid) recalculate();

            return self;
        }

        public Vector3 getHead()
        {
            if (!valid) recalculate();

            return head;
        }
    }

    class Arm_Vectors : Arm
    {
        enum SectionName
        {
            HAND_TWIST = 4,
            HAND_FLEX = 3,
            ELBOW_FOREARM = 2,
            UPPER_ARM_ELEVATION = 1,
            SHOULDER_ROTATION = 0

        }

        private Vector3 cursor, gimbal;
        private ArmSection[] sections = new ArmSection[5];
        private float grip = 0f;
        private const float gripMin = 60f, gripMax = 125f;

        private readonly float[] lengths =
        {
            2f,
            10.5f,
            9.7f,
            3f,
            14f
        };

        public Arm_Vectors()
        {
            cursor = new Vector3(16f, 0, 10f);
            gimbal = Vector3.UnitZ;

            ArmSection parent = null;
            for(int i=0; i<5; i++)
            {
                sections[i] = new ArmSection(parent, lengths[i]);
                parent = sections[i];
            }
        }

        public Vector3 getCursor()
        {
            return cursor;
        }
        public Vector3 getGimbal()
        {
            return gimbal;
        }
        public float getGrip()
        {
            return grip;
        }
        public void setGrip(float percent)
        {
            if (percent < 0) percent = 0;
            if (percent > 1f) percent = 1f;

            grip = percent;
        }

        public float[] getServoAngles()
        {
            throw new NotImplementedException();
        }

        public bool setPose(Vector3 position, Vector3 orientation)
        {
            Vector3 oriRotVec = Vector3.Cross(Vector3.UnitZ, position);
            Vector3 oriOffset = Vector3.Normalize(Vector3.Cross(orientation, oriRotVec));

            sections[(int)SectionName.HAND_TWIST].setOrientation(oriOffset);

            throw new NotImplementedException();
        }
    }
}
