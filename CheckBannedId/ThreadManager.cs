using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CheckBannedId
{
    class ThreadManager
    {
        private ThreadManager()
        {

        }

        private static List<MyThread> sRunningList;

        private static List<MyThread> sWaitList;

        private static int sMaxThreadCount = 0;

        /// <summary>
        /// 初始化线程池
        /// </summary>
        /// <param name="maxThreadCount">最大线程数</param>
        public static void init(int maxThreadCount)
        {
            sMaxThreadCount = maxThreadCount;
            sRunningList = new List<MyThread>();
            sWaitList = new List<MyThread>();
            Thread thread = new Thread(run);
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// 将线程加入线程池
        /// </summary>
        /// <param name="thread"></param>
        public static void startOneThread(Action run)
        {
            MyThread thd = new MyThread(run);
            thd.OnMyThreadEnd += new MyThread.MyThreadEndDelegate(OnOneThreadEnd);
            sWaitList.Add(thd);

        }

        private static void OnOneThreadEnd(MyThread thd)
        {
            sRunningList.Remove(thd);
        }

        /// <summary>
        /// 处理线程池
        /// </summary>
        private static void run()
        {
            while(true)
            {
                if (sWaitList.Count > 0 && sRunningList.Count <= sMaxThreadCount)
                {
                    MyThread thd = sWaitList[0];
                    sWaitList.Remove(thd);
                    thd.start();
                    sRunningList.Add(thd);
                }
                Thread.Sleep(100);
            }
        }

        class MyThread
        {
            private Action _runable;

            public MyThread(Action runable)
            {
                _runable = runable;
            }

            public delegate void MyThreadEndDelegate(MyThread thd);
            public event MyThreadEndDelegate OnMyThreadEnd;

            private void run()
            {
                _runable();
                if (OnMyThreadEnd != null)
                {
                    OnMyThreadEnd(this);
                }
            }

            public void start()
            {
                Thread thread = new Thread(run);
                thread.IsBackground = true;
                thread.Start();
            }
        }
    }
}
