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

namespace RoboArm
{
    public partial class UI : Form
    {

        SerialPort mSerialPort;

        const String mPortName = "COM5";
        const int mBaudRate = 57600;
        const Parity mParity = Parity.None;
        const int mDataBits = 8;
        const StopBits mStopBits = StopBits.One;

        BackgroundWorker worker = new BackgroundWorker();

        Joint[] joints = new Joint[6];
        const double MAXR = 360;
        const double RSCALE = 1;


int[] startingAngles = {
  65,
  107,
  45,
  145,
  90,
  90
};

        public UI()
        {
            Control.CheckForIllegalCrossThreadCalls = false;

            InitializeComponent();

            //mSerialPort = new SerialPort(mPortName, mBaudRate, mParity, mDataBits, mStopBits);
            //mSerialPort.Close();
            //mSerialPort.DtrEnable = true;
            //mSerialPort.RtsEnable = true;
            //mSerialPort.ReceivedBytesThreshold = 1;
            //mSerialPort.Open();

            worker.DoWork += new DoWorkEventHandler(this.Async);
            worker.RunWorkerAsync();

            for(int i=0; i<joints.Length; i++)
            {
                joints[i] = new Joint();
                joints[i].rot = startingAngles[i];
            }

            Console.WriteLine("--init--");
        }

        private long currentTimeMilis()
        {
            return DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private double rectify(double i)
        {
            while (i >= MAXR * RSCALE)
            {
                i -= MAXR * RSCALE;
            }

            while (i < 0)
            {
                i += MAXR * RSCALE;
            }

            return i;
        }

        Arm_Vectors arm = new Arm_Vectors();
        private void Async(object sender, DoWorkEventArgs e)
        {
            var last = currentTimeMilis();
            
            while (false)
            {
                //Console.WriteLine("spin");
                //arm.test();
            }
        }

        private void handleKeyDown(object sender, KeyEventArgs e)
        {
            Console.WriteLine("<< test >>");
            arm.test();
            Console.WriteLine("<< end  >>");
        }

        private void handleKeyUp(object sender, KeyEventArgs e)
        {

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