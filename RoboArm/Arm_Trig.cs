using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;

namespace RoboArm
{
    class Arm_Trig : Arm
    {
        const float PI = (float)Math.PI;
        struct Lengths
        {
            public static readonly float SHOULDER = 2f;
            public static readonly float UPPER_ARM = 10.5f;
            public static readonly float FOREARM = 9.7f;
            public static readonly float WRIST_HEIGHT = 3f;
            public static readonly float HAND_LENGTH = 14f;

            public static readonly float SHOULDER_SQ = (float)Math.Pow(SHOULDER, 2);
            public static readonly float UPPER_ARM_SQ = (float)Math.Pow(UPPER_ARM, 2);
            public static readonly float FOREARM_SQ = (float)Math.Pow(FOREARM, 2);
            public static readonly float WRIST_HEIGHT_SQ = (float)Math.Pow(WRIST_HEIGHT, 2);
            public static readonly float HAND_SQ = (float)Math.Pow(HAND_LENGTH, 2);

        }
        const int DEBUG_LEVEL = 10;

        const int
            SHOULDER_ROTATION = 5,
            SHOULDER_ELEVATION = 4,
            ELBOW = 3,
            WRIST_FLEX = 2,
            WRIST_TWIST = 1,
            HAND = 0;

        readonly float[] STARTING_ANGLES = 
        {
            0f,
            90f,
            145f,
            45f,
            107f,
            65f
        };

        readonly float[] SERVO_DIR =
        {
            1f,
            1f,
            -1f,
            -1f,
            1f,
            1f
        };

        readonly float[] SERVO_OFFSETS =
        {
            0f,
            18f,
            -120f,
            14f,
            -25.42f,
            90f
        };

        readonly float[,] BOUNDS = {
            {10f, 70f},  //0
            {0f, 180f},   //1
            {0f, 180f},   //2
            {0f, 180f},   //3
            {0f, 180f},   //4
            {0f, 180f},   //5
        };
        
        float[] jointAngles;
        float[] servoAngles;

        Vector3 cursor = new Vector3(11.5f, 0, 11f);
        Vector3 gimbal = Vector3.Normalize(new Vector3(0, 0, 1));
        float grip = 0;

        public float[] getServoAngles() 
        {
            lock (this)
            {
                float[] ret = new float[servoAngles.Length];
                Array.Copy(servoAngles, ret, servoAngles.Length);
                return ret;
            }
        }

        public Arm_Trig()
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
            debug("cursor: " + cursor.ToString(), 5);
            return new Vector3(cursor.X, cursor.Y, cursor.Z);
        }

        public Vector3 getGimbal()
        {
            debug("gimbal: " + gimbal.ToString(), 5);
            return new Vector3(gimbal.X, gimbal.Y, gimbal.Z);
        }

        public float getGrip()
        {
            return grip;
        }

        public void setGrip(float percent)
        {

            if(percent > 1f)
            {
                percent = 1f;
            }else if(percent < 0f)
            {
                percent = 0f;
            }

            grip = percent;

            float closed = BOUNDS[HAND, 0];
            float open = BOUNDS[HAND, 1];
            float raw = percent * (float)Math.Abs(open - closed);
            jointAngles[HAND] = (closed + raw) * (float)Math.PI / 180f;
        }

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
            float raw = (float)Math.Acos(top / bot);

