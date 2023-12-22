using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvLoader : MonoBehaviour
{
    public static GameObject env;
    public void init()
    {
        env=Resources.Load<GameObject>("Env");
    }
    public void loadEnv()
    {
        Instantiate(env, new Vector3(-13.80f, -27.57f, 5.12f), Quaternion.identity);
    }
}
