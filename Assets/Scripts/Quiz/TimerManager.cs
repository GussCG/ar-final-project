using UnityEngine;

public class TimerManager : MonoBehaviour
{
    public float ElapsedTime { get; private set; }
    bool running;

    public void StartTimer()
    {
        ElapsedTime = 0f;
        running = true;
    }

    public void StopTimer()
    {
        running = false;
    }

    void Update()
    {
        if (running)
            ElapsedTime += Time.deltaTime;
    }
}
