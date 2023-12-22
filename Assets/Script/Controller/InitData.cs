using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Init
{
    public struct ColliderAtt{
    public float radius;
    public float height;
    }
    public struct RigidbodyAtt{
        public float mass;
        public float drag;
        public float angularDrag;
    }
    public struct Attribute{
        public float rayOffset;
        public float susRest;
        public float springStr;
        public float springDamp;
        public float maxSpeed;
        public float maxAngle;
        public AnimationCurve powerCurve;
        public float tireGripFactor;
    }
    public static class InitData
    {
        public static int number=1;

        public static RigidbodyAtt[] rbAtt;
        public static ColliderAtt[] cAtt;
        public static Attribute[] att;
        public static void setup()
        {
            rbAtt=new RigidbodyAtt[number];
            att=new Attribute[number];
            cAtt=new ColliderAtt[number];
            for(int i=0;i<number;i++)
            {
                rbAtt[i]=new RigidbodyAtt();
                rbAtt[i].mass=10;
                rbAtt[i].drag=1;
                rbAtt[i].angularDrag=0.05f;

                cAtt[i].radius=0.5f;
                cAtt[i].height=1;

                att[i]=new Attribute();
                att[i].rayOffset=1.5f;
                att[i].susRest=7.5f;
                att[i].springStr=500;
                att[i].springDamp=10;
                att[i].maxSpeed=100;
                att[i].maxAngle=20;
                Keyframe[] keyframes=new Keyframe[2];
                keyframes[0]=new Keyframe(0,1,0,0,0,0.3333333f);
                keyframes[1]=new Keyframe(1,1,0,0,0.3333333f,0);
                att[i].powerCurve=new AnimationCurve(keyframes);
                att[i].tireGripFactor=0.3f;
            }
            
        }
        
    }
}
