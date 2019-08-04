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
        const int DEBUG_LEVEL = 10;

        const int
            SHOULDER_ROTATION = 0,
            SHOULDER_ELEVATION = 1,
            ELBOW = 2,
            WRIST_TWIST = 3,
            WRIST_FLEX = 4,
            HAND = 5;

        readonly float[] STARTING_ANGLES = 
        {
            90f,
            90f,
            145f,
            45f,
            107f,
            65f
        };

        readonly float[] SERVO_OFFSETS =
        {
            0f,
            0f,
            0f,
            0f,
            0f,
            0f
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
            public static readonly float WRIST_HEIGHT = 3;
            public static readonly float HAND = 16;

            public static readonly float SHOULDER_SQ = (float) Math.Pow(SHOULDER, 2);
            public static readonly float UPPER_ARM_SQ = (float)Math.Pow(UPPER_ARM, 2);
            public static readonly float FOREARM_SQ = (float)Math.Pow(FOREARM, 2);
            public static readonly float WRIST_HEIGHT_SQ = (float) Math.Pow(WRIST_TWIST, 2);
            public static readonly float HAND_SQ = (float)Math.Pow(HAND, 2);
        }

        private float[] jointAngles;
        private float[] servoAngles;

        private Vector3 cursor = new Vector3(5,5,5);
        private Vector3 gimbal = new Vector3(0, 0, 1);
        private float grip = 0;

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
            servoAngles = new float[STARTING_ANGLES.Length];
            jointAngles = new float[STARTING_ANGLES.Length];
            for (int i=0; i<6; i++)
            {
                servoAngles[i] = STARTING_ANGLES[i];
                jointAngles[i] = STARTING_ANGLES[i];
            }
        }

        public Vector3 getCursor()
        {
            return new Vector3(cursor.X, cursor.Y, cursor.Z);
        }

        public Vector3 getGimbal()
        {
            return new Vector3(gimbal.X, gimbal.Y, gimbal.Z);
        }

        public float getGrip()
        {
            return grip;
        }

        public void setGrip(float percent)
        {
            grip = percent;

            float closed = BOUNDS[HAND, 0];
            float open = BOUNDS[HAND, 1];
            float raw = percent * Math.Abs(open - closed) / 100;

            jointAngles[HAND] = closed + raw;
        }

        public bool setPose(Vector3 position, Vector3 orientation)
        {
            orientation = Vector3.Normalize(orientation);
            cursor = new Vector3(position.X, position.Y, position.Z);
            gimbal = new Vector3(orientation.X, orientation.Y, orientation.Z); ;

            debug("position: " + position.ToString(), 6);

            Vector3 rectifiedPosition = Vector3.Add( getOrientationOffset(orientation, position), position);

            float forearmElevation = setPosition(rectifiedPosition);
            setOrientation(rectifiedPosition, orientation, forearmElevation);

            return flushPose();
        }

        //todo
        private float getLengthOfHand()
        {
            //todo: factor in grip state
            return Lengths.HAND;
        }

        //returns offset due to orientation
        //assumes heading is normalized
        private Vector3 getOrientationOffset(Vector3 heading, Vector3 target)
        {
            debug("---" + heading.ToString() + target.ToString(), 10);

            Vector3 targetToShoulder = Vector3.Normalize(Vector3.Subtract(Vector3.Zero, target));
            Vector3 handHeightOffset = Vector3.Multiply(heading, Lengths.WRIST_HEIGHT);

            // two cross-products gets vector pointing to shoulder in x, y and perpendicular to target in x, d
            Vector3 rotVec = Vector3.Cross(target, targetToShoulder);
            Vector3 offset = Vector3.Cross(target, rotVec);
            offset = Vector3.Normalize(offset);
            offset = Vector3.Multiply(offset, getLengthOfHand());   // offset for hand length - dir * length = vector for offset
            offset = Vector3.Add(offset, handHeightOffset);         // offset for hand "height" due to servos being stacked

            debug("offset: " + offset.ToString(), 6);
            return offset;
        }

        private float setPosition(Vector3 position)
        {
            //project onto Z plane
            Vector2 xy = new Vector2(position.X, position.Y);
            float distZ = xy.Length();
            
            float rawShoulderRot = (float)Math.Atan(xy.Y / xy.X);
            //debug("--xy: " + xy.ToString(), 10);
            if (xy.X < 0)
            {
                if(xy.Y > 0)
                {
                    rawShoulderRot = (float)(rawShoulderRot + Math.PI);
                }
                else
                {
                    rawShoulderRot = (float)(rawShoulderRot - Math.PI);
                }
            }
            jointAngles[SHOULDER_ROTATION] = rawShoulderRot;
            debug("shoulder: " + rawShoulderRot, 6);

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

        private void setOrientation(Vector3 target, Vector3 orientation, float forearmElevation)
        {
            Vector2 xy = new Vector2(orientation.X, orientation.Y);
            float d = xy.Length();
            Vector2 dz = new Vector2(d, orientation.Z);

            float handElevation = (float)Math.Acos(Vector2.Dot(dz, Vector2.UnitX) / dz.Length());
            handElevation = (float)Math.PI / 2 - handElevation;

            jointAngles[WRIST_FLEX] = handElevation - forearmElevation;

            //direction to target in xy
            Vector3 dVec = new Vector3(target.X, target.Y, 0f);
            Vector3 crossDZ = Vector3.Cross(dVec, Vector3.UnitZ);

            float handTwist = (float)Math.Acos(Vector3.Dot(orientation, crossDZ) / crossDZ.Length());
            handTwist = (float)Math.PI - handTwist;

            jointAngles[WRIST_TWIST] = handTwist;
        }

        private bool flushPose()
        {
            lock (this)
            {
                bool inBounds = true;
                StringBuilder angles = new StringBuilder();
                for (int i = 0; i < jointAngles.Length; i++)
                {

                    float min = BOUNDS[i, 0];
                    float max = BOUNDS[i, 1];
                    float desired = (float)(jointAngles[i] * 180/Math.PI);

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
                    }else if (float.IsNaN(desired))
                    {
                        inBounds = false;
                        desired = (max + min) / 2;
                    }

                    servoAngles[i] = desired;
                    angles.Append(i + ", " + desired+" ");
                }

                debug("flush: " + angles.ToString() + " inBounds: "+inBounds+"\r\n", 5);
                return inBounds;
            }
        }

        private void debug(String str, int lvl)
        {
            if(DEBUG_LEVEL >= lvl)
            {
                Console.WriteLine(str);
            }
        }
    }
}