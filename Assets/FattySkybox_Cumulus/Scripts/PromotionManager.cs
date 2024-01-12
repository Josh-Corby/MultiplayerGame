using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PromotionManager : MonoBehaviour {

    int dayState = 0;
    public float changeValue = 10;
    public Light keyLight;
    public Color32 currentfogColor;

    public Material sunnyMat;
    public float dayIntensity = 1;
    public Color32 dayFog;

    public Material sunsetMat;
    public float sunsetIntensity = 1;
    public Color32 sunsetFog;

    public Material nightMat;
    public float nightIntensity = 1;
    public Color32 nightFog;

    public Material cloudyMat;
    public float cloudyIntensity = 1;
    public Color32 cloudyFog;

    
    void Start () {
        //currentIntensity = keyLight.intensity;
        currentfogColor = RenderSettings.fogColor;

        if (RenderSettings.skybox == sunnyMat)
        {
            currentfogColor = dayFog;
        }
        else if (RenderSettings.skybox == sunsetMat)
        {
            currentfogColor = sunsetFog;
        }
        else if (RenderSettings.skybox == nightMat)
        {
            currentfogColor = nightFog;
        }
        else if (RenderSettings.skybox == cloudyMat)
        {
            currentfogColor = cloudyFog;
        }
        else
        {
            print("Sky box is empty!");
        }
    }
	
	void Update ()
    {

        if (dayState == 0)
        {
            
        }
        else if (dayState == 1) //day
        {
            
            if (sunnyMat != null)
            {
                GotoDay();
            }
            else
            {
                print("sunnyMat missing");
            }
        }
        else if (dayState == 2) //sunset
        {
            if (sunsetMat != null)
            {
                GotoSunset();
            }
            else
            {
                print("sunsetMat missing");
            }
        }
        else if (dayState == 3) //night
        {
            if (nightMat != null)
            {
                GotoNight();
            }
            else
            {
                print("nightMat missing");
            }
        }
        else if (dayState == 4) //rain
        {
            if (cloudyMat != null)
            {
                GotoRain();
            }
            else
            {
                print("cloudyMat missing");
            }
        }
        else
        {
            print("unknown state");
        }
    }

    public void GotoDay()
    {
        if (RenderSettings.skybox != sunnyMat)
        {
            if (Mathf.Abs(dayIntensity - keyLight.intensity) > 0.01f)
            {
                keyLight.intensity = Mathf.Lerp(keyLight.intensity, dayIntensity, Time.deltaTime * changeValue);
                RenderSettings.fogColor = dayFog;
            }
            else
            {
                RenderSettings.skybox = sunnyMat;
                currentfogColor = dayFog;
                dayState = 0;
            }
        }
    }

    public void GotoSunset()
    {
        if (RenderSettings.skybox != sunsetMat)
        {
            if (Mathf.Abs(sunsetIntensity - keyLight.intensity) > 0.01f)
            {
                keyLight.intensity = Mathf.Lerp(keyLight.intensity, sunsetIntensity, Time.deltaTime * changeValue);
                RenderSettings.fogColor = sunsetFog;
            }
            else
            {
                RenderSettings.skybox = sunsetMat;
                currentfogColor = sunsetFog;
                dayState = 0;
            }
        }
    }

    public void GotoNight()
    {
        if (RenderSettings.skybox != nightMat)
        {
            if (Mathf.Abs(nightIntensity - keyLight.intensity) > 0.01f)
            {
                keyLight.intensity = Mathf.Lerp(keyLight.intensity, nightIntensity, Time.deltaTime * changeValue);
                RenderSettings.fogColor = nightFog;
            }
            else
            {
                RenderSettings.skybox = nightMat;
                currentfogColor = nightFog;
                dayState = 0;
            }
        }
    }

    public void GotoRain()
    {
        if (RenderSettings.skybox != cloudyMat)
        {
            if (Mathf.Abs(cloudyIntensity - keyLight.intensity) > 0.01f)
            {
                keyLight.intensity = Mathf.Lerp(keyLight.intensity, cloudyIntensity, Time.deltaTime * changeValue);
                RenderSettings.fogColor = cloudyFog;
            }
            else
            {
                RenderSettings.skybox = cloudyMat;
                currentfogColor = cloudyFog;
                dayState = 0;
            }
        }
    }

    public void DayButton(int buttonState)
    {
        dayState = buttonState;
    }
}
