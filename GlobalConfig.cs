using UnityEngine;

namespace Global
{
    /// <summary>
    /// 全局配置类。
    /// 
    /// 这个类用于存放文字运动系统中会被多个脚本共享的数据。
    /// 当前项目中，TextChange 会从这里读取注视点、运动速度、半径等参数。
    /// 
    /// 注意：
    /// 这里使用 static 是为了方便原型开发，让不同脚本可以直接访问同一份数据。
    /// 如果后续项目规模变大，可以考虑改成 ScriptableObject 或普通 MonoBehaviour 配置组件。
    /// </summary>
    public static class GlobalConfig
    {
        /// <summary>
        /// 当前被 TextChange 成功记录的可见字符总数。
        /// 主要用于调试，表示实际参与运动逻辑的字符数量。
        /// </summary>
        public static int totalCount = 0;

        /// <summary>
        /// 字符回归原位的预设时间。
        /// 
        /// 当前版本中实际回归速度主要由 back_angleSpeed 和 back_radiusSpeed 控制，
        /// 这个变量保留为后续扩展使用。
        /// </summary>
        public static float back_time = 0f;

        /// <summary>
        /// 字符进入旋转状态后，围绕原始位置偏移旋转的轨道半径。
        /// 
        /// 注意：
        /// 这里的“轨道”不是围绕屏幕中心，而是围绕字符自己的原始中心点进行偏移。
        /// 简单理解就是字符最多会离开原本位置多远。
        /// </summary>
        public static float orbit_radius = 180f;

        /// <summary>
        /// 注视点影响半径。
        /// 
        /// 当字符原始中心点距离 gazePoint 小于这个值时，
        /// 字符会从 Stop 状态进入 Move 状态。
        /// 
        /// 当前版本使用的是 Vector2.Distance，也就是圆形检测区域。
        /// </summary>
        public static float radius = 150f;

        /// <summary>
        /// 字符最大额外缩放比例。
        /// 
        /// ApplyOffset 中会根据字符离开原位的距离计算缩放：
        /// scale = 1 + distance / orbit_radius * max_extraScale
        /// 
        /// 例如 max_extraScale = 0.5 时，
        /// 字符在最远轨道处最大会变成 1.5 倍。
        /// </summary>
        public static float max_extraScale = 0.5f;

        /// <summary>
        /// 注视点相对上一帧的横向移动量。
        /// 
        /// 这个值不是字符自身速度，而是外部输入系统传进来的“视线/鼠标移动偏移”。
        /// TextChange 会用它制造文字被拖拽、滞后的感觉。
        /// </summary>
        public static float minus_x = 0f;

        /// <summary>
        /// 注视点相对上一帧的纵向移动量。
        /// 
        /// 与 minus_x 配合使用，用来模拟注视点移动时文字产生的偏移残留。
        /// </summary>
        public static float minus_y = 0f;

        /// <summary>
        /// 字符从 Move 状态向外扩散时的旋转角速度，单位是“度/秒”。
        /// 
        /// TextChange 内部会将它转换成弧度：
        /// move_rotateSpeed * Mathf.Deg2Rad
        /// </summary>
        public static float move_rotateSpeed = 180f;

        /// <summary>
        /// 字符进入 Spin 状态后持续旋转的角速度，单位是“度/秒”。
        /// 
        /// 一般可以比 move_rotateSpeed 更快，让已经脱离原位的字符形成明显扰动。
        /// </summary>
        public static float spin_rotateSpeed = 270f;

        /// <summary>
        /// 字符从原位向 orbit_radius 扩散的径向速度，单位约等于 Unity 坐标单位/秒。
        /// 
        /// Move_Logic 中会让 distance 持续增加，
        /// 直到达到 orbit_radius 后进入 Spin 状态。
        /// </summary>
        public static float radialSpeed = 150f;

        /// <summary>
        /// 文字偏移残留的释放速度。
        /// 
        /// 当 gazePoint 移动时，ChangeOffset 会把 minus_x / minus_y 累积到字符 offset 中。
        /// offsetSpeed 控制这些残留偏移每帧最多恢复多少。
        /// 
        /// 值越大，文字越快跟上；
        /// 值越小，文字拖尾感越明显。
        /// </summary>
        public static float offsetSpeed = 50f;

        /// <summary>
        /// 当前注视点坐标。
        /// 
        /// 这个坐标需要和 TextMeshPro 顶点所在的坐标系一致。
        /// 如果外部传入的是屏幕坐标，需要先转换到 TMP 所在 RectTransform 的本地坐标。
        /// 
        /// 当前默认值 new(1920, 1080) 只是初始占位。
        /// </summary>
        public static Vector2 gazePoint = new(1920, 1080);

        /// <summary>
        /// 上一帧注视点坐标。
        /// 
        /// 通常用于外部脚本计算 gazePoint 的移动差值，
        /// 再写入 minus_x 和 minus_y。
        /// </summary>
        public static Vector2 lastPoint = new(0, 0);

        /// <summary>
        /// 当前 TMP_Text 对象对应的 RectTransform。
        /// 
        /// TextChange 初始化时会写入 text.rectTransform。
        /// 其他脚本如果需要坐标转换，可以通过这里访问文本区域。
        /// </summary>
        public static RectTransform rect;
    }
}