using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace RoboArm
{
    class Arm
    {
        const int
            SHOULDER_ROTATION = 0,
            SHOULDER_ELEVATION = 1,
            ELBOW = 2,
            WRIST_TWIST = 3,
            WRIST_ELEVAITON = 4,
            HAND = 5;

        readonly float[] STARTING_ANGLES = {
            90f,
            90f,
            145f,
            45f,
            107f,
            65f
        };

        float[] lengths = {
            1.5f,
            14f,
            13.5f,
            4f,
            5f,
            2f
        };

        float[] servoAngles;

        public Arm()
        {

            for(int i=0; i<6; i++)
            {
                servoAngles[i] = STARTING_ANGLES[i];
            }

        }

        public bool setPose(Vector3 position, Vector3 heading)
        {
            Vector3 targetPos = new Vector3(position.X, position.Y, position.Z);
            Vector3 targetDir = new Vector3(heading.X, heading.Y, heading.Z);

            Vector3 rectifiedPosition = getOrientationOffset(targetDir);
            rectifiedPosition = Vector3.Add(targetPos, rectifiedPosition);

            setPosition(rectifiedPosition);
            setOrientation(targetDir);

            return true;
        }

        //returns offset due to orientation
        private Vector3 getOrientationOffset(Vector3 heading)
        {
            Vector3 offset = new Vector3(0, 0, 0);
            return offset;
        }

        private bool setPosition(Vector3 position)
        {


            return true;
        }

        private bool setOrientation(Vector3 orientation)
        {
            return true;
        }

    }

    class Block
    {
        Block parent;
        Block child;

        private Vector3 _rotationAxis;
        private Vector3 _headPosition;

        private float _length;
        private float _rotation;

        private float[] _bounds = { 0, 180 };

        private bool _valid;

        public Block(Vector3 rotationAxis, float length)
        {
            _valid = false;

            _rotationAxis = rotationAxis;
            _length = length;
        }
    }
}