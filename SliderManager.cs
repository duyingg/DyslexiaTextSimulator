using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Global;

public class SliderManager : MonoBehaviour
{

    public enum SliderType
    {
        sensitivity,
        rotateSpeed,
        radialSpeed,
        radius
    }

    public SliderType sliderType;
    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnSliderChanged(float value)
    {
        switch (sliderType)
        {
            case SliderType.sensitivity:
                SetSensitivity(value);
                break;
            case SliderType.rotateSpeed:
                SetRotateSpeed(value);
                break;
            case SliderType.radialSpeed:
                SetRadialSpeed(value);
                break;
            case SliderType.radius:
                SetRadius(value);
                break;
        }
    }
    private void SetSensitivity(float value)
    {
        GlobalConfig.minus_x = value;
        GlobalConfig.minus_y = value;
    }
    private void SetRotateSpeed(float value)
    {
        GlobalConfig.move_rotateSpeed = value;
        GlobalConfig.spin_rotateSpeed = value;
    }
    private void SetRadialSpeed(float value)
    {
        GlobalConfig.radialSpeed = value;
    }
    private void SetRadius(float value)
    {
        GlobalConfig.radius = value;
    }
    
}


