using UnityEngine;
using Tobii.StreamEngine;
using TMPro;
using Global;
using System;
public class Logic : MonoBehaviour
{

    public TMP_Text text;
    private IntPtr api;
    private IntPtr device;
    private tobii_gaze_point_callback_t gazeCallback;
    private Vector2 gazePoint;
    void Start()
    {
        GlobalConfig.rect = text.rectTransform;
        /* 若接入Tobii 4c眼动仪，请取消注释以下代码
        Interop.tobii_api_create(out api, null);
        List<string> urls;
        Interop.tobii_enumerate_local_device_urls(api, out urls);
        Interop.tobii_device_create(api, urls[0], Interop.tobii_field_of_use_t.TOBII_FIELD_OF_USE_INTERACTIVE, out device);

        gazeCallback = OnGazePoint;
        Interop.tobii_gaze_point_subscribe(device, gazeCallback);
        */
    }

    void Update()
    {
        /*
        Interop.tobii_device_process_callbacks(device);
        GetGazePosition(gazePoint);
        */
        //Debug.Log(gazePoint);


        Vector2 mousePos2D = Input.mousePosition;
        GetGazePosition(mousePos2D);
    }
    public void OnServerInitialized()
    {
        Debug.Log("Server initialized");
    }
    private void OnGazePoint(ref tobii_gaze_point_t gaze, IntPtr userData)
    {
        if (gaze.validity == tobii_validity_t.TOBII_VALIDITY_VALID)
        {
            float x = gaze.position.x * Screen.width;
            float y = (1 - gaze.position.y) * Screen.height;

            gazePoint = new Vector2(x, y);
        }
    }

    private void GetGazePosition(Vector2 gazePosition)
    {
        GlobalConfig.lastPoint = GlobalConfig.gazePoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(GlobalConfig.rect, gazePosition, null, out GlobalConfig.gazePoint);

        GlobalConfig.minus_x = GlobalConfig.gazePoint.x - GlobalConfig.lastPoint.x;
        GlobalConfig.minus_y = GlobalConfig.gazePoint.y - GlobalConfig.lastPoint.y;
        return;
    }
}
