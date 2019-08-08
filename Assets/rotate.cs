//#define DEBUG

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using System.IO;

public class DataPoint
{
    public Vector3 value;
    public float time;

    public DataPoint(Vector3 _value, float _time)
    {
        value = _value;
        time = _time;
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
        Vector3 _value = (value * delta) + dataPoint.value;

        return new DataPoint(_value, time);
    }

    public DataPoint scale(float a)
    {
        return  new DataPoint(value * a, time);
    }


    /* --- !!         USES TIME OF FIRST DATA POINT             !! --- */
    /* --- !!     FISRT IS THE VELOCITY SECOND IS POSITION      !! --- */

    public static DataPoint operator+(DataPoint a, DataPoint b)
    {
        return new DataPoint(a.value + b.value, a.time);
    }

    public static DataPoint zero()
    {
        return new DataPoint(Vector3.zero, 0);
    }
}

public class IMUData
{
    public DataPoint accelerometer;
    public DataPoint gyroscope;
    public DataPoint magnetometer;
    public float time;

    public IMUData(DataPoint _accelerometer, DataPoint _gyroscope, DataPoint _magnetometer, float _time)
    {
        accelerometer = _accelerometer;
        gyroscope = _gyroscope;
        magnetometer = _magnetometer;
        time = _time;
    }

    public static IMUData fromCSVString(string str)
    {
        string[] strings = str.Split(',');
        float time = float.Parse(strings[strings.Length - 2]);
        DataPoint accelerometer = new DataPoint(stringsArrToVec3(strings, 0), time);
        DataPoint gyroscope = new DataPoint(stringsArrToVec3(strings, 3), time);
        DataPoint magnetometer = new DataPoint(stringsArrToVec3(strings, 6), time);
        return new IMUData(accelerometer, gyroscope, magnetometer, time);
    }

    public static Vector3 stringsArrToVec3(string[] strings, int start)
    {
        float[] floats = new float[3];
        for (int i = 0; i < floats.Length; i++)
        {
            floats[i] = float.Parse(strings[i + start], CultureInfo.InvariantCulture);
        }
        return new Vector3(floats[0], floats[1], floats[2]);
    }

    public static IMUData firstIntegral(IMUData data, IMUData origin)
    {
        DataPoint accelerometer = data.accelerometer.scale(data.time);
        DataPoint gyroscope = data.gyroscope.scale(data.time);
        DataPoint magnetometer = data.magnetometer.scale(data.time);
        return new IMUData(accelerometer, gyroscope, magnetometer, data.time) + origin;

    }

    /* --- !!         USES TIME OF FIRST DATA POINT             !! --- */
    /* --- !!     FISRT IS THE VELOCITY SECOND IS POSITION      !! --- */
    public IMUData integrate_with(IMUData data)
    {
        DataPoint _accelerometer = accelerometer.integrate_with(data.accelerometer);
        DataPoint _gyroscope = gyroscope.integrate_with(data.gyroscope);
        DataPoint _magnetometer = magnetometer.integrate_with(magnetometer);
        return new IMUData(_accelerometer, _gyroscope, _magnetometer, time);
    }

    public IMUData scale(float a)
    {
        return new IMUData(accelerometer.scale(a), gyroscope.scale(a), magnetometer.scale(a), time);
    }

    public static IMUData operator+(IMUData a, IMUData b)
    {
        return new IMUData(a.accelerometer + b.accelerometer, a.gyroscope + b.gyroscope, a.magnetometer + b.magnetometer, a.time);
    }
}

public class IMUController
{
    IMUData[] data;
    IMUData[] integrals;
    DataPoint[] filteredOrientations;
    int length;

    public IMUController(string[] csv, IMUData origin)
    {
        data = new IMUData[csv.Length - 1];

        for (int i = 0; i < data.Length; i++)
        {
            data[i] = IMUData.fromCSVString(csv[i + 1]);
        }

        length = data.Length;
        integrals = integrate(data, origin);
        filteredOrientations = fuseAndFilter(data, integrals);
    }

    readonly float filterWeight = 0.1f;

    DataPoint[] filter(DataPoint[] data)
    {
        for (int i = 1; i < data.Length; i++)
        {
            data[i] = data[i - 1].scale(1 - filterWeight) + data[i].scale(filterWeight);
        }

        return data;
    }

    IMUData[] filterIMU(IMUData[] data)
    {
        for (int i = 1; i < data.Length; i++)
        {
            data[i] = data[i - 1].scale(1 - filterWeight) + data[i].scale(filterWeight);
        }

        return data;
    }

    IMUData[] integrate(IMUData[] data, IMUData origin)
    {
        IMUData[] integrals = new IMUData[data.Length];
        integrals[0] = IMUData.firstIntegral(data[0], origin);

        for (int i = 1; i < data.Length; i++)
        {
            integrals[i] = data[i].integrate_with(integrals[i - 1]);
        }

        return integrals;
    }

    float magnetWeight = 0f;

