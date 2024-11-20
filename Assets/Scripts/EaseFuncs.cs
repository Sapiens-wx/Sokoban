using UnityEngine;
public class EaseFuncs{
    public static float Damping(float time, float duration, float overshootOrAmplitude, float period){
        time/=duration;
        return Mathf.Sin(5*6.283f*time)*Mathf.Exp(-3*time)*.4f;
    }
}