#define DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class IMU : MonoBehaviour
{
    float updateFreq = 2.0f;
    Vector3 angVel;
    Vector3 angAccel; 
    Vector3 linVel;
    Vector3 linAccel;
    private Vector3 lastPos;
    private Vector3 lastAng;
    private Vector3 lastLinVel;
    private Vector3 lastAngVel;
    private float timer = 0.0f;
    readonly int MAX_LINES = 10;
    string[] lines;
    int index;
    string path;

    // Start is called before the first frame update
    void Start()
    {
        path = $"Z:/Share/csvs/{transform.parent.name}.csv";
        File.WriteAllText(path, "LinVel: x,y,z,AngVel: x,y,z,time,name\n");
        lines = new string[MAX_LINES];
        index = 0;
    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        if (timer > (1 / updateFreq))
        {

            lastLinVel = linVel;
            lastAngVel = angVel;

            var lastPosInv = transform.InverseTransformPoint(lastPos);

            linVel.x = (0 - lastPosInv.x) / timer;
            linVel.y = (0 - lastPosInv.y) / timer;
            linVel.z = (0 - lastPosInv.z) / timer;

            var deltaX = Mathf.Abs((transform.rotation.eulerAngles).x) - lastAng.x;
            if (Mathf.Abs(deltaX) < 180 && deltaX > -180) angVel.x = deltaX / timer;
            else
            {
                if (deltaX > 180) angVel.x = (360 - deltaX) / timer;
                else angVel.x = (360 + deltaX) / timer;
            }

            var deltaY = Mathf.Abs((transform.rotation.eulerAngles).y) - lastAng.y;
            if (Mathf.Abs(deltaY) < 180 && deltaY > -180) angVel.y = deltaY / timer;
            else
            {
                if (deltaY > 180) angVel.y = (360 - deltaY) / timer;
                else angVel.y = (360 - deltaY) / timer;
            }

            var deltaZ = Mathf.Abs((transform.rotation.eulerAngles).z) - lastAng.z;
            if (Mathf.Abs(deltaZ) < 180 && deltaZ > -180) angVel.z = deltaZ / timer;
            else
            {
                if (deltaZ > 180) angVel.z = (360 - deltaZ) / timer;
                else angVel.z = (360 + deltaZ) / timer;
            }


            linAccel.x = (linVel.x - lastLinVel.x) / timer;
            linAccel.y = (linVel.y - lastLinVel.y) / timer;
            linAccel.z = (linVel.z - lastLinVel.z) / timer;
            angAccel.x = ((angVel.x - lastAngVel.x) / timer) / 9.81f;
            angAccel.y = ((angVel.y - lastAngVel.y) / timer) / 9.81f;
            angAccel.z = ((angVel.z - lastAngVel.z) / timer) / 9.81f;

            lastPos = transform.position;

            lastAng.x = Mathf.Abs((transform.rotation.eulerAngles).x);
            lastAng.y = Mathf.Abs((transform.rotation.eulerAngles).y);
            lastAng.z = Mathf.Abs((transform.rotation.eulerAngles).z);

            timer = 0;

            saveCSV(linVel, angVel);
        }
    }

    void saveCSV(Vector3 linVel, Vector3 angVel)
    {
        save($"{linVel.x},{linVel.y},{linVel.z},{angVel.x},{angVel.y},{angVel.z},{Time.time},{transform.parent.name}");
    }

    void save(string str)
    {
        #if DEBUG
            Debug.Log(str);
        #endif

        if (index == MAX_LINES) {
            #if DEBUG
                Debug.Log("saving");
            #endif
            File.AppendAllLines(path, lines);
            index = 0;
        } else
        {
            lines[index] = $"{str}\n";
            ++index;
        }
    }
}