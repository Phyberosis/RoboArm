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
        const bool SERIAL = false;
        SerialPort mSerialPort;

        const String mPortName = "COM4";
        const int mBaudRate = 57600;
        const Parity mParity = Parity.None;
        const int mDataBits = 8;
        const StopBits mStopBits = StopBits.One;

        BackgroundWorker worker = new BackgroundWorker();

        String keysDown = "";
        Arm arm = new Arm();

        private bool running = true;

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
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }



        private void Async(object sender, DoWorkEventArgs e)
        {
            var last = currentTimeMilis();
            float rate = 10;
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
                    last = currentTimeMilis();

                    StringBuilder angles = new StringBuilder();
                    StringBuilder keys = new StringBuilder();
                    String w = "w", a = "a", s = "s", d = "d";

                    Vector3 displacement = Vector3.Zero;
                    float dDist = rate / dt;

                    if (keyDown(w))
                    {
                        keys.Append(w);
                        displacement.Y += 1;
                    }
                    else if (keyDown(s))
                    {
                        keys.Append(s);
                        displacement.Y -= 1;
                    }

                    if (keyDown(a))
                    {
                        keys.Append(a);
                        displacement.X -= 1;
                    }
                    else if (keyDown(d))
                    {
                        keys.Append(d);
                        displacement.X += 1;
                    }
                    //Console.WriteLine("---" + displacement);
                    if (!displacement.Equals(Vector3.Zero))
                    {
                        displacement = Vector3.Normalize(displacement);
                    }
                    displacement = Vector3.Multiply(displacement, dDist);
                    Vector3 target = Vector3.Add(arm.getCursor(), displacement);
                    //Console.WriteLine("---" + arm.getCursor());
                    arm.setPose(target, arm.getGimbal());

                    float[] servoPos = arm.getServoAngles();

                    for(int index=0; index<servoPos.Length; index++)
                    {
                        float rot = servoPos[index];
                        angles.Append("_").Append(index).Append(",").Append(rot).Append(";");
                    }

                    label1.Text = keys.ToString();

                    String msg = angles.ToString();
                    if(SERIAL) mSerialPort.Write(msg);
                    label2.Text = msg;
                }
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
                if (!keysDown.Contains(key))
                {
                    keysDown += parseKey(key);
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
        }

        //private void UI_KeyDown(object sender, KeyEventArgs e)
        //{
        //    lock (joints)
        //    {
        //        var key = e.KeyCode;
        //        switch (key)
        //        {
        //            case Keys.W:
        //                joints[0].plus();
        //                break;
        //            case Keys.Q:
        //                joints[0].minus();
        //                break;

        //            case Keys.S:
        //                joints[1].plus();
        //                break;
        //            case Keys.A:
        //                joints[1].minus();
        //                break;

        //            case Keys.X:
        //                joints[2].plus();
        //                break;
        //            case Keys.Z:
        //                joints[2].minus();
        //                break;

        //            case Keys.R:
        //                joints[3].plus();
        //                break;
        //            case Keys.E:
        //                joints[3].minus();
        //                break;

        //            case Keys.F:
        //                joints[4].plus();
        //                break;
        //            case Keys.D:
        //                joints[4].minus();
        //                break;

        //            case Keys.V:
        //                joints[5].plus();
        //                break;
        //            case Keys.C:
        //                joints[5].minus();
        //                break;

        //            case Keys.J:
        //                Application.Exit();
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //}

        //private void UI_KeyUp(object sender, KeyEventArgs e)
        //{
        //    lock (joints)
        //    {
        //        var key = e.KeyCode;
        //        switch (key)
        //        {
        //            case Keys.Q:
        //            case Keys.W:
        //                joints[0].stop();
        //                break;

        //            case Keys.A:
        //            case Keys.S:
        //                joints[1].stop();
        //                break;

        //            case Keys.Z:
        //            case Keys.X:
        //                joints[2].stop();
        //                break;

        //            case Keys.E:
        //            case Keys.R:
        //                joints[3].stop();
        //                break;

        //            case Keys.D:
        //            case Keys.F:
        //                joints[4].stop();
        //                break;

        //            case Keys.C:
        //            case Keys.V:
        //                joints[5].stop();
        //                break;
        //            default:
        //                break;
        //        }
        //    }
        //}

    }
}

class Joint
{
    private bool mPlus, mMinus;
    public double rot;

    public Joint()
    {
        mPlus = false;
        mMinus = false;
        rot = 0;
    }

    public bool[] peek()
    {
        return new bool[] { mPlus, mMinus };
    }

    public void plus()
    {
        mPlus = true;
        mMinus = false;
    }

    public void minus()
    {
        mMinus = true;
        mPlus = false;
    }

    public void stop()
    {
        mMinus = false;
        mPlus = false;
    }
}