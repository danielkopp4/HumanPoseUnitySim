#define DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using System.IO;

public class DataPoint
{
    public Vector3 linear;
    public Vector3 angular;
    public float time;

    public DataPoint(Vector3 _linear, Vector3 _angular, float _time)
    {
        linear = _linear;
        angular = _angular;
        time = _time;
    }

    public static DataPoint FromCSVString(string str)
    {
        string[] strings = str.Split(',');
        Vector3 linear = stringsArrToVec3(strings, 0);
        Vector3 angular = stringsArrToVec3(strings, 3);
        float time = float.Parse(strings[6], CultureInfo.InvariantCulture);
        return new DataPoint(linear, angular, time);
    }

    public static Vector3 stringsArrToVec3(string[] strings, int start)
    {
        float[] floats = new float[3];
        for (int i = 0; i < floats.Length; i ++)
        {
            floats[i] = float.Parse(strings[i + start], CultureInfo.InvariantCulture);
        }
        return new Vector3(floats[0], floats[1], floats[2]);
    }

    /* --- !!         USES TIME OF FIRST DATA POINT             !! --- */
    /* --- !!     FISRT IS THE VELOCITY SECOND IS POSITION      !! --- */
    public DataPoint integrate_with(DataPoint dataPoint)
    {
        float delta = (time - dataPoint.time);
        Vector3 _linear = (linear * delta) + dataPoint.linear;
        Vector3 _angular = (angular * delta) + dataPoint.angular;

        return new DataPoint(_linear, _angular, time);
    }
}

public class CSVData
{
    DataPoint[] dataPoints;
    DataPoint[] integrals;
    float[] times;
    public string name;
    private int prevIndex = 0;

    public CSVData(string[] csv)
    {
        name = csv[1].Split(',')[7];
        dataPoints = new DataPoint[csv.Length - 1];

        for (int i = 0; i < csv.Length - 1; i++)
        {
            dataPoints[i] = DataPoint.FromCSVString(csv[i + 1]);
        }
        integrals = integrate(dataPoints);
        times = getTimes(dataPoints);
    }

    public DataPoint[] integrate(DataPoint[] dataPoints)
    {
        DataPoint[] integrals = new DataPoint[dataPoints.Length];

        float dptime = dataPoints[0].time;
        integrals[0] = new DataPoint(dataPoints[0].linear * dptime, dataPoints[0].angular * dptime, dptime);

        for (int i = 1; i < dataPoints.Length; i++)
        {
            integrals[i] = dataPoints[i].integrate_with(integrals[i - 1]);
        }

        return integrals;
    }

    public float[] getTimes(DataPoint[] dataPoints)
    {
        float[] times = new float[dataPoints.Length];
        for (int i = 0; i < dataPoints.Length; i++)
        {
            times[i] = dataPoints[i].time;
        }
        return times;
    }

    public int getIndex(float time)
    {
        if (time <= times[0])
        {
            return 0;
        }

        if (time >= times[times.Length - 1])
        {
            return times.Length - 1;
        }

        if (prevIndex == times.Length - 1)
        {
            return times.Length - 1;
        }

        if (time >= times[prevIndex + 2])
        {
            prevIndex += 2;
            return getIndex(time);
        }

        if (time >= times[prevIndex + 1])
        {
            prevIndex++;
            return prevIndex;
        }

        return prevIndex;
    }

    private DataPoint getPoint(int index, DataPoint[] dataPoints)
    {
        int realIndex = index;
        if (index < 0)
        {
            Debug.LogError($"INDEX LESS THAN ZERO: {index} in {name}");
            index = 0;
        }

        if (index > times.Length - 1)
        {
            #if DEBUG
                Debug.Log("End reached");
            #endif
            index = times.Length - 1;
        }

        return integrals[realIndex];
    }

    public DataPoint getIntegral(int index)
    {
        return getPoint(index, integrals);
    }

    public DataPoint getDataPoint(int index)
    {
        return getPoint(index, dataPoints);
    }

}

public class Rotater
{
    CSVData data;
    Transform body;

    public Rotater(string path, Transform _body)
    {
        data = new CSVData(File.ReadAllLines(path));
        body = _body;
    }

    public void update(float time)
    {
        int index = data.getIndex(time);
        DataPoint this_position = data.getIntegral(index);
        DataPoint next_position = data.getIntegral(index + 1);

        float percent_completion = (time - this_position.time) / (next_position.time - this_position.time);

        //body.position = Vector3.Slerp(this_position.linear, next_position.linear, percent_completion);

        body.rotation = Quaternion.Euler(Vector3.Slerp(this_position.angular, next_position.angular, percent_completion));
    }
}

public class rotate : MonoBehaviour
{

    readonly string path = "Z:/Share/csvs";
    readonly bool read_all = true;
    Rotater rotater;
    // Start is called before the first frame update
    void Start()
    {
        Transform child = transform.Find("braco.R");
        rotater = new Rotater("Z:/Share/csvs/braco.R.csv", child);
    }

    // Update is called once per frame
    void Update()
    {
        rotater.update(Time.time);
    }
}