    DataPoint[] fuseAndFilter(IMUData[] data, IMUData[] integrals)
    {
        //data = filterIMU(data);
        DataPoint[] orientations = new DataPoint[length];
        for (int i = 0; i < length; i++)
        {
            DataPoint angleFromAccel; //???

            DataPoint angleFromGyroscope;//integral of angVel

            DataPoint angleFromMagnetometer = new DataPoint(Vector3.zero, data[i].time); //??

            float tau = 0.1f;
            float dt;
            if (i == 0)
            {
                dt = data[i].time;
            } else
            {
                dt = data[i].time - data[i - 1].time;
            }
            float alpha = 1;//tau / (tau + dt); 
            DataPoint complementedFilter = integrals[i].gyroscope.scale(alpha) + data[i].accelerometer.scale(1 - alpha);

            orientations[i] = complementedFilter.scale(1 - magnetWeight) + angleFromMagnetometer.scale(magnetWeight);  //angleFromAccel.scale(accelWeight) + angleFromGyroscope.scale(gyroscopeWeight) + angleFromMagnetometer.scale(1 - accelWeight - gyroscopeWeight);
        }

        return filter(orientations);
    }

    public float[] getTimes()
    {
        float[] times = new float[length];
        for (int i = 0; i < length; i++)
        {
            times[i] = data[i].time;
        }
        return times;
    }

    public DataPoint get(int index)
    {
        return filteredOrientations[index];
    }

    public int size()
    {
        return length;
    }

}

public class CSVData
{
    IMUController controller;
    float[] times;
    public string name;
    private int prevIndex = 0;

    public CSVData(string[] csv, IMUData origin)
    {
        name = csv[1].Split(',')[7];
        controller = new IMUController(csv, origin);
        times = getTimes(controller);
    }

    public bool atEnd()
    {
        return prevIndex == times.Length - 1;
    }
    
    public float[] getTimes(IMUController controller)
    {
        return controller.getTimes();
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
            return prevIndex;
        }

        if (time >= times[prevIndex + 1])
        {
            prevIndex++;
            return prevIndex;
        }

        if (prevIndex == times.Length - 2)
        {
            return prevIndex;
        }

        if (time >= times[prevIndex + 2])
        {
            prevIndex += 2;
            return getIndex(time);
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
            realIndex = times.Length - 1;
        }

        return dataPoints[realIndex];
    }

    public Quaternion getOrientation(float time)
    {
        int index = getIndex(time);

        int realIndex = index;
        if (index < 0)
        {
            Debug.LogError($"INDEX LESS THAN ZERO: {index} in {name}");
            index = 0;
        }

        if (index > times.Length - 2)
        {
#if DEBUG
            Debug.Log("End reached");
#endif
            realIndex = times.Length - 2;
        }

        if (atEnd())
        {
            return Quaternion.Euler(controller.get(controller.size() - 1).value);
        }

        DataPoint currentPoint = controller.get(realIndex);
        DataPoint targetPoint = controller.get(realIndex + 1);
        float percent_completion = (time - currentPoint.time) / (targetPoint.time - currentPoint.time);
        return Quaternion.Euler(Vector3.Slerp(currentPoint.value, targetPoint.value, percent_completion));
    }
}

public class Rotater
{
    CSVData data;
    IMUData origin;
    Transform body;

    public Rotater(string path, Transform _body)
    {
        string[] strings = File.ReadAllLines(path);
        List<string> stringFinal = new List<string>();

        foreach (string str in strings)
        {
            if (str.Contains(","))
            {
                stringFinal.Add(str);
            }
        }

        body = _body;
       // if (body.name == "braco.R")
      //  {
      //      origin = new IMUData(DataPoint.zero(), DataPoint.zero(), DataPoint.zero(), 0);
      // } else
       // {
            origin = new IMUData(DataPoint.zero(), new DataPoint(body.eulerAngles, 0), DataPoint.zero(), 0);
     //   }
      
        data = new CSVData(stringFinal.ToArray(), origin);
    }

    public static float function(float time)
    {
        if (time < 2)
        {
            return 0;
        }

        return (90 * Mathf.Sin(2 * Mathf.PI * 0.1f * time));
    }

    public static Quaternion rotationFunction(float time, IMUData origin)
    {
        return Quaternion.Euler(new Vector3(0, function(time), 0) + origin.gyroscope.value);
    }

    public void update(float time)
    {
        //body.rotation = data.getOrientation(time);

       if (body.name.Equals("braco.R"))
        {
            body.rotation = rotationFunction(time, origin);
        }
  }
}

public class rotate : MonoBehaviour
{
    readonly string path = "Z:/Share/csvs/final_csv";
    readonly bool read_all = true;
    Rotater[] rotaters;
    // Start is called before the first frame update

    void OnGUI()
    {
        GUILayout.Label("Magnetometer reading: " + Input.compass.rawVector.ToString());
    }

    void Start()
    {
        Input.compass.enabled = true;
        //TODO: record beginning offset values
        //Model and original should have the same starting position
        string[] files = Directory.GetFiles(path);
        rotaters = new Rotater[files.Length];

        for (int i = 0; i < files.Length; i++)
        {
            string name = getNameFromPath(files[i]);
            Debug.Log($"{name} found");
            Transform child = GameObject.Find(name).transform;
            rotaters[i] = new Rotater(files[i], child);
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < rotaters.Length; i++)
        {
            rotaters[i].update(Time.time);
        }
    }

    string getNameFromPath(string path)
    {
        string[] folders = path.Split('/');
        string[] subParts = folders[folders.Length - 1].Split('\\')[1].Split('.');
        string[] finalNameArr = new string[subParts.Length - 1];

        for (int i = 0; i < finalNameArr.Length; i++)
        {
            finalNameArr[i] = subParts[i];
        }

        return string.Join(".", finalNameArr);
    }
}
