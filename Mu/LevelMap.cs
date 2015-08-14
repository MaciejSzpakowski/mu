using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlatRedBall;
using FlatRedBall.Screens;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Input;

namespace Mu
{
    public class LevelMap : Screen
    {
        public LevelMap()
            : base("LevelMap")
        {
        }

        public override void Initialize(bool addToManagers)
        {
            //i think this should be first
            base.Initialize(addToManagers);
            TestInit();
        }

        public override void Activity(bool firstTimeCalled)
        {
            //test
            TestActivity();
            //server and client
            Globals.Server.Activity();
            Globals.Client.Activity();

            //i think this should be at the end
            base.Activity(firstTimeCalled);
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        public void TestInit()
        {
        }

        public void TestActivity()
        {
        }
    }

}