            return raw;
        }

        public bool setPose(Vector3 position, Vector3 orientation)
        {
            orientation = Vector3.Normalize(orientation);
            cursor = new Vector3(position.X, position.Y, position.Z);
            gimbal = new Vector3(orientation.X, orientation.Y, orientation.Z); ;

            //debug("position: " + position.ToString(), 6);

            Vector3 rectifiedPosition = Vector3.Add( getOrientationOffset(orientation, position), position);
            debug("rectified position: " + rectifiedPosition.ToString(), 6);

            float forearmElevation = setPosition(rectifiedPosition);
            setOrientation(position, orientation, forearmElevation);

            return flushPose();
        }

        //todo
        private float getLengthOfHand()
        {
            //todo: factor in grip state
            float theta = (1f-grip) * (PI / 3f);
            float l = Lengths.HAND_LENGTH - (3f - 3f*(float)Math.Cos(theta));
            return l;
        }

        //returns offset due to orientation
        //assumes heading is normalized
        private Vector3 getOrientationOffset(Vector3 heading, Vector3 target)
        {
            //debug("---HT" + heading.ToString() + target.ToString(), 10);

            //Vector3 targetToShoulder = Vector3.Normalize(Vector3.Subtract(Vector3.Zero, target));
            
            //debug("---SH" + /*targetToShoulder.ToString() + */handHeightOffset.ToString(), 10);

            Vector3 wristFlexAxis = Vector3.Cross(target, Vector3.UnitZ);
            if(wristFlexAxis.Length() == 0) wristFlexAxis = Vector3.UnitY;

            Vector3 flexComponent = squash(heading, wristFlexAxis);
            Vector3 handHeightOffset = Vector3.Multiply(flexComponent, -Lengths.WRIST_HEIGHT);
            Vector3 handOffsetDir = Vector3.Normalize( Vector3.Cross(flexComponent, wristFlexAxis));
            //debug("---offdir" + handOffsetDir.ToString(), 10);
            Vector3 offset = Vector3.Multiply(handOffsetDir, -getLengthOfHand());
            offset = Vector3.Add(offset, handHeightOffset);
            //debug("---RO" + rotVec.ToString() + offset.ToString(), 10);

            //debug("offset: " + offset.ToString(), 6);
            return offset;
        }

        private float setPosition(Vector3 position)
        {
            //project onto Z plane
            Vector2 xy = new Vector2(position.X, position.Y);
            float distZ = xy.Length();
            
            float rawShoulderRot = (float)Math.Atan(xy.Y / xy.X);

            if (xy.X < 0)
            {
                distZ = -distZ;
            }
            distZ += Lengths.SHOULDER;
            jointAngles[SHOULDER_ROTATION] = rawShoulderRot;

            //project onto (distZ, Z) plane
            Vector2 dz = new Vector2(distZ, position.Z);
            debug("dz : " + dz.ToString(), 6);

            float l1 = Lengths.UPPER_ARM;
            float l2 = Lengths.FOREARM;
            float l3 = dz.Length();

            float l1sq = Lengths.UPPER_ARM_SQ;
            float l2sq = Lengths.FOREARM_SQ;
            float l3sq = dz.LengthSquared();

            float L2 = (float) Math.Acos((l1sq - l2sq + l3sq) / (2 * l1 * l3));
            float L3 = (float) Math.Acos((l1sq + l2sq - l3sq) / (2 * l1 * l2));

            float dzElevation = (float)Math.Atan(dz.Y / dz.X);
            if (dz.X < 0)
            {
                dzElevation += PI;
            }
            float upperArmElevation = L2 + dzElevation;

            jointAngles[SHOULDER_ELEVATION] = upperArmElevation;
            jointAngles[ELBOW] = L3;

            return L3 + upperArmElevation - PI;
        }

        private void setOrientation(Vector3 target, Vector3 orientation, float forearmElevation)
        {
            Vector3 wristFlexAxis = Vector3.Cross(target, Vector3.UnitZ);
            if (wristFlexAxis.Length() == 0) wristFlexAxis = Vector3.UnitY;

            Vector3 flexComponent = squash(orientation, wristFlexAxis);
            Vector3 targetXY = new Vector3(target.X, target.Y, 0);

            float wristElevation = angleBetween(flexComponent, targetXY) - PI/2f;
            //debug("---we " + wristElevation * 180/Math.PI + " : " + forearmElevation * 180 / Math.PI, 10);
            //debug("---ft " + flexComponent + " : " + targetXY, 10);

            //Vector3 twistComponent = squash(orientation, targetXY);
            float wristTwist = angleBetween(orientation, wristFlexAxis);
            debug("---twist " + wristTwist, 10);

            jointAngles[WRIST_TWIST] = PI - wristTwist; // 90f * (float)Math.PI/180f;
            jointAngles[WRIST_FLEX] = -forearmElevation + wristElevation;
        }

        private bool flushPose()
        {
            lock (this)
            {
                bool inBounds = true;
                StringBuilder angles = new StringBuilder();
                StringBuilder rawAngles = new StringBuilder();
                for (int i = 0; i < jointAngles.Length; i++)
                {

                    float min = BOUNDS[i, 0];
                    float max = BOUNDS[i, 1];
                    float desired = (float)(jointAngles[i] * 180/Math.PI);
                    rawAngles.Append(i + ", " + desired + " | ");

                    if(SERVO_DIR[i] == -1f)
                    {
                        desired = 180 - desired;
                    }
                    desired += SERVO_OFFSETS[i];

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
                    angles.Append(i + ", " + desired+" | ");
                }

                debug("flush raw     : " + rawAngles.ToString() + " inBounds: "+inBounds+"\r\n", 5);
                debug("flush adjusted: " + angles.ToString() + " inBounds: "+inBounds+"\r\n", 7);
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