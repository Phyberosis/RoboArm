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

        readonly float[,] BOUNDS = {
            {60f, 125f},  //0
            {0f, 180f},   //1
            {0f, 180f},   //2
            {0f, 165f},   //3
            {0f, 180f},   //4
            {0f, 180f},   //5
        };

        private struct Lengths
        {
            public static readonly float SHOULDER = 1.5f;
            public static readonly float UPPER_ARM = 14;
            public static readonly float FOREARM = 14;
            public static readonly float WRIST_FLEX = 4;
            public static readonly float WRIST_TWIST = 5;
            public static readonly float HAND = 5;

            public static readonly float SHOULDER_SQ = (float) Math.Pow(SHOULDER, 2);
            public static readonly float UPPER_ARM_SQ = (float)Math.Pow(UPPER_ARM, 2);
            public static readonly float FOREARM_SQ = (float)Math.Pow(FOREARM, 2);
            public static readonly float WRIST_FLEX_SQ = (float)Math.Pow(WRIST_FLEX, 2);
            public static readonly float WRIST_TWIST_SQ = (float) Math.Pow(WRIST_TWIST, 2);
            public static readonly float HAND_SQ = (float)Math.Pow(HAND, 2);
        }

        private float[] jointAngles;
        private float[] servoAngles;

        public float[] getServoAngles()
        {
            lock (this)
            {
                float[] ret = new float[servoAngles.Length];
                Array.Copy(servoAngles, ret, servoAngles.Length);
                return ret;
            }
        }

        public Arm()
        {
            for(int i=0; i<6; i++)
            {
                servoAngles[i] = STARTING_ANGLES[i];
                jointAngles[i] = STARTING_ANGLES[i];
            }
        }

        public bool setPose(Vector3 position, Vector3 orientation)
        {
            orientation = Vector3.Normalize(orientation);

            Vector3 rectifiedPosition = Vector3.Add( getOrientationOffset(orientation, position), position);

            float forearmElevation = setPosition(rectifiedPosition);
            setOrientation(orientation, forearmElevation);

            return flushPose();
        }

        private float getLengthOfHand()
        {
            //todo: factor in grip state
            return Lengths.HAND;
        }

        //returns offset due to orientation
        //assumes heading is normalized
        private Vector3 getOrientationOffset(Vector3 heading, Vector3 target)
        {
            Vector3 adjTarget = Vector3.Add(target, heading);

            Vector3 toBase = Vector3.Normalize(Vector3.Subtract(Vector3.Zero, target));

            Vector3 offset = new Vector3(0, 0, 0);
            return offset;
        }

        private float setPosition(Vector3 position)
        {
            //project onto Z plane
            Vector2 xy = new Vector2(position.X, position.Y);
            float distZ = xy.Length();
            
            float rawShoulderRot = (float)Math.Atan(xy.Y / xy.X);
            if(xy.X < 0)
            {
                if(xy.Y > 0)
                {
                    rawShoulderRot = (float) Math.PI + 180;
                }
                else
                {
                    rawShoulderRot = (float) Math.PI - 180;
                }
            }
            jointAngles[SHOULDER_ROTATION] = (float)Math.Atan(xy.Y / xy.X);

            //project onto (distZ, Z) plane
            Vector2 dz = new Vector2(distZ, position.Z);

            float l1 = Lengths.UPPER_ARM;
            float l2 = Lengths.FOREARM;
            float l3 = dz.Length();

            float l1sq = Lengths.UPPER_ARM_SQ;
            float l2sq = Lengths.FOREARM_SQ;
            float l3sq = dz.LengthSquared();

            float L2 = (float) Math.Acos((l1sq - l2sq + l3sq) / (2 * l1 * l3));
            float L3 = (float) Math.Acos((l1sq + l2sq - l3sq) / (2 * l1 * l2));

            float dzElevation = (float)Math.Atan(dz.Y / dz.X);
            float upperArmElevation = L2 + dzElevation;

            jointAngles[SHOULDER_ELEVATION] = upperArmElevation;
            jointAngles[ELBOW] = L3;

            return L3 - upperArmElevation - (float) Math.PI;
        }

        private Vector3 setOrientation(Vector3 orientation, float forearmElevation)
        {
            //Vector2 dz = 

            return Vector3.Zero;
        }

        private bool flushPose()
        {
            lock (this)
            {
                bool inBounds = true;
                for (int i = 0; i < jointAngles.Length; i++)
                {
                    float min = BOUNDS[i, 0];
                    float max = BOUNDS[i, 1];
                    float desired = jointAngles[i];

                    //check bounds
                    if (desired < min)
                    {
                        desired = min;
                        inBounds = false;
                    }
                    else if (desired > max)
                    {
                        desired = max;
                        inBounds = false;
                    }

                    servoAngles[i] = desired;
                }

                return inBounds;
            }
        }
    }
}