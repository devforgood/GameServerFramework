using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

#if _USE_BEPU_PHYSICS
using BEPUphysics.Entities.Prefabs;
#endif


namespace core
{
    public class Engine
    {
        public int ServerClientId { get; set; }
        public bool IsServer { get; set; }
        public bool IsClient { get; set; }

        public bool IsRunning
        {
            get { return mShouldKeepRunning; }
        }

        bool mShouldKeepRunning;

        /// <summary>
        /// Global instance of Engine
        /// </summary>
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        public static Engine sInstance = new Engine();

        public Engine()
        {
            mShouldKeepRunning = true;
        }
        public void SetShouldKeepRunning(bool inShouldKeepRunning) { mShouldKeepRunning = inShouldKeepRunning; }

        public virtual void DoFrame()
        {
            World.Update();
        }
        public virtual int Run()
        {
            return DoRunLoop();
        }
        private int DoRunLoop(int millisecondsFrameTerm = 1)
        {
            // Main message loop
            bool quit = false;

            while (!quit && mShouldKeepRunning)
            {
                Timing.sInstance.Update();
                DoFrame();

                Thread.Sleep(millisecondsFrameTerm);

            }

            return 0;
        }

    }
}
