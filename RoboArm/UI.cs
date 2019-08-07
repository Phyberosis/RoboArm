using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Numerics;

namespace RoboArm
{
    public partial class UI : Form
    {
        const bool SERIAL = true;
        SerialPort mSerialPort;

        const String mPortName = "COM4";
        const int mBaudRate = 57600;
        const Parity mParity = Parity.None;
        const int mDataBits = 8;
        const StopBits mStopBits = StopBits.One;

        BackgroundWorker worker = new BackgroundWorker();

        String keysDown = "";
        Arm arm = new Arm_Trig();

        private bool running = true;
        private bool stopped = false;

        public UI()
        {
            Control.CheckForIllegalCrossThreadCalls = false;

            InitializeComponent();

            if (SERIAL)
            {
                mSerialPort = new SerialPort(mPortName, mBaudRate, mParity, mDataBits, mStopBits);
                mSerialPort.Close();
                mSerialPort.DtrEnable = true;
                mSerialPort.RtsEnable = true;
                mSerialPort.ReceivedBytesThreshold = 1;
                mSerialPort.Open();
            }

            worker.DoWork += new DoWorkEventHandler(this.Async);
            worker.RunWorkerAsync();
        }

        private long currentTimeMilis()
        {
            return DateTime.Now.Ticks;
        }

        private void Async(object sender, DoWorkEventArgs e)
        {
            long last = currentTimeMilis();
            const float NORM_RATE = 10;
            float rate = NORM_RATE;
            float range = 30;
            bool canContinue;

            lock (arm)
            {
                canContinue = running;
            }

            while (canContinue)
            {

                lock (arm)
                {
                    canContinue = running;

                    float dt = currentTimeMilis() - last;
                    dt /= 10000000f;
                    if (dt < 0.01f) continue;

                    last = currentTimeMilis();

                    StringBuilder angles = new StringBuilder();

                    Vector3 displacement = Vector3.Zero;
                    float dDist = rate * dt;

                    //cursor
                    if (keyDown("w"))
                    {
                        displacement.Y -= 1;
                    }
                    else if (keyDown("s"))
                    {
                        displacement.Y += 1;
                    }

                    if (keyDown("a"))
                    {
                        displacement.X += 1;
                    }
                    else if (keyDown("d"))
                    {
                        displacement.X -= 1;
                    }

                    if (keyDown("z"))
                    {
                        displacement.Z -= 1;
                    }
                    else if (keyDown("x"))
                    {
                        displacement.Z += 1;
                    }

                    float gripRate = 0.02f;

                    //grip
                    if (keyDown("f"))
                    {
                        arm.setGrip(arm.getGrip() + gripRate);
                    }
                    else if (keyDown("r"))
                    {
                        arm.setGrip(arm.getGrip() - gripRate);
                    }
                    Vector3 gimbal = arm.getGimbal();
                    Vector3 pitch = Vector3.Cross(gimbal, Vector3.UnitY);
                    Vector3 roll = Vector3.Cross(gimbal, Vector3.UnitX);
                    Vector3 yaw = Vector3.Cross(gimbal, Vector3.UnitZ);
                    if (yaw.Length() == 0) yaw = roll;

                    float rotRate = 0.005f;

                    //gimbal
                    if (keyDown("i"))
                    {
                        roll = Vector3.Multiply(Vector3.Normalize(roll), rotRate);
                        gimbal = Vector3.Normalize(Vector3.Add(roll, gimbal));
                    }
                    else if (keyDown("k"))
                    {
                        roll = Vector3.Multiply(Vector3.Normalize(roll), -rotRate);
                        gimbal = Vector3.Normalize(Vector3.Add(roll, gimbal));
                    }

                    if (keyDown("j"))
                    {
                        pitch = Vector3.Multiply(Vector3.Normalize(pitch), -rotRate);
                        gimbal = Vector3.Normalize(Vector3.Add(pitch, gimbal));
                    }
                    else if (keyDown("l"))
                    {
                        pitch = Vector3.Multiply(Vector3.Normalize(pitch), +rotRate);
                        gimbal = Vector3.Normalize(Vector3.Add(pitch, gimbal));
                    }

                    if (keyDown("n"))
                    {
                        yaw = Vector3.Multiply(Vector3.Normalize(yaw), -rotRate);
                        gimbal = Vector3.Normalize(Vector3.Add(yaw, gimbal));
                    }
                    else if (keyDown("m"))
                    {
                        yaw = Vector3.Multiply(Vector3.Normalize(yaw), +rotRate);
                        gimbal = Vector3.Normalize(Vector3.Add(yaw, gimbal));
                    }

                    gimbal.X = Math.Abs(gimbal.X);
                    gimbal.Z = Math.Abs(gimbal.Z);

                    if (keyDown("v")) rate = NORM_RATE / 5; else rate = NORM_RATE;

                    //Console.WriteLine("---" + displacement);
                    if (!displacement.Equals(Vector3.Zero))
                    {
                        displacement = Vector3.Normalize(displacement);
                    }
                    displacement = Vector3.Multiply(displacement, dDist);
                    Vector3 cursor = arm.getCursor();
                    Vector3 target = Vector3.Add(cursor, displacement);
                    target.X = Math.Abs(target.X);

                    if (target.Length() > range)
                    {
                        target = Vector3.Multiply(Vector3.Normalize(target), range);
                    }

                    //Console.WriteLine("---" + arm.getCursor());
                    arm.setPose(target, gimbal);

                    float[] servoPos = arm.getServoAngles();

                    for (int index = 0; index < servoPos.Length; index++)
                    {
                        float rot = servoPos[index];
                        if (float.IsNaN(rot)) continue;
                        angles.Append("_").Append(index).Append(",").Append(rot).Append(";");
                    }

                    label1.Text = keysDown.ToString() + " \r\n" + cursor.ToString();

                    String msg = angles.ToString();
                    if (SERIAL) mSerialPort.Write(msg);
                    label2.Text = msg;
                }
            }

            lock (arm)
            {
                stopped = true;
            }
        }

        private string parseKey(string k)
        {
            return ("." + k + ".").ToUpper();
        }

        private bool keyDown(string k)
        {
            lock (keysDown)
            {
                return keysDown.Contains(parseKey(k));
            }
        }

        private void handleKeyDown(object sender, KeyEventArgs e)
        {
            lock (keysDown)
            {
                String key = parseKey(e.KeyCode.ToString());
                if (!keysDown.Contains(key) && key.Length == 3)
                {
                    keysDown += key;
                }
            }
        }

        private void handleKeyUp(object sender, KeyEventArgs e)
        {
            lock (keysDown)
            {
                String key = parseKey(e.KeyCode.ToString());
                keysDown = keysDown.Replace(key, "");
            }
        }

        private void UI_FormClosing(object sender, FormClosingEventArgs e)
        {
            lock (arm)
            {
                running = false;
            }

            while (true)
            {
                lock (arm)
                {
                    if (stopped)
                    {
                        break;
                    }
                    else
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
            }
        }

        private void UI_Load(object sender, EventArgs e)
        {

        }
    }
}